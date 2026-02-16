import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthSessionStore } from '../auth/auth-session.store';

export const authGuard: CanActivateFn = () => {
  const authSessionStore = inject(AuthSessionStore);
  const router = inject(Router);

  return authSessionStore.isAuthenticated() ? true : router.createUrlTree(['/auth/login']);
};
