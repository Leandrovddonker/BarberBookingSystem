import '@angular/compiler';
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  createEnvironmentInjector,
  EnvironmentInjector,
  Provider,
  runInInjectionContext,
} from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';
import { Api } from '../src/app/core/api';
import { Auth } from '../src/app/core/auth';
import {
  CreateReservationDto,
  OpeningHoursDto,
  OpeningHours,
  Reservation,
} from '../src/app/core/models';
import { Booking } from '../src/app/pages/booking';
import { Dashboard } from '../src/app/pages/dashboard';
import { AuthPage } from '../src/app/pages/auth-page';
import { Admin } from '../src/app/pages/admin';
import { errorMessage, openingHoursLabel } from '../src/app/core/display';

const reservation: Reservation = {
  id: 42,
  barberId: 2,
  hairCutId: 3,
  userId: 7,
  startTime: '2099-01-07T09:00:00Z',
};
const service = { id: 3, cut: 'Classic cut', durationMinutes: 30, price: 25 };
const barber = {
  id: 2,
  userId: 8,
  name: 'Sam',
  email: 'sam@example.com',
  phoneNumber: '123',
  description: '',
};
const user = {
  id: 7,
  name: 'Alex',
  email: 'alex@example.com',
  phoneNumber: '456',
  role: 0,
  reservations: [],
};

function createInjector(api: Partial<Api>, providers: Provider[] = []) {
  return createEnvironmentInjector(
    [FormBuilder, { provide: Api, useValue: api }, ...providers],
    null as unknown as EnvironmentInjector,
  );
}

function routeWith(query: Record<string, string> = {}, path = '') {
  return {
    snapshot: {
      routeConfig: { path },
      queryParamMap: {
        get: (key: string) => query[key] ?? null,
        has: (key: string) => key in query,
      },
    },
  };
}

test('booking resets obsolete slots, updates summary, and sends the unchanged creation DTO', () => {
  const slotRequests: unknown[][] = [];
  const responses: Subject<string[]>[] = [];
  const created: CreateReservationDto[] = [];
  const navigations: unknown[][] = [];
  const injector = createInjector(
    {
      barbers: () => of([barber]),
      cuts: () => of([service]),
      slots: (...criteria) => {
        slotRequests.push(criteria);
        const response = new Subject<string[]>();
        responses.push(response);
        return response;
      },
      createReservation: (body) => {
        created.push(body);
        return of(reservation);
      },
    },
    [
      {
        provide: ActivatedRoute,
        useValue: routeWith({ barberId: '2', haircutId: '3' }),
      },
      {
        provide: Router,
        useValue: {
          navigate: (...args: unknown[]) => {
            navigations.push(args);
            return Promise.resolve(true);
          },
        },
      },
    ],
  );
  try {
    const booking = runInInjectionContext(injector, () => new Booking());
    assert.equal(booking.cut()?.cut, service.cut);
    assert.equal(booking.barber()?.name, barber.name);
    assert.deepEqual(slotRequests[0], [2, 3, booking.today]);
    responses[0].next([reservation.startTime]);
    booking.selected.set(reservation.startTime);
    booking.form.patchValue({ date: '2099-01-08' });
    assert.equal(booking.selected(), '');
    assert.deepEqual(booking.slots(), []);
    assert.equal(responses[0].observed, false);
    responses[0].next(['stale']);
    assert.deepEqual(booking.slots(), []);
    responses[1].next([reservation.startTime]);
    booking.selected.set(reservation.startTime);
    booking.submit();
    assert.deepEqual(created, [
      { barberId: 2, hairCutId: 3, startTime: reservation.startTime },
    ]);
    assert.deepEqual(navigations, [
      [['/dashboard'], { queryParams: { booked: 1 } }],
    ]);
    injector.destroy();
    assert.equal(responses[1].observed, false);
  } finally {
    if (!injector.destroyed) injector.destroy();
  }
});

