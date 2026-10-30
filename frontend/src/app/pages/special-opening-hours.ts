import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { Api } from '../core/api';
import { errorMessage } from '../core/display';
import {
  CreateSpecialOpeningHoursDto,
  SpecialOpeningHours,
  UpdateSpecialOpeningHoursDto,
} from '../core/models';
import { shopDate } from '../core/time';
import { Modal } from '../shared/modal';

type PendingAction =
  | { kind: 'create'; body: CreateSpecialOpeningHoursDto; date: string }
  | {
      kind: 'update';
      id: number;
      body: UpdateSpecialOpeningHoursDto;
      date: string;
    }
  | { kind: 'delete'; id: number; date: string };

const DUPLICATE_DATE_MESSAGE =
  'Special opening hours already exist for this date. Edit the existing exception instead.';
const TIME_PATTERN = /^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/;

@Component({
  selector: 'app-special-opening-hours',
  imports: [ReactiveFormsModule, Modal],
  templateUrl: './special-opening-hours.html'
})
export class SpecialOpeningHoursComponent {
  private readonly api = inject(Api);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dateFormatter = new Intl.DateTimeFormat('nl-NL', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  });

  readonly today = shopDate;
  readonly exceptions = signal<SpecialOpeningHours[]>([]);
  readonly sortedExceptions = computed(() =>
    [...this.exceptions()].sort((first, second) =>
      first.date.localeCompare(second.date),
    ),
  );
  readonly loading = signal(false);
  readonly listError = signal('');
  readonly formError = signal('');
  readonly message = signal('');
  readonly editor = signal<{ id: number | null; date: string } | null>(null);
  readonly deletion = signal<SpecialOpeningHours | null>(null);
  readonly pendingAction = signal<PendingAction | null>(null);
  readonly reservationCount = signal(0);
  readonly operation = signal<'idle' | 'checking' | 'saving'>('idle');
  readonly busy = computed(() => this.operation() !== 'idle');
  readonly dialogOpen = computed(
    () => this.editor() !== null || this.deletion() !== null,
  );
  readonly dialogTitle = computed(() => {
    if (this.pendingAction()) return 'Existing appointments';
    if (this.deletion()) return 'Delete special opening hours?';
    return this.editor()?.id !== null
      ? 'Edit special opening hours'
      : 'Add special opening hours';
  });

  readonly form = this.formBuilder.nonNullable.group({
    date: [
      '',
      [Validators.required, Validators.pattern(/^\d{4}-\d{2}-\d{2}$/)],
    ],
    isClosed: [false],
    openingTime: [
      '09:00',
      [Validators.required, Validators.pattern(TIME_PATTERN)],
    ],
    closingTime: [
      '18:00',
      [Validators.required, Validators.pattern(TIME_PATTERN)],
    ],
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.listError.set('');
    this.api
      .specialHours()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (exceptions) => {
          this.exceptions.set(exceptions);
          this.loading.set(false);
        },
        error: (error) => {
          this.listError.set(errorMessage(error));
          this.loading.set(false);
        },
      });
  }

  formatDate(date: string): string {
    // DateOnly is a calendar date, not an appointment timestamp.
    return this.dateFormatter.format(new Date(`${date}T00:00:00Z`));
  }

  isPast(date: string): boolean {
    return date < this.today();
  }

  add(): void {
    if (this.busy() || this.loading()) return;
    this.resetDialog();
    this.form.controls.date.enable();
    this.form.reset({
      date: '',
      isClosed: false,
      openingTime: '09:00',
      closingTime: '18:00',
    });
    this.toggleClosed();
    this.editor.set({ id: null, date: '' });
  }

  edit(exception: SpecialOpeningHours): void {
    if (this.busy() || this.loading() || this.isPast(exception.date)) return;
    this.resetDialog();
    this.editor.set({ id: exception.id, date: exception.date });
    this.form.setValue({
      date: exception.date,
      isClosed: exception.isClosed,
      openingTime: exception.openingTime?.slice(0, 8) ?? '09:00',
      closingTime: exception.closingTime?.slice(0, 8) ?? '18:00',
    });
    this.form.controls.date.disable();
    this.toggleClosed();
  }

  toggleClosed(): void {
    const { openingTime, closingTime, isClosed } = this.form.controls;
    if (isClosed.value) {
      openingTime.setValue('00:00');
      closingTime.setValue('00:00');
      openingTime.disable();
      closingTime.disable();
    } else {
      openingTime.enable();
      closingTime.enable();
    }
  }

  save(): void {
    const editor = this.editor();
    if (!editor || this.busy() || this.pendingAction()) return;
    this.form.markAllAsTouched();
    this.formError.set('');
    if (this.form.invalid) {
      this.formError.set(
        'Enter a date and valid opening and closing times, or mark the day as closed.',
      );
      return;
    }
    const values = this.form.getRawValue();
    const date = editor.id === null ? values.date : editor.date;
    if (this.isPast(date)) {
      this.formError.set(
        'Special opening hours cannot be changed for a past date.',
      );
      return;
    }
    if (
      editor.id === null &&
      this.exceptions().some((exception) => exception.date === date)
    ) {
      this.formError.set(DUPLICATE_DATE_MESSAGE);
      return;
    }
    const openingTime = values.isClosed
      ? null
      : this.serializeTime(values.openingTime);
    const closingTime = values.isClosed
      ? null
      : this.serializeTime(values.closingTime);
    if (
      openingTime !== null &&
      closingTime !== null &&
      openingTime >= closingTime
    ) {
      this.formError.set('Opening time must be earlier than closing time.');
      return;
    }
    const body: UpdateSpecialOpeningHoursDto = {
      isClosed: values.isClosed,
      openingTime,
      closingTime,
    };
    this.checkReservations(
      editor.id === null
        ? { kind: 'create', date, body: { date, ...body } }
        : { kind: 'update', id: editor.id, date, body },
    );
  }

  askDelete(exception: SpecialOpeningHours): void {
    if (this.busy() || this.loading()) return;
    this.resetDialog();
    this.deletion.set(exception);
  }

  deleteException(): void {
    const exception = this.deletion();
    if (!exception || this.busy() || this.pendingAction()) return;
    this.checkReservations({
      kind: 'delete',
      id: exception.id,
      date: exception.date,
    });
  }

  confirmAction(): void {
    const action = this.pendingAction();
    if (!action || this.busy()) return;
    this.mutate(action);
  }

  cancelConfirmation(): void {
    if (this.busy()) return;
    if (this.deletion()) {
      this.closeDialog();
      return;
    }
    this.pendingAction.set(null);
    this.formError.set('');
    this.reservationCount.set(0);
  }

  closeDialog(): void {
    if (!this.busy()) this.resetDialog();
  }

  private checkReservations(action: PendingAction): void {
    this.operation.set('checking');
    this.formError.set('');
    this.api
      .specialHoursReservationCount(action.date)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (count) => {
          if (count > 0) {
            this.reservationCount.set(count);
            this.pendingAction.set(action);
            this.operation.set('idle');
          } else {
            this.mutate(action);
          }
        },
        error: (error) => {
          this.formError.set(errorMessage(error));
          this.operation.set('idle');
        },
      });
  }

  private mutate(action: PendingAction): void {
    // Recheck after counting/confirmation in case the shop date changed overnight.
    if (action.kind !== 'delete' && this.isPast(action.date)) {
      this.formError.set(
        'Special opening hours cannot be changed for a past date.',
      );
      this.pendingAction.set(null);
      this.operation.set('idle');
      return;
    }
    this.operation.set('saving');
    this.formError.set('');
    const request: Observable<unknown> =
      action.kind === 'create'
        ? this.api.createSpecialHours(action.body)
        : action.kind === 'update'
          ? this.api.updateSpecialHours(action.id, action.body)
          : this.api.deleteSpecialHours(action.id);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.operation.set('idle');
        this.resetDialog();
        this.message.set(
          action.kind === 'delete'
            ? 'Special opening hours deleted.'
            : 'Special opening hours saved.',
        );
        this.load();
      },
      error: (error) => {
        this.formError.set(
          error instanceof HttpErrorResponse && error.status === 409
            ? DUPLICATE_DATE_MESSAGE
            : errorMessage(error),
        );
        this.pendingAction.set(null);
        this.operation.set('idle');
      },
    });
  }

  private resetDialog(): void {
    this.editor.set(null);
    this.deletion.set(null);
    this.pendingAction.set(null);
    this.reservationCount.set(0);
    this.formError.set('');
  }

  private serializeTime(value: string): string {
    return value.length === 5 ? `${value}:00` : value;
  }
}
