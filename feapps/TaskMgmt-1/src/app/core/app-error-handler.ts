import { ErrorHandler, Injectable, inject } from '@angular/core';
import { AppInsightsService } from './app-insights.service';

@Injectable()
export class AppErrorHandler implements ErrorHandler {
  private readonly appInsights = inject(AppInsightsService);

  handleError(error: unknown): void {
    const original = this.unwrap(error);
    this.appInsights.trackCodeError(original, {
      source: 'global-error-handler'
    });

    console.error(original);
  }

  private unwrap(error: unknown): unknown {
    if (typeof error === 'object' && error && 'rejection' in error) {
      return (error as { rejection?: unknown }).rejection ?? error;
    }

    if (typeof error === 'object' && error && 'ngOriginalError' in error) {
      return (error as { ngOriginalError?: unknown }).ngOriginalError ?? error;
    }

    return error;
  }
}