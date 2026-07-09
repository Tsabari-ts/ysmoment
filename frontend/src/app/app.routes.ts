import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/landing/landing.component').then(m => m.LandingComponent) },
  { path: 'login', loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent) },
  { path: 'e/:slug', loadComponent: () => import('./pages/guest-order/guest-order.component').then(m => m.GuestOrderComponent) },
  { path: 'privacy-policy', loadComponent: () => import('./pages/privacy-policy/privacy-policy.component').then(m => m.PrivacyPolicyComponent) },
  { path: 'terms-of-use', loadComponent: () => import('./pages/terms-of-use/terms-of-use.component').then(m => m.TermsOfUseComponent) },
  { path: 'accessibility', loadComponent: () => import('./pages/accessibility-statement/accessibility-statement.component').then(m => m.AccessibilityStatementComponent) },
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
