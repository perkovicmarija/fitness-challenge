import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./leaderboard/leaderboard').then(m => m.Leaderboard),
    title: 'Leaderboard',
  },
  {
    path: 'me',
    loadComponent: () => import('./dashboard/dashboard').then(m => m.DashboardPage),
    title: 'My dashboard',
  },
  {
    path: 'log',
    loadComponent: () => import('./log-activity/log-activity').then(m => m.LogActivity),
    title: 'Log activity',
  },
  {
    path: 'coach',
    loadComponent: () => import('./coach/coach').then(m => m.Coach),
    title: 'AI coach',
  },
  { path: '**', redirectTo: '' },
];
