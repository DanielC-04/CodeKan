import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { AuthSessionStore } from '../auth/auth-session.store';

const AUTH_ROUTES = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh', '/api/auth/revoke'];

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const sessionStore = inject(AuthSessionStore);

  const shouldSkipAuth = AUTH_ROUTES.some((route) => request.url.includes(route));
  const accessToken = sessionStore.accessToken();

  const authRequest = !shouldSkipAuth && accessToken
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${accessToken}`
        }
      })
    : request;

  return next(authRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || shouldSkipAuth) {
        return throwError(() => error);
      }

      return authService.refresh().pipe(
        switchMap((refreshResponse) => {
          const retriedRequest = request.clone({
            setHeaders: {
              Authorization: `Bearer ${refreshResponse.data.accessToken}`
            }
          });

          return next(retriedRequest);
        }),
        catchError((refreshError) => {
          sessionStore.clearSession();
          return throwError(() => refreshError);
        })
      );
    })
  );
};