test('booking conflict refreshes availability and allows a subsequent request after an availability error', () => {
  let availabilityCalls = 0;
  const injector = createInjector(
    {
      barbers: () => of([barber]),
      cuts: () => of([service]),
      slots: () =>
        ++availabilityCalls === 1
          ? throwError(() => new HttpErrorResponse({ status: 500 }))
          : of([reservation.startTime]),
      createReservation: () =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 409,
              error: { detail: 'Already booked.' },
            }),
        ),
    },
    [
      {
        provide: ActivatedRoute,
        useValue: routeWith({ barberId: '2', haircutId: '3' }),
      },
      { provide: Router, useValue: {} },
    ],
  );
  try {
    const booking = runInInjectionContext(injector, () => new Booking());
    assert.equal(booking.slotLoading(), false);
    assert.notEqual(booking.error(), '');
    booking.form.patchValue({ date: '2099-01-08' });
    assert.deepEqual(booking.slots(), [reservation.startTime]);
    booking.selected.set(reservation.startTime);
    booking.submit();
    assert.equal(booking.busy(), false);
    assert.equal(booking.selected(), '');
    assert.equal(availabilityCalls, 3);
    assert.equal(booking.error(), 'Already booked.');
  } finally {
    injector.destroy();
  }
});

test('rescheduling failure refreshes with the same exclusion ID and preserves the time-only update contract', () => {
  const slotRequests: unknown[][] = [];
  const updates: unknown[][] = [];
  const injector = createInjector(
    {
      reservations: () => of([reservation]),
      barbers: () => of([barber]),
      cuts: () => of([service]),
      slots: (...criteria) => {
        slotRequests.push(criteria);
        return of([reservation.startTime]);
      },
      updateReservation: (...args) => {
        updates.push(args);
        return throwError(
          () =>
            new HttpErrorResponse({
              status: 409,
              error: { detail: 'Already booked.' },
            }),
        );
      },
    },
    [
      { provide: Auth, useValue: { claims: () => ({ role: 'Customer' }) } },
      { provide: ActivatedRoute, useValue: routeWith() },
    ],
  );
  try {
    const dashboard = runInInjectionContext(injector, () => new Dashboard());
    dashboard.beginEdit(reservation);
    dashboard.selected.set(reservation.startTime);
    dashboard.save(reservation);
    assert.deepEqual(updates, [[42, { startTime: reservation.startTime }]]);
    assert.deepEqual(slotRequests, [
      [2, 3, dashboard.today, 42],
      [2, 3, dashboard.today, 42],
    ]);
    assert.equal(dashboard.busy(), false);
    assert.equal(dashboard.selected(), '');
    assert.equal(dashboard.editing(), reservation);
    assert.equal(dashboard.error(), 'Already booked.');
  } finally {
    injector.destroy();
  }
});

test('dashboard releases in-flight availability on destruction', () => {
  const response = new Subject<string[]>();
  const injector = createInjector(
    {
      reservations: () => of([]),
      barbers: () => of([]),
      cuts: () => of([]),
      slots: () => response,
    },
    [
      { provide: Auth, useValue: { claims: () => ({ role: 'Customer' }) } },
      { provide: ActivatedRoute, useValue: routeWith() },
    ],
  );
  const dashboard = runInInjectionContext(injector, () => new Dashboard());
  dashboard.beginEdit(reservation);
  assert.equal(response.observed, true);
  injector.destroy();
  assert.equal(response.observed, false);
  response.next(['late']);
  assert.deepEqual(dashboard.slots(), []);
});

