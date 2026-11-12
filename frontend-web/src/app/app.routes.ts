import { Routes } from '@angular/router';
import { authGuard, visitanteGuard } from './core/guardas';
import { InicioSessaoPage } from './features/conta/inicio-sessao.page';
import { ShellComponent } from './layout/shell.component';

const emConstrucao = () =>
  import('./features/em-construcao/em-construcao.page').then((m) => m.EmConstrucaoPage);

/**
 * Rotas do frontend web. E02 é carregada no pacote inicial (RNF-019); as restantes páginas com `loadComponent`. As
 * rotas autenticadas ficam dentro do `ShellComponent`, protegidas pelo `authGuard` (RF-017).
 */
export const routes: Routes = [
  {
    path: 'registo',
    title: 'Criar conta · DoseCerta',
    canActivate: [visitanteGuard],
    loadComponent: () => import('./features/conta/registo.page').then((m) => m.RegistoPage),
  },
  {
    path: 'entrar',
    title: 'Iniciar sessão · DoseCerta',
    canActivate: [visitanteGuard],
    component: InicioSessaoPage,
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'lista-diaria' },
      {
        path: 'conta',
        title: 'Conta · DoseCerta',
        loadComponent: () => import('./features/conta/conta.page').then((m) => m.ContaPage),
      },
      {
        path: 'lista-diaria',
        title: 'Lista diária · DoseCerta',
        data: { titulo: 'Lista diária' },
        loadComponent: emConstrucao,
      },
      {
        path: 'fichas',
        title: 'Fichas · DoseCerta',
        data: { titulo: 'Fichas' },
        loadComponent: emConstrucao,
      },
      {
        path: 'agenda',
        title: 'Agenda · DoseCerta',
        data: { titulo: 'Agenda' },
        loadComponent: emConstrucao,
      },
      {
        path: 'convites',
        title: 'Convites · DoseCerta',
        data: { titulo: 'Convites' },
        loadComponent: emConstrucao,
      },
      {
        path: 'notificacoes',
        title: 'Notificações · DoseCerta',
        data: { titulo: 'Notificações' },
        loadComponent: emConstrucao,
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
