import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { provideHttpClient } from '@angular/common/http';
import { AuthGuardService } from './app/AuthGuardService';

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
