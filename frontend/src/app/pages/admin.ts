import { Component, DestroyRef, inject, signal } from '@angular/core';
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

type ManagedResource = 'Cut' | 'OpeningHours';
interface PendingDeletion {
  resource: ManagedResource;
  id: number;
  label: string;
}

@Component({
  imports: [ReactiveFormsModule, RouterLink, Modal],
  templateUrl: './admin.html',
})
export class Admin {
  private readonly api = inject(Api);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private originalDay = 0;

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
  readonly hourId = signal<number | null>(null);
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
    day: [1, Validators.required],
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

  editHours(hours: OpeningHours): void {
    this.hourId.set(hours.id);
    this.originalDay = hours.day;
    this.hoursForm.setValue({
      day: hours.day,
      openingTime: hours.openingTime.slice(0, 5),
      closingTime: hours.closingTime.slice(0, 5),
    });
    this.hoursForm.controls.day.disable();
  }

  resetHours(): void {
    this.hourId.set(null);
    this.hoursForm.controls.day.enable();
    this.hoursForm.reset({
      day: 1,
      openingTime: '09:00',
      closingTime: '18:00',
    });
  }

  saveHours(): void {
    if (this.hoursForm.invalid) return;
    const hours = this.hoursForm.getRawValue();
    if (
      !this.hourId() &&
      this.hours().some((existing) => existing.day === hours.day)
    ) {
      this.error.set(
        'This day already has opening hours. Edit the existing entry.',
      );
      return;
    }
    if (hours.closingTime < hours.openingTime) {
      this.error.set(
        'Closing time must be after opening time, or equal for a closed day.',
      );
      return;
    }
    const body: OpeningHoursDto = {
      day: hours.day,
      openingTime: hours.openingTime + ':00',
      closingTime: hours.closingTime + ':00',
    };
    const request = this.hourId()
      ? this.api.updateHours(this.originalDay, body)
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

  askDelete(resource: ManagedResource, id: number, label: string): void {
    this.error.set('');
    this.deletion.set({ resource, id, label });
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
    const request =
      deletion.resource === 'Cut'
        ? this.api.deleteCut(deletion.id)
        : this.api.deleteHours(deletion.id);
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
