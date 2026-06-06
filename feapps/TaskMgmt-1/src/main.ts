import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { trackBootstrapError } from './app/core/app-insights.bootstrap';

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => {
    trackBootstrapError(err);
    console.error(err);
  });
