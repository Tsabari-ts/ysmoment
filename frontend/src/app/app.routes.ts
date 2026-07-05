import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent) },
  { path: 'e/:slug', loadComponent: () => import('./pages/guest-order/guest-order.component').then(m => m.GuestOrderComponent) },
  {
    path: 'admin/events/new',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/create-event/create-event.component').then(m => m.CreateEventComponent)
  },
  {
    path: 'admin/events/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'admin/events/:id/summary',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/event-summary/event-summary.component').then(m => m.EventSummaryComponent)
  }
];
