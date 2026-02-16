import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import es from '@angular/common/locales/es';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { es_ES, provideNzI18n } from 'ng-zorro-antd/i18n';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { kanbanReducer } from './features/kanban/store/kanban.reducer';
import { KanbanEffects } from './features/kanban/store/kanban.effects';

registerLocaleData(es);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideStore({ kanban: kanbanReducer }),
    provideEffects([KanbanEffects]),
    provideAnimations(),
    { provide: LOCALE_ID, useValue: 'es' },
    provideNzI18n(es_ES)
  ]
};
