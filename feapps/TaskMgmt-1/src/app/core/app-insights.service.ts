import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { filter } from 'rxjs';
import { environment } from '../../environments/environment';

type LogLevel = 'error' | 'warning' | 'info';

@Injectable({ providedIn: 'root' })
export class AppInsightsService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly browser = isPlatformBrowser(this.platformId);
  private readonly config = environment.appInsights;
  private appInsights: ApplicationInsights | null = null;
  private initialized = false;
  private routerTrackingStarted = false;

  initialize(router: Router): void {
    if (!this.isEnabled() || this.initialized) {
      return;
    }

    this.appInsights = new ApplicationInsights({
      config: {
        connectionString: this.config.connectionString,
        disableExceptionTracking: true,
        disableAjaxTracking: true,
        disableFetchTracking: true,
        enableAutoRouteTracking: false
      }
    });

    this.appInsights.loadAppInsights();
    this.initialized = true;
    this.setCloudRole();
    this.startRouterTracking(router);
  }

  trackPageView(name: string, uri: string): void {
    if (!this.canTrack()) {
      return;
    }

    this.appInsights!.trackPageView({ name, uri });
  }

  trackCodeError(error: unknown, properties: Record<string, unknown> = {}): void {
    if (!this.canTrack() || !this.shouldTrack(error, properties)) {
      return;
    }

    this.appInsights!.trackException({
      exception: this.normalizeError(error),
      properties: this.stringifyProperties({
        handledBy: 'app-code',
        ...properties
      })
    });
  }

  trackApiFailure(url: string, status: number, message: string, properties: Record<string, unknown> = {}): void {
    if (!this.canTrack()) {
      return;
    }

    this.appInsights!.trackException({
      exception: new Error(message),
      properties: this.stringifyProperties({
        handledBy: 'api-client',
        kind: 'http-error',
        url,
        status,
        ...properties
      })
    });
  }

  trackTrace(message: string, level: LogLevel = 'info', properties: Record<string, unknown> = {}): void {
    if (!this.canTrack()) {
      return;
    }

    this.appInsights!.trackTrace({
      message,
      severityLevel: this.toSeverity(level),
      properties: this.stringifyProperties(properties)
    });
  }

  isEnabled(): boolean {
    return this.browser && this.config.enabled && this.isConfigured(this.config.connectionString);
  }

  private canTrack(): boolean {
    return this.isEnabled() && !!this.appInsights;
  }

  private startRouterTracking(router: Router): void {
    if (this.routerTrackingStarted) {
      return;
    }

    this.routerTrackingStarted = true;
    router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(event => {
      const navigation = event as NavigationEnd;
      this.trackPageView(document.title || navigation.urlAfterRedirects, navigation.urlAfterRedirects);
    });
  }

  private shouldTrack(error: unknown, properties: Record<string, unknown>): boolean {
    if (typeof properties['source'] === 'string' && properties['source'] !== 'global-error-handler') {
      return true;
    }

    const normalized = this.normalizeError(error);
    const text = `${normalized.message}\n${normalized.stack ?? ''}`.toLowerCase();

    if (!text.trim()) {
      return false;
    }

    if (text.includes('node_modules') || text.includes('zone.js') || text.includes('@angular')) {
      return false;
    }

    return text.includes('src/app')
      || text.includes('src/main')
      || text.includes('tasks.component')
      || text.includes('login.component')
      || text.includes('auth.service')
      || text.includes('task.service')
      || text.includes('auth.interceptor');
  }

  private normalizeError(error: unknown): Error {
    if (error instanceof Error) {
      return error;
    }

    if (typeof error === 'string') {
      return new Error(error);
    }

    return new Error('Unknown application error');
  }

  private setCloudRole(): void {
    if (!this.canTrack()) {
      return;
    }

    this.appInsights!.addTelemetryInitializer(envelope => {
      envelope.tags = envelope.tags ?? [];
      envelope.tags['ai.cloud.role'] = this.config.cloudRole;
    });
  }

  private stringifyProperties(properties: Record<string, unknown>): Record<string, string> {
    return Object.entries(properties).reduce<Record<string, string>>((result, [key, value]) => {
      if (value !== undefined && value !== null) {
        result[key] = typeof value === 'string' ? value : JSON.stringify(value);
      }
      return result;
    }, {});
  }

  private isConfigured(value: string | null | undefined): boolean {
    return !!value && !value.startsWith('YOUR_');
  }

  private toSeverity(level: LogLevel): number {
    switch (level) {
      case 'error':
        return 3;
      case 'warning':
        return 2;
      default:
        return 1;
    }
  }
}