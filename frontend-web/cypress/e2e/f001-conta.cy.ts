import { campo, criarContaNaApi, iniciarSessao, novaConta } from '../support/e2e';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v01.md (F001).
describe('F001 Conta (E03)', () => {
  it('TS-W-F001-05 consulta e atualiza o nome e o e-mail da conta (RF-013, RF-014)', () => {
    const conta = novaConta('conta');
    const novoEmail = novaConta('conta-nova').email;
    criarContaNaApi(conta);
    iniciarSessao(conta.email, conta.palavraPasse);
    cy.visit('/conta');
    campo('Nome').should('have.value', conta.nome);
    campo('E-mail').should('have.value', conta.email);

    cy.intercept('PUT', '/api/v1/conta').as('guardar');
    campo('Nome').clear().type('Carla Fictícia');
    campo('E-mail').clear().type(novoEmail);
    cy.contains('button', 'Guardar').click();

    cy.wait('@guardar').its('response.statusCode').should('eq', 200);
    cy.contains('[role="status"]', 'Dados da conta guardados.');
    cy.reload();
    campo('Nome').should('have.value', 'Carla Fictícia');
    campo('E-mail').should('have.value', novoEmail);
  });

  it('TS-W-F001-06 altera a palavra-passe e entra com a nova (RF-015)', () => {
    const conta = novaConta('senha');
    const nova = `${conta.palavraPasse}b2`;
    criarContaNaApi(conta);
    iniciarSessao(conta.email, conta.palavraPasse);
    cy.visit('/conta');

    campo('Palavra-passe atual').type(conta.palavraPasse, { log: false });
    campo('Nova palavra-passe').type(nova, { log: false });
    cy.contains('button', 'Alterar palavra-passe').click();
    cy.contains('[role="status"]', 'Palavra-passe alterada.');

    cy.contains('.cabecalho button', 'Terminar sessão').click();
    iniciarSessao(conta.email, nova);
  });
});
