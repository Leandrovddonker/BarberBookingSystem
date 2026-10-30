import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { Modal } from '../shared/modal';
import { Api } from '../core/api';
import { Auth } from '../core/auth';
import { errorMessage } from '../core/display';
import {
  Barber,
  HairCut,
  Reservation,
  BarberReservation,
  User,
} from '../core/models';
import { shopDate, slotTime, appointmentTime } from '../core/time';

@Component({
  imports: [ReactiveFormsModule, RouterLink, Modal],
  templateUrl: './dashboard.html',
})
export class Dashboard {
  private readonly api = inject(Api);
  private readonly auth = inject(Auth);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private slotRequestVersion = 0;
  private barbers: Barber[] = [];
  private cuts: HairCut[] = [];
  private users: User[] = [];

  readonly role = this.auth.claims()!.role;
  readonly reservations = signal<Reservation[]>([]);
  readonly schedule = signal<BarberReservation[]>([]);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly message = signal(
    this.route.snapshot.queryParamMap.has('booked')
      ? 'You’re booked. We look forward to seeing you!'
      : '',
  );
  readonly editing = signal<Reservation | null>(null);
  readonly cancelTarget = signal<Reservation | null>(null);
  readonly slots = signal<string[]>([]);
  readonly selected = signal('');
  readonly slotLoading = signal(false);
  readonly today = shopDate();
  readonly when = appointmentTime;
  readonly time = slotTime;
  readonly editForm = this.formBuilder.nonNullable.group({
    date: [this.today, Validators.required],
  });
  readonly heading =
    this.role === 'Barber'
      ? 'Behind the chair.'
      : this.role === 'Admin'
        ? 'The appointment book.'
        : 'Your next fresh start.';
  readonly description =
    this.role === 'Barber'
      ? 'Your appointments and customer details, all in one place.'
      : 'Keep track of your visits. Make room for a good cut.';

  constructor() {
    this.load();
  }

  past(startTime: string): boolean {
    return new Date(startTime).getTime() < Date.now();
  }
  cutName(id: number): string {
    return this.cuts.find((cut) => cut.id === id)?.cut || `Service #${id}`;
  }
  barberName(id: number): string {
    return (
      this.barbers.find((barber) => barber.id === id)?.name || `Barber #${id}`
    );
  }
  userName(id: number): string {
    return this.users.find((user) => user.id === id)?.name || `Customer #${id}`;
  }
  userMail(id: number): string{
    return this.users.find((user) => user.id === id)?.email || `Customer #${id}`;
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    if (this.role === 'Barber') {
      this.api
        .barberSchedule()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (schedule) => {
            this.schedule.set(schedule);
            this.loading.set(false);
          },
          error: (error) => this.handleError(error),
        });
      return;
    }

    forkJoin({
      reservations: this.api.reservations(this.role === 'Admin'),
      barbers: this.api.barbers(),
      cuts: this.api.cuts(),
      users: this.role === 'Admin' ? this.api.users() : of<User[]>([]),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.reservations.set(
            data.reservations.sort((first, second) =>
              second.startTime.localeCompare(first.startTime),
            ),
          );
          this.barbers = data.barbers;
          this.cuts = data.cuts;
          this.users = data.users;
          this.loading.set(false);
        },
        error: (error) => this.handleError(error),
      });
  }

  beginEdit(reservation: Reservation): void {
    this.closeEdit();
    this.error.set('');
    this.editing.set(reservation);
    this.editForm.setValue({ date: this.today });
    this.loadSlots(reservation);
  }

  closeEdit(): void {
    this.slotRequestVersion++;
    this.editing.set(null);
    this.slots.set([]);
    this.selected.set('');
    this.slotLoading.set(false);
  }

  cancelEdit(): void {
    if (this.busy()) return;
    this.closeEdit();
    this.error.set('');
  }

  loadSlots(reservation: Reservation): void {
    const requestVersion = ++this.slotRequestVersion;
    this.selected.set('');
    this.slots.set([]);
    const date = this.editForm.controls.date.value;
    this.slotLoading.set(false);
    if (!date || date < this.today) return;

    this.slotLoading.set(true);
    // Exclude only the reservation being edited; the API enforces ownership and conflicts.
    this.api
      .slots(reservation.barberId, reservation.hairCutId, date, reservation.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (slots) => {
          if (requestVersion !== this.slotRequestVersion) return;
          this.slots.set(slots);
          this.slotLoading.set(false);
        },
        error: (error) => {
          if (requestVersion !== this.slotRequestVersion) return;
          this.error.set(errorMessage(error));
          this.slotLoading.set(false);
        },
      });
  }

  save(reservation: Reservation): void {
    if (
      !this.selected() ||
      this.busy() ||
      this.slotLoading() ||
      this.editForm.invalid
    )
      return;
    this.busy.set(true);
    this.error.set('');
    this.api
      .updateReservation(reservation.id, { startTime: this.selected() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.closeEdit();
          this.message.set('Your appointment has been rescheduled.');
          this.load();
        },
        error: (error) => {
          this.handleError(error);
          this.loadSlots(reservation);
        },
      });
  }

  beginCancellation(reservation: Reservation): void {
    if (this.busy()) return;
    this.closeEdit();
    this.error.set('');
    this.cancelTarget.set(reservation);
  }

  closeCancellation(): void {
    if (this.busy()) return;
    this.cancelTarget.set(null);
    this.error.set('');
  }

  cancel(reservation: Reservation): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.api
      .cancelReservation(reservation.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.cancelTarget.set(null);
          this.closeEdit();
          this.message.set('Your visit has been cancelled.');
          this.load();
        },
        error: (error) => this.handleError(error),
      });
  }

  private handleError(error: unknown): void {
    this.error.set(errorMessage(error));
    this.loading.set(false);
    this.busy.set(false);
  }
}
