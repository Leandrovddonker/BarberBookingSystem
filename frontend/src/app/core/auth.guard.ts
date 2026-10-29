import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from './auth';

export const roleGuard: CanActivateFn = (route, state) => {
  const auth = inject(Auth);
  const router = inject(Router);
  if (!auth.token()) {
    return router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url },
    });
  }
  const roles = route.data['roles'] as readonly string[];
  return (
    roles.includes(auth.claims()!.role) || router.createUrlTree(['/dashboard'])
  );
};
