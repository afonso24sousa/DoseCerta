import { Routes } from '@angular/router';

/**
 * Rotas das fichas de utentes (F002 e F003), dentro do `ShellComponent`. Os parâmetros `fichaId` e `resultado` chegam
 * às páginas como entradas (`withComponentInputBinding`).
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
];
