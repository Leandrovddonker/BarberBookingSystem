import '@angular/compiler';
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  createEnvironmentInjector,
  EnvironmentInjector,
  runInInjectionContext,
} from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';
import { Api } from '../src/app/core/api';
import {
  CreateSpecialOpeningHoursDto,
  SpecialOpeningHours,
  UpdateSpecialOpeningHoursDto,
} from '../src/app/core/models';
import { SpecialOpeningHoursComponent } from '../src/app/pages/special-opening-hours';

const exception: SpecialOpeningHours = {
  id: 7,
  date: '2026-12-24',
  openingTime: '09:00:00',
  closingTime: '15:00:00',
  isClosed: false,
};
const closed: SpecialOpeningHours = {
  id: 8,
  date: '2026-12-25',
  openingTime: null,
  closingTime: null,
  isClosed: true,
};

function setup(
  records: SpecialOpeningHours[] = [],
  overrides: Partial<Api> = {},
) {
  let saved = records.map((record) => ({ ...record }));
  const events: { kind: string; date?: string; id?: number; body?: unknown }[] =
    [];
  const api: Partial<Api> = {
    specialHours: () => {
      events.push({ kind: 'load' });
      return of(saved);
    },
    specialHoursReservationCount: (date) => {
      events.push({ kind: 'count', date });
      return of(0);
    },
    createSpecialHours: (body) => {
      events.push({ kind: 'create', body });
      const result = { id: 99, ...body };
      saved = [...saved, result];
      return of(result);
    },
    updateSpecialHours: (id, body) => {
      events.push({ kind: 'update', id, body });
      const result = {
        ...exception,
        ...saved.find((record) => record.id === id),
        ...body,
        id,
      };
      saved = saved.map((record) => (record.id === id ? result : record));
      return of(result);
    },
    deleteSpecialHours: (id) => {
      events.push({ kind: 'delete', id });
      saved = saved.filter((record) => record.id !== id);
      return of(undefined);
    },
    ...overrides,
  };
  const injector = createEnvironmentInjector(
    [FormBuilder, { provide: Api, useValue: api }],
    null as unknown as EnvironmentInjector,
  );
  const component = runInInjectionContext(
    injector,
    () => new SpecialOpeningHoursComponent(),
  );
  return { component, injector, events };
}

function fillOpenDay(
  component: SpecialOpeningHoursComponent,
  date = exception.date,
) {
  component.add();
  component.form.setValue({
    date,
    isClosed: false,
    openingTime: '09:00',
    closingTime: '15:00',
  });
}

test('loads without writes, sorts DateOnly values, and formats Dutch dates without timezone shifts', () => {
  const { component, injector, events } = setup([closed, exception]);
  try {
    assert.deepEqual(
      component.sortedExceptions().map((record) => record.date),
      ['2026-12-24', '2026-12-25'],
    );
    assert.match(
      component.formatDate('2026-12-25'),
      /vrijdag 25 december 2026/,
    );
    assert.deepEqual(events, [{ kind: 'load' }]);
    assert.equal(component.dialogOpen(), false);
  } finally {
    injector.destroy();
  }
});

test('creates open and closed dates with exact DTOs, only after checking reservations, then refreshes', () => {
  const { component, injector, events } = setup();
  try {
    fillOpenDay(component);
    component.save();
    assert.deepEqual(events.slice(1), [
      { kind: 'count', date: exception.date },
      {
        kind: 'create',
        body: {
          date: exception.date,
          openingTime: '09:00:00',
          closingTime: '15:00:00',
          isClosed: false,
        },
      },
      { kind: 'load' },
    ]);
    assert.equal(component.dialogOpen(), false);
    component.add();
    component.form.patchValue({
      date: closed.date,
      isClosed: true,
      openingTime: '',
      closingTime: '',
    });
    component.toggleClosed();
    assert.equal(component.form.controls.openingTime.disabled, true);
    assert.equal(component.form.controls.openingTime.value, '00:00');
    assert.equal(component.form.controls.closingTime.value, '00:00');
    component.save();
    assert.deepEqual(events.at(-2), {
      kind: 'create',
      body: {
        date: closed.date,
        openingTime: null,
        closingTime: null,
        isClosed: true,
      },
    });
    assert.equal(component.sortedExceptions().length, 2);
  } finally {
    injector.destroy();
  }
});

