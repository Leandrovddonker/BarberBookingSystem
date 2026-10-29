import { Routes } from '@angular/router';
import { roleGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home').then((m) => m.Home) },
  {
    path: 'login',
    loadComponent: () => import('./pages/auth-page').then((m) => m.AuthPage),
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/auth-page').then((m) => m.AuthPage),
  },
  {
    path: 'book',
    canActivate: [roleGuard],
    data: { roles: ['Customer'] },
    loadComponent: () => import('./pages/booking').then((m) => m.Booking),
  },
  {
    path: 'dashboard',
    canActivate: [roleGuard],
    data: { roles: ['Customer', 'Barber', 'Admin'] },
    loadComponent: () => import('./pages/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'admin',
    canActivate: [roleGuard],
    data: { roles: ['Admin'] },
    loadComponent: () => import('./pages/admin').then((m) => m.Admin),
  },
  { path: '**', redirectTo: '' },
];
