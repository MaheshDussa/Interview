import { APP_INITIALIZER, ApplicationConfig, ErrorHandler, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { AuthService } from './core/auth.service';
import { AppErrorHandler } from './core/app-error-handler';
import { AppInsightsService } from './core/app-insights.service';
import { authInterceptor } from './core/auth.interceptor';

function initializeAuth(authService: AuthService): () => Promise<void> {
  return () => authService.initialize();
}

function initializeAppInsights(appInsights: AppInsightsService, router: Router): () => void {
  return () => appInsights.initialize(router);
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    {
      provide: ErrorHandler,
      useClass: AppErrorHandler
    },
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: initializeAuth,
      deps: [AuthService]
    },
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: initializeAppInsights,
      deps: [AppInsightsService, Router]
    }
  ]
};