test('registration validates required details, registers before login, and rejects an external return URL', () => {
  const calls: string[] = [];
  const destinations: string[] = [];
  const injector = createInjector(
    {
      register: () => {
        calls.push('register');
        return of(user);
      },
    },
    [
      {
        provide: Auth,
        useValue: {
          login: () => {
            calls.push('login');
            return of({ token: 'token' });
          },
        },
      },
      {
        provide: ActivatedRoute,
        useValue: routeWith({ returnUrl: '//example.com' }, 'register'),
      },
      {
        provide: Router,
        useValue: {
          navigateByUrl: (url: string) => {
            destinations.push(url);
            return Promise.resolve(true);
          },
        },
      },
    ],
  );
  try {
    const page = runInInjectionContext(injector, () => new AuthPage());
    page.submit();
    assert.equal(page.invalid(), true);
    assert.deepEqual(calls, []);
    page.form.setValue({
      name: 'Alex',
      phoneNumber: '123',
      email: 'alex@example.com',
      password: 'password',
    });
    page.submit();
    assert.deepEqual(calls, ['register', 'login']);
    assert.deepEqual(destinations, ['/dashboard']);
  } finally {
    injector.destroy();
  }
});

function setupOpeningHours(initialRecords: OpeningHours[]) {
  let records = initialRecords.map((record) => ({ ...record }));
  const creates: OpeningHoursDto[] = [];
  const updates: { day: number; body: OpeningHoursDto }[] = [];
  const injector = createInjector({
    cuts: () => of([]),
    users: () => of([]),
    barbers: () => of([]),
    hours: () => of(records.map((record) => ({ ...record }))),
    createHours: (body) => {
      creates.push(body);
      records.push({ id: records.length + 1, ...body });
      return of(body);
    },
    updateHours: (day, body) => {
      updates.push({ day, body });
      records = records.map((record) =>
        record.day === day ? { ...record, ...body } : record,
      );
      return of(body);
    },
  });
  const admin = runInInjectionContext(injector, () => new Admin());
  return { admin, injector, creates, updates };
}

test('opening hours always show Monday through Sunday without creating records on load or cancel', () => {
  const { admin, injector, creates, updates } = setupOpeningHours([]);
  try {
    assert.deepEqual(
      admin.weekdays().map((weekday) => weekday.day),
      [1, 2, 3, 4, 5, 6, 0],
    );
    assert.ok(admin.weekdays().every((weekday) => !weekday.record));
    assert.equal(admin.editingDay(), null);
    assert.deepEqual(creates, []);
    admin.editHours(1);
    assert.equal(admin.hoursForm.controls.closed.value, true);
    assert.equal(admin.hoursForm.controls.openingTime.disabled, true);
    admin.resetHours();
    admin.saveHours();
    assert.equal(admin.editingDay(), null);
    assert.deepEqual(creates, []);
    assert.deepEqual(updates, []);
  } finally {
    injector.destroy();
  }
});

test('saving missing Sunday creates once; subsequent edits update the saved day', () => {
  const { admin, injector, creates, updates } = setupOpeningHours([]);
  try {
    admin.editHours(0);
    admin.saveHours();
    assert.deepEqual(creates, [
      { day: 0, openingTime: '00:00:00', closingTime: '00:00:00' },
    ]);
    assert.equal(admin.editingDay(), null);
    assert.equal(admin.weekdays()[6].record?.day, 0);
    admin.editHours(0);
    admin.hoursForm.controls.closed.setValue(false);
    admin.toggleClosed();
    admin.hoursForm.patchValue({ openingTime: '10:00', closingTime: '16:30' });
    admin.saveHours();
    assert.equal(creates.length, 1);
    assert.deepEqual(updates, [
      {
        day: 0,
        body: { day: 0, openingTime: '10:00:00', closingTime: '16:30:00' },
      },
    ]);
  } finally {
    injector.destroy();
  }
});

test('existing opening hours update by weekday and can be marked closed', () => {
  const { admin, injector, creates, updates } = setupOpeningHours([
    { id: 42, day: 1, openingTime: '09:00:00', closingTime: '18:00:00' },
  ]);
  try {
    assert.equal(admin.weekdays()[0].record?.id, 42);
    assert.equal(admin.weekdays()[1].record, undefined);
    admin.editHours(1);
    admin.hoursForm.patchValue({ closingTime: '17:00' });
    admin.saveHours();
    assert.deepEqual(updates[0], {
      day: 1,
      body: { day: 1, openingTime: '09:00:00', closingTime: '17:00:00' },
    });
    admin.editHours(1);
    admin.hoursForm.controls.closed.setValue(true);
    admin.toggleClosed();
    admin.saveHours();
    assert.deepEqual(updates[1], {
      day: 1,
      body: { day: 1, openingTime: '00:00:00', closingTime: '00:00:00' },
    });
    assert.deepEqual(creates, []);
    assert.equal(openingHoursLabel(admin.weekdays()[0].record!), 'Closed');
  } finally {
    injector.destroy();
  }
});

