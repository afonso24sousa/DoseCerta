import { associarNaApi, contaNaApi, entrar, fichaNaApi } from '../support/dados';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v02.md (F004, pessoas da ficha: E12).
describe('F004 Pessoas da ficha (E12)', () => {
  /** Ficha da Ana com o Bruno como Familiar e a Carla como Cuidador. */
  function fichaComPessoas(prefixo: string): void {
    contaNaApi(`${prefixo}-ana`).then((ana) => {
      contaNaApi(`${prefixo}-bruno`, 'Bruno Fictício').then((bruno) => {
        contaNaApi(`${prefixo}-carla`, 'Carla Fictícia').then((carla) => {
          fichaNaApi(ana.token, 'Marta Fictícia').then((fichaId) => {
            associarNaApi(ana.token, fichaId, bruno, 'Familiar');
            associarNaApi(ana.token, fichaId, carla, 'Cuidador');
            entrar(ana.conta);
            cy.visit(`/fichas/${fichaId}/pessoas`);
            cy.contains('tr', 'Carla Fictícia');
          });
        });
      });
    });
  }

  const linha = (nome: string): Cypress.Chainable<JQuery<HTMLElement>> => cy.contains('tr', nome);

  it('TS-W-F004-08 promove um Familiar a Cuidador e despromove-o (RF-047, RF-049, RF-130)', () => {
    fichaComPessoas('f004-promover');
    linha('Bruno Fictício').within(() => cy.contains('button', 'Promover a Cuidador').click());
    cy.contains('button', 'Sim, promover a Cuidador').click();
    cy.contains('[role="status"]', 'Bruno Fictício passou a Cuidador.');
    linha('Bruno Fictício').should('contain', 'Cuidador');

    linha('Bruno Fictício').within(() => cy.contains('button', 'Despromover a Familiar').click());
    cy.contains('button', 'Sim, despromover a Familiar').click();
    cy.contains('[role="status"]', 'Bruno Fictício passou a Familiar.');
    linha('Bruno Fictício').should('contain', 'Familiar');
  });

  it('TS-W-F004-09 remove o acesso de uma pessoa (RF-131)', () => {
    fichaComPessoas('f004-remover');
    linha('Bruno Fictício').within(() => cy.contains('button', 'Remover').click());
    cy.contains('button', 'Sim, remover o acesso').click();
    cy.contains('[role="status"]', 'Bruno Fictício deixou de ter acesso à ficha.');
    cy.contains('tr', 'Bruno Fictício').should('not.exist');
  });

  it('TS-W-F004-10 o Cuidador principal só sai depois de outro assumir o papel; a transferência é imediata (RF-132 a RF-134)', () => {
    fichaComPessoas('f004-principal');
    linha('Ana Fictícia (eu)').within(() => {
      cy.contains('Cuidador principal');
      cy.contains('button', 'Despromover a Familiar').should('be.disabled');
      cy.contains('button', 'Remover').should('be.disabled');
    });
    cy.contains(
      '#pessoas-principal-protegido',
      'Indique outro Cuidador para assumir o papel antes de despromover ou remover o Cuidador principal.',
    );
    cy.contains('A ficha tem de ficar com pelo menos um Cuidador.');

    linha('Carla Fictícia').within(() => cy.contains('button', 'Tornar principal').click());
    cy.contains('button', 'Sim, tornar Cuidador principal').click();
    cy.contains('[role="status"]', 'Carla Fictícia é agora o Cuidador principal.');
    linha('Carla Fictícia').should('contain', 'Cuidador principal');
    linha('Ana Fictícia (eu)').should('not.contain', 'Cuidador principal').and('contain', 'Cuidador');
  });

  it('TS-W-F004-11 o Familiar que abre o convite à mão volta à ficha com o aviso (RF-048)', () => {
    contaNaApi('f004-guarda-ana').then((ana) => {
      contaNaApi('f004-guarda-bruno', 'Bruno Fictício').then((bruno) => {
        fichaNaApi(ana.token, 'Sara Fictícia').then((fichaId) => {
          associarNaApi(ana.token, fichaId, bruno, 'Familiar');
          entrar(bruno.conta);
          cy.visit(`/fichas/${fichaId}/pessoas/convidar`);
          cy.location('pathname').should('eq', `/fichas/${fichaId}`);
          cy.contains('[role="status"]', 'Como Familiar, só pode consultar esta ficha.');
        });
      });
    });
  });
});
