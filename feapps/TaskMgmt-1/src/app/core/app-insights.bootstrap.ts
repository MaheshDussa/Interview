import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { environment } from '../../environments/environment';

export function trackBootstrapError(error: unknown): void {
  const { appInsights } = environment;

  if (typeof window === 'undefined' || !appInsights.enabled || !appInsights.connectionString || appInsights.connectionString.startsWith('YOUR_')) {
    return;
  }

  const client = new ApplicationInsights({
    config: {
      connectionString: appInsights.connectionString,
      disableExceptionTracking: true,
      disableAjaxTracking: true,
      disableFetchTracking: true,
      enableAutoRouteTracking: false
    }
  });

  client.loadAppInsights();
  client.trackException({
    exception: error instanceof Error ? error : new Error(String(error)),
    properties: {
      handledBy: 'bootstrap',
      source: 'main.ts'
    }
  });
  client.flush();
}