test('open days require valid increasing times; a missing day is created only on a valid save', () => {
  const { admin, injector, creates, updates } = setupOpeningHours([]);
  try {
    admin.editHours(2);
    admin.hoursForm.controls.closed.setValue(false);
    admin.toggleClosed();
    admin.hoursForm.patchValue({ openingTime: '10:00', closingTime: '09:00' });
    admin.saveHours();
    assert.match(admin.error(), /Closing time must be after opening time/);
    admin.hoursForm.patchValue({ closingTime: '10:00' });
    admin.saveHours();
    assert.deepEqual(creates, []);
    admin.hoursForm.patchValue({ openingTime: '' });
    admin.saveHours();
    assert.equal(admin.hoursForm.invalid, true);
    assert.deepEqual(creates, []);
    admin.hoursForm.patchValue({ openingTime: '09:00', closingTime: '18:00' });
    admin.saveHours();
    assert.deepEqual(creates, [
      { day: 2, openingTime: '09:00:00', closingTime: '18:00:00' },
    ]);
    assert.deepEqual(updates, []);
  } finally {
    injector.destroy();
  }
});

test('display helpers preserve closed hours and API error precedence', () => {
  assert.equal(
    openingHoursLabel({
      day: 1,
      openingTime: '09:00:00',
      closingTime: '18:00:00',
    }),
    '09:00 – 18:00',
  );
  assert.equal(
    openingHoursLabel({
      day: 1,
      openingTime: '00:00:00',
      closingTime: '00:00:00',
    }),
    'Closed',
  );
  assert.equal(
    errorMessage(
      new HttpErrorResponse({
        status: 400,
        error: {
          detail: 'Details',
          errors: { email: ['Invalid'] },
          title: 'Title',
        },
      }),
    ),
    'Details',
  );
  assert.equal(
    errorMessage(
      new HttpErrorResponse({
        status: 400,
        error: { errors: { email: ['Invalid'] }, title: 'Title' },
      }),
    ),
    'Invalid',
  );
});

test('deletion cancellation sends no request; confirmation blocks duplicate submissions and refreshes', () => {
  const pending = new Subject<void>();
  const deletions: number[] = [];
  let loads = 0;
  const injector = createInjector({
    cuts: () => of([]),
    hours: () => {
      loads++;
      return of([]);
    },
    users: () => of([]),
    barbers: () => of([]),
    deleteCut: (id) => {
      deletions.push(id);
      return pending;
    },
  });
  try {
    const admin = runInInjectionContext(injector, () => new Admin());
    admin.askDelete(9, 'Classic cut');
    admin.cancelDeletion();
    assert.equal(admin.deletion(), null);
    admin.confirmDelete();
    assert.deepEqual(deletions, []);
    admin.askDelete(9, 'Classic cut');
    admin.confirmDelete();
    admin.confirmDelete();
    admin.cancelDeletion();
    assert.deepEqual(deletions, [9]);
    assert.notEqual(admin.deletion(), null);
    assert.equal(admin.busy(), true);
    pending.next();
    pending.complete();
    assert.equal(admin.deletion(), null);
    assert.equal(admin.busy(), false);
    assert.equal(loads, 2);
  } finally {
    injector.destroy();
  }
});

