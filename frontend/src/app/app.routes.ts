import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'auth/login',
    loadComponent: () => import('./features/auth/pages/login/login.page').then((m) => m.LoginPage)
  },
  {
    path: 'auth/register',
    loadComponent: () =>
      import('./features/auth/pages/register/register.page').then((m) => m.RegisterPage)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell.page').then((m) => m.ShellPage),
    children: [
      {
        path: 'kanban',
        loadComponent: () => import('./features/kanban/pages/kanban/kanban.page').then((m) => m.KanbanPage)
      },
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'kanban'
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
