import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { UiService } from '../ui/ui.service';

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const uiService = inject(UiService);

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      const message =
        error.error?.message ??
        (typeof error.error === 'string' ? error.error : null) ??
        'Unexpected request error.';

      uiService.showError(message);
      return throwError(() => error);
    })
  );
};
