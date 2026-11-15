import { campo, criarContaNaApi, novaConta } from '../support/e2e';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v01.md (F001).
describe('F001 Registo (E01)', () => {
  it('TS-W-F001-01 cria a conta com dados válidos (RF-010)', () => {
    const conta = novaConta('registo');
    cy.intercept('POST', '/api/v1/auth/registo').as('registo');
    cy.visit('/registo');
    campo('Nome').type(conta.nome);
    campo('E-mail').type(conta.email);
    campo('Palavra-passe').type(conta.palavraPasse, { log: false });
    cy.contains('button', 'Criar conta').click();

    cy.wait('@registo').its('response.statusCode').should('eq', 201);
    cy.location('pathname').should('eq', '/entrar');
    cy.contains('[role="status"]', 'Conta criada. Inicie sessão para continuar.');
  });

  it('TS-W-F001-02 recusa um e-mail já registado (RF-011)', () => {
    const conta = novaConta('repetido');
    criarContaNaApi(conta);
    cy.intercept('POST', '/api/v1/auth/registo').as('registo');
    cy.visit('/registo');
    campo('Nome').type('Bruno Fictício');
    campo('E-mail').type(conta.email);
    campo('Palavra-passe').type(conta.palavraPasse, { log: false });
    cy.contains('button', 'Criar conta').click();

    cy.wait('@registo').its('response.statusCode').should('eq', 409);
    cy.location('pathname').should('eq', '/registo');
    cy.contains('Já existe uma conta com este e-mail.');
  });
});
