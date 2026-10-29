import { test } from 'node:test';
import assert from 'node:assert/strict';
import { decodeToken } from '../src/app/core/session.ts';
import { slotTime, appointmentTime, shopDate } from '../src/app/core/time.ts';
const token = (claims) =>
  'header.' +
  Buffer.from(JSON.stringify(claims)).toString('base64url') +
  '.signature';
const exp = Math.floor(Date.now() / 1000) + 3600;
test('reads the actual ASP.NET role and email claim names', () => {
  for (const role of ['Admin', 'Barber', 'Customer']) {
    assert.deepEqual(
      decodeToken(
        token({
          exp,
          'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
          'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress':
            'a@b.com',
        }),
      ),
      { exp, role, email: 'a@b.com' },
    );
  }
});
test('rejects expired, malformed, unknown-role and missing-expiry sessions', () => {
  for (const value of [
    null,
    'bad',
    token({ exp: 1, role: 'Admin' }),
    token({ exp, role: 'Owner' }),
    token({ role: 'Customer' }),
    token({ exp: 'future', role: 'Admin' }),
  ])
    assert.equal(decodeToken(value), null);
});
test('renders shop time through winter and summer DST changes', () => {
  assert.equal(slotTime('2026-01-15T09:00:00Z'), '10:00');
  assert.equal(slotTime('2026-07-15T09:00:00Z'), '11:00');
  assert.equal(slotTime('2026-03-29T00:30:00Z'), '01:30');
  assert.equal(slotTime('2026-03-29T01:30:00Z'), '03:30');
  assert.match(appointmentTime('2026-07-15T09:00:00Z'), /11:00/);
  assert.match(shopDate(), /^\d{4}-\d{2}-\d{2}$/);
});