test('editing preserves the original date for count and sends the date-free update DTO', () => {
  const { component, injector, events } = setup([exception]);
  try {
    component.edit(exception);
    assert.equal(component.form.controls.date.disabled, true);
    component.form.patchValue({ date: '2027-01-01', closingTime: '14:30' });
    component.save();
    assert.deepEqual(events[1], { kind: 'count', date: exception.date });
    assert.deepEqual(events[2], {
      kind: 'update',
      id: 7,
      body: {
        isClosed: false,
        openingTime: '09:00:00',
        closingTime: '14:30:00',
      },
    });
    assert.equal(component.dialogOpen(), false);
    component.edit(closed);
    assert.equal(component.form.controls.openingTime.value, '00:00');
    assert.equal(component.form.controls.closingTime.value, '00:00');
    component.form.controls.isClosed.setValue(false);
    component.toggleClosed();
    assert.equal(component.form.controls.openingTime.enabled, true);
    component.closeDialog();
    component.add();
    assert.equal(component.form.controls.date.enabled, true);
  } finally {
    injector.destroy();
  }
});

test('validates required times, increasing times and local duplicate dates before counting or writing', () => {
  const { component, injector, events } = setup([exception]);
  try {
    fillOpenDay(component);
    component.save();
    assert.match(component.formError(), /already exist/);
    component.form.patchValue({ date: '2026-12-26', openingTime: '' });
    component.save();
    assert.equal(component.form.invalid, true);
    component.form.patchValue({ openingTime: '15:00', closingTime: '15:00' });
    component.save();
    assert.match(component.formError(), /earlier/);
    component.form.patchValue({ openingTime: '16:00' });
    component.save();
    assert.match(component.formError(), /earlier/);
    assert.deepEqual(events, [{ kind: 'load' }]);
  } finally {
    injector.destroy();
  }
});

for (const kind of ['create', 'update', 'delete'] as const) {
  test(`${kind} with appointments requires confirmation before its mutation; cancel writes nothing`, () => {
    const counts = new Subject<number>();
    const { component, injector, events } = setup(
      kind === 'create' ? [] : [exception],
      {
        specialHoursReservationCount: (date) => {
          events.push({ kind: 'count', date });
          return counts;
        },
      },
    );
    try {
      if (kind === 'create') fillOpenDay(component);
      if (kind === 'update') component.edit(exception);
      if (kind === 'delete') component.askDelete(exception);
      const request = () =>
        kind === 'delete' ? component.deleteException() : component.save();
      request();
      request();
      assert.equal(component.busy(), true);
      assert.equal(events.filter((event) => event.kind === 'count').length, 1);
      counts.next(3);
      assert.equal(component.reservationCount(), 3);
      assert.equal(component.pendingAction()?.kind, kind);
      assert.equal(
        events.some((event) => event.kind === kind),
        false,
      );
      component.cancelConfirmation();
      assert.equal(component.pendingAction(), null);
      assert.equal(
        events.some((event) => event.kind === kind),
        false,
      );
      if (kind === 'delete') component.askDelete(exception);
      request();
      counts.next(3);
      component.confirmAction();
      assert.equal(events.filter((event) => event.kind === kind).length, 1);
      assert.equal(component.dialogOpen(), false);
      assert.equal(events.at(-1)?.kind, 'load');
    } finally {
      injector.destroy();
    }
  });
}

test('delete with zero reservations still counts before deleting; dismissing deletion sends nothing', () => {
  const { component, injector, events } = setup([exception]);
  try {
    component.askDelete(exception);
    component.closeDialog();
    assert.deepEqual(events, [{ kind: 'load' }]);
    component.askDelete(exception);
    component.deleteException();
    assert.deepEqual(events.slice(1), [
      { kind: 'count', date: exception.date },
      { kind: 'delete', id: 7 },
      { kind: 'load' },
    ]);
  } finally {
    injector.destroy();
  }
});

test('409 errors explain duplicate dates and retain form values for correction', () => {
  const { component, injector } = setup([], {
    createSpecialHours: () =>
      throwError(
        () =>
          new HttpErrorResponse({ status: 409, error: { title: 'Conflict' } }),
      ),
  });
  try {
    fillOpenDay(component);
    component.save();
    assert.match(
      component.formError(),
      /already exist.*Edit the existing exception/,
    );
    assert.equal(component.form.controls.date.value, exception.date);
    assert.equal(component.dialogOpen(), true);
    assert.equal(component.busy(), false);
  } finally {
    injector.destroy();
  }
});

test('failed count blocks saving and remains retryable; failed list supports retry', () => {
  const { component, injector, events } = setup([], {
    specialHoursReservationCount: () =>
      throwError(() => new HttpErrorResponse({ status: 503 })),
  });
  try {
    fillOpenDay(component);
    component.save();
    assert.notEqual(component.formError(), '');
    assert.equal(component.busy(), false);
    assert.equal(component.dialogOpen(), true);
    assert.equal(
      events.some((event) => event.kind === 'create'),
      false,
    );
  } finally {
    injector.destroy();
  }
  let attempts = 0;
  const retry = setup([], {
    specialHours: () =>
      ++attempts === 1
        ? throwError(() => new HttpErrorResponse({ status: 500 }))
        : of([exception]),
  });
  try {
    assert.notEqual(retry.component.listError(), '');
    retry.component.load();
    assert.equal(retry.component.listError(), '');
    assert.deepEqual(retry.component.exceptions(), [exception]);
  } finally {
    retry.injector.destroy();
  }
});

