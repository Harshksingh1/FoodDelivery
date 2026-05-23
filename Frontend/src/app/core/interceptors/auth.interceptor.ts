import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        const refreshToken = sessionStorage.getItem('refreshToken');
        if (refreshToken) {
          return auth.refreshToken(refreshToken).pipe(
            switchMap(res => {
              const retried = req.clone({ setHeaders: { Authorization: `Bearer ${res.data.accessToken}` } });
              return next(retried);
            }),
            catchError(() => { auth.clearSession(); return throwError(() => err); })
          );
        }
        auth.clearSession();
      }
      return throwError(() => err);
    })
  );
};
