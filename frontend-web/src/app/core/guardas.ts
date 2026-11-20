import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router, UrlTree } from '@angular/router';
import { Observable, catchError, map, of } from 'rxjs';
import { ResultadoFicha } from '../features/fichas/resultados-ficha';
import { FichaDto } from '../models/fichas.models';
import { ProblemaApi } from '../models/problema-api';
import { AuthService } from './auth.service';
import { FichaContextoService } from './ficha-contexto.service';

/** Rota inicial depois de iniciar sessão: E04, `/hoje` (modelo de classes do frontend web v02, secção 7.1). */
export const ROTA_INICIAL = '/hoje';

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

/**
 * Carrega a ficha da rota (OP-10) no {@link FichaContextoService}. Quem não está associado recebe 404 do servidor
 * (RNF-005) e volta à lista com o aviso. Outros erros (por exemplo sem ligação) deixam abrir a página, que os mostra.
 */
export const fichaGuard: CanActivateFn = (rota) => {
  const fichaId = idDaFicha(rota);
  return verificar(inject(FichaContextoService).carregar(fichaId), () => true);
};

/**
 * Exige o papel Cuidador na ficha (Cuidador principal e Cuidador têm as mesmas permissões, RF-047); um Familiar volta
 * a E08 com o aviso de que só pode consultar (RF-048).
 */
export const cuidadorGuard: CanActivateFn = (rota) => {
  const fichaId = idDaFicha(rota);
  const router = inject(Router);
  return verificar(inject(FichaContextoService).obter(fichaId), (ficha) =>
    ficha.meuPapel === 'Cuidador' ? true : paraFicha(router, fichaId, ResultadoFicha.soConsultaFamiliar),
  );
};

/**
 * Exige ficha ativa nos ecrãs de registo e alteração; numa ficha arquivada volta a E08 com o aviso de que só pode ser
 * consultada (RF-120, RF-122). Não se aplica à resposta a convites, que continua permitida.
 */
export const fichaAtivaGuard: CanActivateFn = (rota) => {
  const fichaId = idDaFicha(rota);
  const router = inject(Router);
  return verificar(inject(FichaContextoService).obter(fichaId), (ficha) =>
    ficha.estado === 'Ativa' ? true : paraFicha(router, fichaId, ResultadoFicha.soConsultaArquivada),
  );
};

/** `fichaId` da rota ou de uma rota acima (as rotas da ficha estão sob `/fichas/:fichaId`). */
function idDaFicha(rota: ActivatedRouteSnapshot): string {
  return rota.pathFromRoot.map((r) => r.paramMap.get('fichaId')).find((id) => id !== null) ?? '';
}

function paraFicha(router: Router, fichaId: string, resultado: string): UrlTree {
  return router.createUrlTree(['/fichas', fichaId], { queryParams: { resultado } });
}

function verificar(
  pedido: Observable<FichaDto>,
  decidir: (ficha: FichaDto) => boolean | UrlTree,
): Observable<boolean | UrlTree> {
  const router = inject(Router);
  return pedido.pipe(
    map(decidir),
    catchError((problema: ProblemaApi) =>
      of(
        problema.codigo === 'FICHA_NAO_ENCONTRADA'
          ? router.createUrlTree(['/fichas'], { queryParams: { resultado: ResultadoFicha.semAcesso } })
          : true,
      ),
    ),
  );
}
