import { Component, DestroyRef, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { Api } from '../core/api';
import { Barber, HairCut, OpeningHours } from '../core/models';
import { days, errorMessage, openingHoursLabel } from '../core/display';
@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe],
  templateUrl: './home.html',
})
export class Home {
  private readonly api = inject(Api);
  private readonly destroyRef = inject(DestroyRef);

  readonly hoursLabel = openingHoursLabel;
  readonly barbers = signal<Barber[]>([]);
  readonly cuts = signal<HairCut[]>([]);
  readonly hours = signal<OpeningHours[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly days = days;

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    forkJoin({
      barbers: this.api.barbers(),
      cuts: this.api.cuts(),
      hours: this.api.hours(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (catalog) => {
          this.barbers.set(catalog.barbers);
          this.cuts.set(catalog.cuts);
          this.hours.set(
            catalog.hours.sort(
              (first, second) => ((first.day + 6) % 7) - ((second.day + 6) % 7),
            ),
          );
          this.loading.set(false);
        },
        error: (error) => {
          this.error.set(errorMessage(error));
          this.loading.set(false);
        },
      });
  }

  initials(name: string): string {
    return name
      .split(' ')
      .map((part) => part[0])
      .slice(0, 2)
      .join('');
  }
}
