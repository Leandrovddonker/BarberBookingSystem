import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable, forkJoin } from 'rxjs';
import { Modal } from '../shared/modal';
import { Api } from '../core/api';
import {
  HairCut,
  OpeningHours,
  User,
  Barber,
  OpeningHoursDto,
} from '../core/models';
import { days, errorMessage, openingHoursLabel } from '../core/display';

interface PendingDeletion {
  id: number;
  label: string;
}

@Component({
  imports: [ReactiveFormsModule, RouterLink, Modal],
  templateUrl: './admin.html',
  styleUrl: './admin.css',
})
export class Admin {
  private readonly api = inject(Api);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly days = days;
  readonly roles = ['Customer', 'Barber', 'Admin'];
  readonly hoursLabel = openingHoursLabel;
  readonly cuts = signal<HairCut[]>([]);
  readonly hours = signal<OpeningHours[]>([]);
  readonly users = signal<User[]>([]);
  readonly barbers = signal<Barber[]>([]);
  readonly error = signal('');
  readonly message = signal('');
  readonly busy = signal(false);
  readonly loading = signal(false);
  readonly cutId = signal<number | null>(null);
  readonly editingDay = signal<number | null>(null);
  readonly weekdays = computed(() =>
    [1, 2, 3, 4, 5, 6, 0].map((day) => ({
      day,
      record: this.hours().find((hours) => hours.day === day),
    })),
  );
  readonly deletion = signal<PendingDeletion | null>(null);

  readonly cutForm = this.formBuilder.nonNullable.group({
    cut: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
    durationMinutes: [
      30,
      [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)],
    ],
  });
  readonly hoursForm = this.formBuilder.nonNullable.group({
    closed: [false],
    openingTime: ['09:00', Validators.required],
    closingTime: ['18:00', Validators.required],
  });
  readonly barberForm = this.formBuilder.nonNullable.group({
    name: ['', Validators.required],
    description: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', Validators.required],
    password: ['', Validators.required],
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    forkJoin({
      cuts: this.api.cuts(),
      hours: this.api.hours(),
      users: this.api.users(),
      barbers: this.api.barbers(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.cuts.set(data.cuts);
          this.hours.set(
            data.hours.sort((first, second) => first.day - second.day),
          );
          this.users.set(data.users);
          this.barbers.set(data.barbers);
          this.loading.set(false);
        },
        error: (error) => {
          this.error.set(errorMessage(error));
          this.loading.set(false);
        },
      });
  }

  editCut(cut: HairCut): void {
    this.cutId.set(cut.id);
    this.cutForm.setValue({
      cut: cut.cut,
      price: cut.price,
      durationMinutes: cut.durationMinutes,
    });
  }

  resetCut(): void {
    this.cutId.set(null);
    this.cutForm.reset({ cut: '', price: 0, durationMinutes: 30 });
  }

  saveCut(): void {
    if (this.cutForm.invalid) return;
    const cut = this.cutForm.getRawValue();
    const id = this.cutId();
    const request = id ? this.api.updateCut(id, cut) : this.api.createCut(cut);
    this.runMutation(request, 'Service saved.', () => this.resetCut());
  }

  editHours(day: number): void {
    if (this.busy()) return;
    const record = this.hours().find((hours) => hours.day === day);
    this.editingDay.set(day);
    this.error.set('');
    this.hoursForm.setValue({
      closed: !record || record.openingTime >= record.closingTime,
      openingTime: record?.openingTime.slice(0, 5) ?? '09:00',
      closingTime: record?.closingTime.slice(0, 5) ?? '18:00',
    });
    this.toggleClosed();
  }

  toggleClosed(): void {
    const { openingTime, closingTime } = this.hoursForm.controls;
    if (this.hoursForm.controls.closed.value) {
      openingTime.disable();
      closingTime.disable();
    } else {
      openingTime.enable();
      closingTime.enable();
    }
  }

  resetHours(): void {
    this.editingDay.set(null);
    this.hoursForm.reset({
      closed: false,
      openingTime: '09:00',
      closingTime: '18:00',
    });
    this.toggleClosed();
  }

  saveHours(): void {
    const day = this.editingDay();
    if (day === null || this.busy() || this.hoursForm.invalid) return;
    const hours = this.hoursForm.getRawValue();
    if (!hours.closed && hours.closingTime <= hours.openingTime) {
      this.error.set(
        'Closing time must be after opening time. Mark the day as closed if the shop is not open.',
      );
      return;
    }
    // Equal times represent a closed day in the existing backend contract.
    const body: OpeningHoursDto = {
      day,
      openingTime: hours.closed ? '00:00:00' : hours.openingTime + ':00',
      closingTime: hours.closed ? '00:00:00' : hours.closingTime + ':00',
    };
    const exists = this.hours().some((record) => record.day === day);
    const request = exists
      ? this.api.updateHours(day, body)
      : this.api.createHours(body);
    this.runMutation(request, 'Opening hours saved.', () => this.resetHours());
  }

  saveBarber(): void {
    if (this.barberForm.invalid) return;
    this.runMutation(
      this.api.createBarber(this.barberForm.getRawValue()),
      'Barber account created.',
      () => this.barberForm.reset(),
    );
  }

  askDelete(id: number, label: string): void {
    this.error.set('');
    this.deletion.set({ id, label });
  }

  cancelDeletion(): void {
    if (this.busy()) return;
    this.deletion.set(null);
    this.error.set('');
  }

  confirmDelete(): void {
    if (this.busy()) return;
    const deletion = this.deletion();
    if (!deletion) return;
    const request = this.api.deleteCut(deletion.id);
    this.runMutation(request, 'Item deleted.', () => {
      this.deletion.set(null);
      this.resetCut();
      this.resetHours();
    });
  }

  private runMutation(
    request: Observable<unknown>,
    message: string,
    onSuccess: () => void,
  ): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busy.set(false);
        onSuccess();
        this.message.set(message);
        this.load();
      },
      error: (error) => {
        this.busy.set(false);
        this.error.set(errorMessage(error));
      },
    });
  }
}
