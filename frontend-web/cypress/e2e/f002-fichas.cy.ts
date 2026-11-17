import { campo } from '../support/e2e';
import { contaNaApi, entrar, fichaNaApi, pdfFicticio } from '../support/dados';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v02.md (F002).
describe('F002 Criar e editar fichas (E05 a E09)', () => {
  it('TS-W-F002-01 cria uma ficha de terceiro com um documento, sendo o Cuidador principal (RF-020 a RF-022, RF-024)', () => {
    contaNaApi('f002-criar').then(({ conta }) => {
      entrar(conta);
      cy.intercept('POST', '/api/v1/fichas').as('criar');
      cy.visit('/fichas');
      cy.contains('a', 'Criar ficha').click();
      cy.contains('label', 'Ficha de terceiro').find('input').check();
      cy.contains('label', 'Eu sou o Cuidador principal').find('input').check();
      campo('Nome completo').type('Maria Fictícia Santos');
      campo('Data de nascimento').type('1946-02-11');
      campo('Contacto de emergência').type('+351 910 000 004');
      campo('Adicionar documento').selectFile(pdfFicticio('relatorio-ficticio.pdf'));
      campo('Categoria de relatorio-ficticio.pdf').select('Relatório clínico');
      cy.contains('button', 'Criar ficha').click();

      cy.wait('@criar').its('response.statusCode').should('eq', 201);
      cy.contains('[role="status"]', 'Ficha criada.');
      cy.contains('h1', 'Maria Fictícia Santos');
      cy.contains('O meu papel: Cuidador principal');
      cy.contains('relatorio-ficticio.pdf');
    });
  });

  it('TS-W-F002-02 cria uma ficha com convite para Cuidador principal, ficando o criador provisório (RF-025)', () => {
    contaNaApi('f002-convite').then(({ conta }) => {
      entrar(conta);
      cy.visit('/fichas/nova');
      cy.contains('label', 'Ficha de terceiro').find('input').check();
      cy.contains('label', 'Convidar outra pessoa para Cuidador principal').find('input').check();
      campo('E-mail da pessoa a convidar').type(`principal.${Date.now()}@exemplo.pt`);
      campo('Nome completo').type('João Fictício Lopes');
      campo('Data de nascimento').type('1939-07-30');
      campo('Contacto de emergência').type('+351 910 000 005');
      cy.contains('button', 'Criar ficha').click();

      cy.contains('[role="status"]', 'Ficha criada e convite enviado para Cuidador principal.');
      cy.contains('a', 'Pessoas da ficha').click();
      cy.contains('td', 'Cuidador principal (provisório)');
    });
  });

  it('TS-W-F002-03 recusa nome em falta e data futura, junto a cada campo, mantendo os dados (RF-021, RNF-028)', () => {
    contaNaApi('f002-validar').then(({ conta }) => {
      entrar(conta);
      cy.intercept('POST', '/api/v1/fichas').as('criar');
      cy.visit('/fichas/nova');
      cy.contains('label', 'Ficha de terceiro').find('input').check();
      cy.contains('label', 'Eu sou o Cuidador principal').find('input').check();
      campo('Data de nascimento').type('2099-01-01');
      campo('Contacto de emergência').type('+351 910 000 006');
      cy.contains('button', 'Criar ficha').click();

      cy.contains('Nome completo: é obrigatório.');
      cy.contains('Data de nascimento: não pode ser uma data futura.');
      campo('Contacto de emergência').should('have.value', '+351 910 000 006');
      cy.get('@criar.all').should('have.length', 0);
    });
  });

  it('TS-W-F002-14 recusa criar sem escolher o Cuidador principal e um convite sem e-mail (RF-024, RF-025, RNF-028)', () => {
    contaNaApi('f002-principal').then(({ conta }) => {
      entrar(conta);
      cy.intercept('POST', '/api/v1/fichas').as('criar');
      cy.visit('/fichas/nova');
      cy.contains('label', 'Ficha de terceiro').find('input').check();
      campo('Nome completo').type('Vera Fictícia');
      campo('Data de nascimento').type('1944-10-03');
      campo('Contacto de emergência').type('+351 910 000 012');
      cy.contains('button', 'Criar ficha').click();
      cy.contains('Escolha quem é o Cuidador principal.');

      cy.contains('label', 'Convidar outra pessoa para Cuidador principal').find('input').check();
      cy.contains('button', 'Criar ficha').click();
      cy.contains('E-mail da pessoa a convidar: é obrigatório.');
      campo('Nome completo').should('have.value', 'Vera Fictícia');
      cy.get('@criar.all').should('have.length', 0);
    });
  });

  it('TS-W-F002-15 quem não está associado à ficha não a vê (RF-028, RNF-005)', () => {
    contaNaApi('f002-dono').then((dono) => {
      contaNaApi('f002-estranho', 'Gil Fictício').then((estranho) => {
        fichaNaApi(dono.token, 'Beatriz Fictícia').then((fichaId) => {
          entrar(estranho.conta);
          cy.intercept('GET', `/api/v1/fichas/${fichaId}`).as('consultar');
          cy.visit(`/fichas/${fichaId}`);
          cy.wait('@consultar').its('response.statusCode').should('eq', 404);
          cy.contains('[role="alert"]', 'A ficha não existe ou não tem acesso a ela.');
          cy.contains('Beatriz Fictícia').should('not.exist');
        });
      });
    });
  });

  it('TS-W-F002-04 edita os dados da ficha; um erro de validação mantém o que foi escrito (RF-029, RNF-028)', () => {
    contaNaApi('f002-editar').then(({ conta, token }) => {
      fichaNaApi(token, 'Rosa Fictícia').then((fichaId) => {
        entrar(conta);
        cy.visit(`/fichas/${fichaId}`);
        cy.contains('a', 'Editar ficha').click();
        campo('Nome completo').clear();
        campo('Contacto de emergência').clear().type('+351 910 000 007');
        cy.contains('button', 'Guardar alterações').click();
        cy.contains('Nome completo: é obrigatório.');
        campo('Contacto de emergência').should('have.value', '+351 910 000 007');

        cy.intercept('PUT', `/api/v1/fichas/${fichaId}`).as('editar');
        campo('Nome completo').type('Rosa Fictícia Pereira');
        cy.contains('button', 'Guardar alterações').click();
        cy.wait('@editar').its('response.statusCode').should('eq', 200);
        cy.contains('[role="status"]', 'Alterações guardadas.');
        cy.contains('h1', 'Rosa Fictícia Pereira');
        cy.contains('+351 910 000 007');
      });
    });
  });
});
