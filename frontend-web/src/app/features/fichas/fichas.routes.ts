import { Routes } from '@angular/router';
import { cuidadorGuard, fichaAtivaGuard, fichaGuard } from '../../core/guardas';

/**
 * Rotas das fichas de utentes (F002 e F003), das pessoas da ficha (F004) e da medicação (F005) e dos registos (F009), dentro do
 * `ShellComponent`. Os parâmetros `fichaId`, `tratamentoId` e `resultado` chegam às páginas como entradas
 * (`withComponentInputBinding`). E12, E15, E17 e E23 exigem a ficha acessível; E13, E16 e E24 exigem também o papel Cuidador e a
 * ficha ativa (modelo de classes do frontend web v02, secções 6.1 e 7.1).
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
  {
    path: ':fichaId/registos',
    title: 'Ocorrências e notas · DoseCerta',
    canActivate: [fichaGuard],
    loadComponent: () => import('../registos/registos-ficha.page').then((m) => m.RegistosFichaPage),
  },
  {
    path: ':fichaId/registos/novo',
    title: 'Novo registo · DoseCerta',
    canActivate: [fichaGuard, cuidadorGuard, fichaAtivaGuard],
    loadComponent: () => import('../registos/novo-registo.page').then((m) => m.NovoRegistoPage),
  },
  {
    path: ':fichaId/medicacao',
    title: 'Medicação · DoseCerta',
    canActivate: [fichaGuard],
    loadComponent: () => import('../medicacao/lista-tratamentos.page').then((m) => m.ListaTratamentosPage),
  },
  {
    path: ':fichaId/medicacao/novo',
    title: 'Novo tratamento · DoseCerta',
    canActivate: [fichaGuard, cuidadorGuard, fichaAtivaGuard],
    loadComponent: () => import('../medicacao/form-tratamento.page').then((m) => m.FormTratamentoPage),
  },
  {
    path: ':fichaId/medicacao/:tratamentoId',
    title: 'Tratamento · DoseCerta',
    canActivate: [fichaGuard],
    loadComponent: () => import('../medicacao/detalhe-tratamento.page').then((m) => m.DetalheTratamentoPage),
  },
  {
    path: ':fichaId/medicacao/:tratamentoId/editar',
    title: 'Editar tratamento · DoseCerta',
    canActivate: [fichaGuard, cuidadorGuard, fichaAtivaGuard],
    loadComponent: () => import('../medicacao/form-tratamento.page').then((m) => m.FormTratamentoPage),
  },
];