test('pending writes block duplicate submissions/dismissal; destruction releases count requests', () => {
  const pending = new Subject<SpecialOpeningHours>();
  let writes = 0;
  const { component, injector } = setup([], {
    specialHoursReservationCount: () => of(2),
    createSpecialHours: () => {
      writes++;
      return pending;
    },
  });
  try {
    fillOpenDay(component);
    component.save();
    component.confirmAction();
    component.confirmAction();
    component.save();
    component.closeDialog();
    assert.equal(writes, 1);
    assert.equal(component.dialogOpen(), true);
    assert.equal(component.busy(), true);
    pending.next(exception);
    assert.equal(component.dialogOpen(), false);
  } finally {
    injector.destroy();
  }
  const count = new Subject<number>();
  const second = setup([], { specialHoursReservationCount: () => count });
  fillOpenDay(second.component);
  second.component.save();
  assert.equal(count.observed, true);
  second.injector.destroy();
  assert.equal(count.observed, false);
});

test('API methods match the checked-in controller routes and preserve date/time payloads', () => {
  const requests: { method: string; path: string; value?: unknown }[] = [];
  const http = {
    get: (path: string, options: unknown) => {
      requests.push({ method: 'GET', path, value: options });
      return of([]);
    },
    post: (path: string, body: unknown) => {
      requests.push({ method: 'POST', path, value: body });
      return of(exception);
    },
    put: (path: string, body: unknown) => {
      requests.push({ method: 'PUT', path, value: body });
      return of(exception);
    },
    delete: (path: string) => {
      requests.push({ method: 'DELETE', path });
      return of(undefined);
    },
  };
  const injector = createEnvironmentInjector(
    [Api, { provide: HttpClient, useValue: http }],
    null as unknown as EnvironmentInjector,
  );
  try {
    const api = injector.get(Api);
    const create: CreateSpecialOpeningHoursDto = {
      date: closed.date,
      openingTime: null,
      closingTime: null,
      isClosed: true,
    };
    const update: UpdateSpecialOpeningHoursDto = {
      openingTime: '09:00:00',
      closingTime: '15:00:00',
      isClosed: false,
    };
    api.specialHours();
    api.specialHoursById(7);
    api.createSpecialHours(create);
    api.updateSpecialHours(7, update);
    api.deleteSpecialHours(7);
    api.specialHoursReservationCount(closed.date);
    assert.deepEqual(requests, [
      {
        method: 'GET',
        path: '/api/SpecialOpeningHours',
        value: { params: undefined },
      },
      {
        method: 'GET',
        path: '/api/SpecialOpeningHours/7',
        value: { params: undefined },
      },
      { method: 'POST', path: '/api/SpecialOpeningHours', value: create },
      { method: 'PUT', path: '/api/SpecialOpeningHours/7', value: update },
      { method: 'DELETE', path: '/api/SpecialOpeningHours/7' },
      {
        method: 'GET',
        path: '/api/SpecialOpeningHours/reservation-count',
        value: { params: { date: closed.date } },
      },
    ]);
  } finally {
    injector.destroy();
  }
});

test('past exceptions cannot be edited or saved and past dates cannot be created', () => {
  const past = { ...exception, date: '2000-01-01' };
  const { component, injector, events } = setup([past]);
  try {
    assert.equal(component.isPast(past.date), true);
    component.edit(past);
    assert.equal(component.dialogOpen(), false);
    fillOpenDay(component, past.date);
    component.save();
    assert.match(component.formError(), /past date/);
    component.editor.set({ id: past.id, date: past.date });
    component.save();
    assert.match(component.formError(), /past date/);
    assert.deepEqual(events, [{ kind: 'load' }]);
  } finally {
    injector.destroy();
  }
});

test('today remains editable and bookable as a special-hours date in the shop timezone', () => {
  const { component, injector, events } = setup();
  try {
    const today = component.today();
    assert.equal(component.isPast(today), false);
    fillOpenDay(component, today);
    component.save();
    assert.equal(
      events.some((event) => event.kind === 'create'),
      true,
    );
    component.edit({ ...exception, date: today });
    assert.equal(component.dialogOpen(), true);
  } finally {
    injector.destroy();
  }
});
