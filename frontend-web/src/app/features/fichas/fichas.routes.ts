import { Routes } from '@angular/router';
import { cuidadorGuard, fichaAtivaGuard, fichaGuard } from '../../core/guardas';

/**
 * Rotas das fichas de utentes (F002 e F003) e das pessoas da ficha (F004), dentro do `ShellComponent`. Os parâmetros
 * `fichaId` e `resultado` chegam às páginas como entradas (`withComponentInputBinding`). E12 exige a ficha acessível;
 * E13 exige também o papel Cuidador e a ficha ativa (modelo de classes do frontend web v02, secções 6.1 e 7.1).
 */
export const FICHAS_ROUTES: Routes = [
  {
    path: '',
    title: 'Fichas · DoseCerta',
    loadComponent: () => import('./lista-fichas.page').then((m) => m.ListaFichasPage),
  },
  {
    path: 'nova',
    title: 'Criar ficha · DoseCerta',
    loadComponent: () => import('./nova-ficha.page').then((m) => m.NovaFichaPage),
  },
  {
    path: ':fichaId',
    title: 'Ficha · DoseCerta',
    loadComponent: () => import('./ficha.page').then((m) => m.FichaPage),
  },
  {
    path: ':fichaId/editar',
    title: 'Editar ficha · DoseCerta',
    loadComponent: () => import('./editar-ficha.page').then((m) => m.EditarFichaPage),
  },
  {
    path: ':fichaId/pessoas',
    title: 'Pessoas da ficha · DoseCerta',
    canActivate: [fichaGuard],
    loadComponent: () => import('../pessoas/pessoas-ficha.page').then((m) => m.PessoasFichaPage),
  },
  {
    path: ':fichaId/pessoas/convidar',
    title: 'Convidar pessoa · DoseCerta',
    canActivate: [fichaGuard, cuidadorGuard, fichaAtivaGuard],
    loadComponent: () => import('../pessoas/convidar-pessoa.page').then((m) => m.ConvidarPessoaPage),
  },
];
