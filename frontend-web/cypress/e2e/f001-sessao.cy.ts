import { campo, criarContaNaApi, iniciarSessao, novaConta } from '../support/e2e';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v01.md (F001).
describe('F001 Início e fim de sessão (E02)', () => {
  it('TS-W-F001-03 inicia sessão com credenciais válidas (RF-012)', () => {
    const conta = novaConta('login');
    criarContaNaApi(conta);
    iniciarSessao(conta.email, conta.palavraPasse);

    cy.contains('.cabecalho__utilizador', conta.nome);
    cy.window().its('sessionStorage').invoke('getItem', 'dosecerta.sessao').should('not.be.empty');
  });

  it('TS-W-F001-04 recusa credenciais inválidas sem dizer qual está errada (RF-012)', () => {
    const conta = novaConta('invalido');
    criarContaNaApi(conta);
    cy.intercept('POST', '/api/v1/auth/login').as('login');
    cy.visit('/entrar');
    campo('E-mail').type(conta.email);
    campo('Palavra-passe').type('Errada123', { log: false });
    cy.contains('button', 'Entrar').click();

    cy.wait('@login').its('response.statusCode').should('eq', 401);
    cy.location('pathname').should('eq', '/entrar');
    cy.contains('[role="alert"]', 'E-mail ou palavra-passe incorretos.');
  });

  it('TS-W-F001-07 termina a sessão e exige sessão para as páginas autenticadas (RF-016, RF-017)', () => {
    const conta = novaConta('logout');
    criarContaNaApi(conta);
    iniciarSessao(conta.email, conta.palavraPasse);
    cy.contains('.cabecalho button', 'Terminar sessão').click();

    cy.location('pathname').should('eq', '/entrar');
    cy.window().its('sessionStorage').invoke('getItem', 'dosecerta.sessao').should('be.null');
    cy.visit('/conta');
    cy.location('pathname').should('eq', '/entrar');
  });
});
