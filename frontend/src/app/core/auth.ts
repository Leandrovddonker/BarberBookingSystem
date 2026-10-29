import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { Api } from './api';
import { decodeToken } from './session';

const SESSION_KEY = 'barber.jwt';

@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly tokenValue = signal<string | null>(
    sessionStorage.getItem(SESSION_KEY),
  );
  private readonly sessionClaims = signal(decodeToken(this.tokenValue()));
  readonly claims = this.sessionClaims.asReadonly();

  token(): string | null {
    if (!decodeToken(this.tokenValue())) {
      this.clear();
      return null;
    }
    return this.tokenValue();
  }

  login(email: string, password: string) {
    return this.api.login(email, password).pipe(
      tap((response) => {
        const claims = decodeToken(response.token);
        if (!claims) throw new Error('Invalid token');
        sessionStorage.setItem(SESSION_KEY, response.token);
        this.tokenValue.set(response.token);
        this.sessionClaims.set(claims);
      }),
    );
  }

  clear(): void {
    sessionStorage.removeItem(SESSION_KEY);
    this.tokenValue.set(null);
    this.sessionClaims.set(null);
  }

  logout(): void {
    this.clear();
    void this.router.navigate(['/']);
  }
}
