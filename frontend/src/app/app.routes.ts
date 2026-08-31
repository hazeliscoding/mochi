import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'overview' },
  { path: 'overview', loadComponent: () => import('./pages/overview').then(m => m.Overview), title: 'Overview · Mochi' },
  { path: 'realtime', loadComponent: () => import('./pages/realtime').then(m => m.Realtime), title: 'Realtime · Mochi' },
  { path: 'pages', loadComponent: () => import('./pages/pages').then(m => m.Pages), title: 'Pages · Mochi' },
  { path: 'sources', loadComponent: () => import('./pages/sources').then(m => m.Sources), title: 'Traffic sources · Mochi' },
  { path: 'geography', loadComponent: () => import('./pages/geography').then(m => m.Geography), title: 'Geography · Mochi' },
  { path: 'devices', loadComponent: () => import('./pages/devices').then(m => m.Devices), title: 'Devices · Mochi' },
  { path: 'events', loadComponent: () => import('./pages/events').then(m => m.Events), title: 'Events · Mochi' },
  { path: 'goals', loadComponent: () => import('./pages/goals').then(m => m.Goals), title: 'Goals · Mochi' },
  { path: 'websites', loadComponent: () => import('./pages/websites').then(m => m.Websites), title: 'Websites · Mochi' },
  { path: 'add-website', loadComponent: () => import('./pages/add-website').then(m => m.AddWebsite), title: 'Add website · Mochi' },
  { path: 'privacy', loadComponent: () => import('./pages/privacy').then(m => m.Privacy), title: 'Privacy center · Mochi' },
  { path: 'settings', loadComponent: () => import('./pages/settings').then(m => m.Settings), title: 'Website settings · Mochi' },
  { path: '**', redirectTo: 'overview' },
];
