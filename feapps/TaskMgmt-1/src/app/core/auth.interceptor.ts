import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AppInsightsService } from './app-insights.service';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const appInsights = inject(AppInsightsService);

  if (!auth.shouldAttachToken(req.url)) {
    return next(req);
  }

  return from(auth.acquireAccessToken()).pipe(
    catchError(err => {
      appInsights.trackCodeError(err, {
        source: 'auth-token-acquisition',
        url: req.url,
        method: req.method
      });
      return throwError(() => err);
    }),
    switchMap(token => {
      const authReq = token
        ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
        : req;

      return next(authReq).pipe(
        catchError(err => {
          appInsights.trackApiFailure(authReq.url, err?.status ?? 0, err?.message || 'HTTP request failed', {
            method: authReq.method,
            source: 'http-interceptor'
          });
          return throwError(() => err);
        })
      );
    })
  );
};
