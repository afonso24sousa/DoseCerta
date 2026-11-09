import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Rota inicial depois de iniciar sessão. */
export const ROTA_INICIAL = '/lista-diaria';

/** Exige sessão iniciada; sem sessão vai para o início de sessão (RF-017). */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.autenticado() ? true : inject(Router).createUrlTree(['/entrar']);
};

/** Quem já tem sessão não vê o registo nem o início de sessão. */
export const visitanteGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.autenticado() ? inject(Router).parseUrl(ROTA_INICIAL) : true;
};
