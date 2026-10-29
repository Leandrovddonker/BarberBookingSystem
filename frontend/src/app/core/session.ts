import type { Role } from './models';

export interface Claims {
  exp: number;
  role: Role;
  email: string;
}

export function decodeToken(token: string | null): Claims | null {
  try {
    if (!token) return null;
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const claims: Record<string, unknown> = JSON.parse(atob(payload));
    const role =
      claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
      claims['role'];
    const expiration = claims['exp'];
    if (
      typeof expiration !== 'number' ||
      !Number.isFinite(expiration) ||
      expiration * 1000 <= Date.now() ||
      !isRole(role)
    ) {
      return null;
    }
    const email =
      claims[
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
      ] ??
      claims['email'] ??
      '';
    return {
      exp: expiration,
      role,
      email: typeof email === 'string' ? email : '',
    };
  } catch {
    return null;
  }
}

function isRole(value: unknown): value is Role {
  return value === 'Customer' || value === 'Barber' || value === 'Admin';
}
