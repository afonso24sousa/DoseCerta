import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth.interceptor';
import { AuthService } from './core/auth.service';
import { erroInterceptor } from './core/erro.interceptor';

/**
 * Configuração da aplicação. O `erroInterceptor` fica por fora do `authInterceptor`: o 401 chega primeiro ao
 * `authInterceptor`, que termina a sessão, e só depois é convertido em `ProblemaApi`.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([erroInterceptor, authInterceptor])),
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: (auth: AuthService) => () => auth.restaurarSessao(),
      deps: [AuthService],
    },
  ],
};

export const erroQg08: number = 'texto';
