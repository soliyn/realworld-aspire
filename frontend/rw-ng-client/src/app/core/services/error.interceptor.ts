import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle authentication errors (401 Unauthorized or 403 Forbidden)
      if (error.status === 401 || error.status === 403) {
        // Token is invalid or expired, logout the user
        authService.logout();
      }

      // Re-throw the error so components can still handle it if needed
      return throwError(() => error);
    })
  );
};