test('reschedule cancel invalidates pending slots without updating, and confirmation closes and reloads', () => {
  const available = new Subject<string[]>();
  const pending = new Subject<Reservation>();
  const updates: unknown[][] = [];
  let loads = 0;
  const injector = createInjector(
    {
      reservations: () => {
        loads++;
        return of([reservation]);
      },
      barbers: () => of([barber]),
      cuts: () => of([service]),
      slots: () => available,
      updateReservation: (...args) => {
        updates.push(args);
        return pending;
      },
    },
    [
      { provide: Auth, useValue: { claims: () => ({ role: 'Customer' }) } },
      { provide: ActivatedRoute, useValue: routeWith() },
    ],
  );
  try {
    const dashboard = runInInjectionContext(injector, () => new Dashboard());
    dashboard.beginEdit(reservation);
    dashboard.cancelEdit();
    assert.equal(dashboard.editing(), null);
    available.next(['late']);
    assert.deepEqual(dashboard.slots(), []);
    assert.deepEqual(updates, []);
    dashboard.beginEdit(reservation);
    available.next([reservation.startTime]);
    dashboard.selected.set(reservation.startTime);
    dashboard.save(reservation);
    dashboard.save(reservation);
    assert.deepEqual(updates, [[42, { startTime: reservation.startTime }]]);
    dashboard.cancelEdit();
    assert.notEqual(dashboard.editing(), null);
    pending.next(reservation);
    pending.complete();
    assert.equal(dashboard.editing(), null);
    assert.equal(dashboard.busy(), false);
    assert.equal(loads, 2);
  } finally {
    injector.destroy();
  }
});

test('cancel visit dismisses without a request and confirms only once before refreshing', () => {
  const pending = new Subject<void>();
  const cancellations: number[] = [];
  let loads = 0;
  const injector = createInjector(
    {
      reservations: () => {
        loads++;
        return of([reservation]);
      },
      barbers: () => of([barber]),
      cuts: () => of([service]),
      cancelReservation: (id) => {
        cancellations.push(id);
        return pending;
      },
    },
    [
      { provide: Auth, useValue: { claims: () => ({ role: 'Customer' }) } },
      { provide: ActivatedRoute, useValue: routeWith() },
    ],
  );
  try {
    const dashboard = runInInjectionContext(injector, () => new Dashboard());
    dashboard.beginCancellation(reservation);
    dashboard.closeCancellation();
    assert.equal(dashboard.cancelTarget(), null);
    assert.deepEqual(cancellations, []);
    dashboard.beginCancellation(reservation);
    dashboard.cancel(reservation);
    dashboard.cancel(reservation);
    dashboard.closeCancellation();
    assert.deepEqual(cancellations, [42]);
    assert.equal(dashboard.busy(), true);
    assert.equal(dashboard.cancelTarget(), reservation);
    pending.next();
    pending.complete();
    assert.equal(dashboard.cancelTarget(), null);
    assert.equal(dashboard.busy(), false);
    assert.equal(loads, 2);
    assert.equal(dashboard.message(), 'Your visit has been cancelled.');
  } finally {
    injector.destroy();
  }
});

test('cancel visit failure keeps the confirmation open for retry and clears its error on dismissal', () => {
  const injector = createInjector(
    {
      reservations: () => of([reservation]),
      barbers: () => of([barber]),
      cuts: () => of([service]),
      cancelReservation: () =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 400,
              error: { detail: 'Unable to cancel.' },
            }),
        ),
    },
    [
      { provide: Auth, useValue: { claims: () => ({ role: 'Customer' }) } },
      { provide: ActivatedRoute, useValue: routeWith() },
    ],
  );
  try {
    const dashboard = runInInjectionContext(injector, () => new Dashboard());
    dashboard.beginCancellation(reservation);
    dashboard.cancel(reservation);
    assert.equal(dashboard.cancelTarget(), reservation);
    assert.equal(dashboard.busy(), false);
    assert.equal(dashboard.error(), 'Unable to cancel.');
    dashboard.closeCancellation();
    assert.equal(dashboard.cancelTarget(), null);
    assert.equal(dashboard.error(), '');
  } finally {
    injector.destroy();
  }
});
