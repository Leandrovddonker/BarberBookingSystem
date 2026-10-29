import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { Api } from '../core/api';
import { Auth } from '../core/auth';
import { errorMessage } from '../core/display';

@Component({
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './auth-page.html',
})
export class AuthPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(Api);
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly register = this.route.snapshot.routeConfig?.path === 'register';
  readonly returnUrl =
    this.route.snapshot.queryParamMap.get('returnUrl') || '/dashboard';
  readonly busy = signal(false);
  readonly error = signal('');
  readonly invalid = signal(false);
  readonly form = this.formBuilder.nonNullable.group({
    name: ['', this.register ? Validators.required : []],
    phoneNumber: ['', this.register ? Validators.required : []],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  submit(): void {
    this.invalid.set(this.form.invalid);
    if (this.form.invalid || this.busy()) return;

    this.busy.set(true);
    this.error.set('');
    const credentials = this.form.getRawValue();
    const request = this.register
      ? this.api
          .register(credentials)
          .pipe(
            switchMap(() =>
              this.auth.login(credentials.email, credentials.password),
            ),
          )
      : this.auth.login(credentials.email, credentials.password);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => void this.router.navigateByUrl(this.safeReturnUrl()),
      error: (error) => {
        this.error.set(errorMessage(error));
        this.busy.set(false);
      },
    });
  }

  private safeReturnUrl(): string {
    return this.returnUrl.startsWith('/') && !this.returnUrl.startsWith('//')
      ? this.returnUrl
      : '/dashboard';
  }
}
