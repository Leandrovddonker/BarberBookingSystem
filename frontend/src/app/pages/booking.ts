import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, forkJoin, of, startWith, switchMap } from 'rxjs';
import { Api } from '../core/api';
import { errorMessage } from '../core/display';
import { Barber, HairCut } from '../core/models';
import { shopDate, slotTime } from '../core/time';

@Component({
  imports: [ReactiveFormsModule, CurrencyPipe],
  templateUrl: './booking.html',
})
export class Booking {
  private readonly api = inject(Api);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly barbers = signal<Barber[]>([]);
  readonly cuts = signal<HairCut[]>([]);
  readonly slots = signal<string[]>([]);
  readonly selected = signal('');
  readonly slotLoading = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly today = shopDate();
  readonly time = slotTime;

  readonly form = this.formBuilder.nonNullable.group({
    barberId: [
      Number(this.route.snapshot.queryParamMap.get('barberId')) || 0,
      Validators.min(1),
    ],
    hairCutId: [
      Number(this.route.snapshot.queryParamMap.get('haircutId')) || 0,
      Validators.min(1),
    ],
    date: [this.today, Validators.required],
  });
  private readonly criteria = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });
  readonly cut = computed(() =>
    this.cuts().find((cut) => cut.id === this.criteria().hairCutId),
  );
  readonly barber = computed(() =>
    this.barbers().find((barber) => barber.id === this.criteria().barberId),
  );
  readonly emptySlotsMessage = computed(() =>
    this.criteria().barberId && this.criteria().hairCutId
      ? 'No available times. Try another date or barber.'
      : 'Choose a barber, service, and date to see available times.',
  );

  constructor() {
    this.loadCatalog();
    this.watchAvailability();
  }

  submit(): void {
    if (!this.selected() || this.form.invalid || this.busy()) return;

    this.busy.set(true);
    this.error.set('');
    const { barberId, hairCutId } = this.form.getRawValue();
    this.api
      .createReservation({ barberId, hairCutId, startTime: this.selected() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () =>
          void this.router.navigate(['/dashboard'], {
            queryParams: { booked: 1 },
          }),
        error: (error) => {
          this.busy.set(false);
          // Refresh server availability after a conflict; never reuse the old selection.
          this.form.updateValueAndValidity({ emitEvent: true });
          this.error.set(errorMessage(error));
        },
      });
  }

  private loadCatalog(): void {
    forkJoin({ barbers: this.api.barbers(), cuts: this.api.cuts() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (catalog) => {
          this.barbers.set(catalog.barbers);
          this.cuts.set(catalog.cuts);
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  private watchAvailability(): void {
    this.form.valueChanges
      .pipe(
        startWith(this.form.getRawValue()),
        switchMap((criteria) => {
          this.slots.set([]);
          this.selected.set('');
          this.error.set('');
          this.slotLoading.set(false);

          const { barberId, hairCutId, date } = criteria;
          if (!barberId || !hairCutId || !date || date < this.today)
            return of<string[]>([]);

          this.slotLoading.set(true);
          // Creation intentionally omits excludeReservationId.
          return this.api.slots(barberId, hairCutId, date).pipe(
            catchError((error) => {
              this.error.set(errorMessage(error));
              return of<string[]>([]);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((slots) => {
        this.slots.set(slots);
        this.slotLoading.set(false);
      });
  }
}
