import '@angular/compiler';
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  createEnvironmentInjector,
  runInInjectionContext,
  EnvironmentInjector,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { of, Subject } from 'rxjs';
import { Api } from '../src/app/core/api';
import { Auth } from '../src/app/core/auth';
import { Dashboard } from '../src/app/pages/dashboard';

test('availability query omits exclusion for creation and includes it for editing', () => {
  const requests: { path: string; params: Record<string, string | number> }[] =
    [];
  const slots = ['2030-01-07T09:00:00Z'];
  const injector = createEnvironmentInjector(
    [
      Api,
      {
        provide: HttpClient,
        useValue: {
          get: (
            path: string,
            options: { params: Record<string, string | number> },
          ) => {
            requests.push({ path, params: options.params });
            return of(slots);
          },
        },
      },
    ],
    null as unknown as EnvironmentInjector,
  );
  try {
    const api = injector.get(Api);
    api
      .slots(2, 3, '2030-01-07')
      .subscribe((result) => assert.deepEqual(result, slots));
    assert.deepEqual(requests[0], {
      path: '/api/Reservation/available-slots',
      params: { barberId: 2, haircutId: 3, date: '2030-01-07' },
    });
    api
      .slots(2, 3, '2030-01-07', 42)
      .subscribe((result) => assert.deepEqual(result, slots));
    assert.deepEqual(requests[1], {
      path: '/api/Reservation/available-slots',
      params: {
        barberId: 2,
        haircutId: 3,
        date: '2030-01-07',
        excludeReservationId: 42,
      },
    });
  } finally {
    injector.destroy();
  }
});

test('dashboard passes edited reservation ID on refresh and ignores stale responses', () => {
  const calls: unknown[][] = [],
    responses: Subject<string[]>[] = [];
  const api = {
    reservations: () => of([]),
    barbers: () => of([]),
    cuts: () => of([]),
    slots: (...args: unknown[]) => {
      calls.push(args);
      const response = new Subject<string[]>();
      responses.push(response);
      return response;
    },
  };
  const injector = createEnvironmentInjector(
    [
      { provide: Api, useValue: api },
      { provide: Auth, useValue: { claims: () => ({ role: 'Customer' }) } },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { queryParamMap: { has: () => false } } },
      },
      FormBuilder,
    ],
    null as unknown as EnvironmentInjector,
  );
  try {
    const dashboard = runInInjectionContext(injector, () => new Dashboard());
    const reservation = {
      id: 42,
      barberId: 2,
      hairCutId: 3,
      userId: 7,
      startTime: '2030-01-07T09:00:00Z',
    };
    dashboard.beginEdit(reservation);
    assert.deepEqual(calls[0], [2, 3, dashboard.today, 42]);
    dashboard.selected.set('old selection');
    dashboard.editForm.setValue({ date: '2099-01-07' });
    dashboard.loadSlots(reservation);
    assert.equal(dashboard.selected(), '');
    assert.deepEqual(calls[1], [2, 3, '2099-01-07', 42]);
    responses[0].next(['stale']);
    assert.deepEqual(dashboard.slots(), []);
    responses[1].next(['2099-01-07T09:00:00Z']);
    assert.deepEqual(dashboard.slots(), ['2099-01-07T09:00:00Z']);
    dashboard.closeEdit();
    responses[1].next(['late']);
    assert.deepEqual(dashboard.slots(), []);
  } finally {
    injector.destroy();
  }
});
