import {
  arquivarNaApi,
  associarNaApi,
  contaNaApi,
  entrar,
  fichaNaApi,
  tratamentoNaApi,
} from '../support/dados';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v02.md (F002, estado da ficha em E05, E08 e E11).
describe('F002 Arquivar, reativar e eliminar fichas e acesso do Familiar (E05, E08)', () => {
  it('TS-W-F002-09 arquiva a ficha, que fica só de consulta e aparece nas arquivadas (RF-120, RF-122, RF-028)', () => {
    contaNaApi('f002-arquivar').then(({ conta, token }) => {
      fichaNaApi(token, 'Lúcia Fictícia').then((fichaId) => {
        entrar(conta);
        cy.visit(`/fichas/${fichaId}`);
        cy.contains('button', 'Arquivar ficha').click();
        cy.contains('button', 'Sim, arquivar a ficha').click();

        cy.contains('[role="status"]', 'Ficha arquivada. Fica só de consulta até ser reativada.');
        cy.contains('Ficha arquivada: só pode ser consultada.');
        cy.contains('a', 'Editar ficha').should('not.exist');
        cy.contains('button', 'Carregar documento').should('not.exist');
        cy.visit('/fichas');
        cy.contains('label', 'Arquivadas').find('input').check();
        cy.contains('tr', 'Lúcia Fictícia').should('contain', 'Arquivada');
      });
    });
  });

  it('TS-W-F002-10 reativa uma ficha arquivada, que volta a poder ser alterada (RF-121)', () => {
    contaNaApi('f002-reativar').then(({ conta, token }) => {
      fichaNaApi(token, 'Paulo Fictício').then((fichaId) => {
        arquivarNaApi(token, fichaId);
        entrar(conta);
        cy.visit(`/fichas/${fichaId}`);
        cy.contains('button', 'Reativar ficha').click();
        cy.contains('button', 'Sim, reativar a ficha').click();

        cy.contains('[role="status"]', 'Ficha reativada.');
        cy.contains('a', 'Editar ficha');
      });
    });
  });

  it('TS-W-F002-11 elimina uma ficha sem histórico (RF-123)', () => {
    contaNaApi('f002-eliminar').then(({ conta, token }) => {
      fichaNaApi(token, 'Sérgio Fictício').then((fichaId) => {
        entrar(conta);
        cy.intercept('DELETE', `/api/v1/fichas/${fichaId}`).as('eliminar');
        cy.visit(`/fichas/${fichaId}`);
        cy.contains('button', 'Eliminar ficha').click();
        cy.contains('button', 'Sim, eliminar a ficha').click();

        cy.wait('@eliminar').its('response.statusCode').should('eq', 204);
        cy.location('pathname').should('eq', '/fichas');
        cy.contains('[role="status"]', 'Ficha eliminada.');
        cy.contains('Sérgio Fictício').should('not.exist');
      });
    });
  });

  it('TS-W-F002-12 recusa eliminar uma ficha com medicação e propõe arquivar (RF-123)', () => {
    contaNaApi('f002-historico').then(({ conta, token }) => {
      fichaNaApi(token, 'Teresa Fictícia').then((fichaId) => {
        tratamentoNaApi(token, fichaId);
        entrar(conta);
        cy.intercept('DELETE', `/api/v1/fichas/${fichaId}`).as('eliminar');
        cy.visit(`/fichas/${fichaId}`);
        cy.contains('button', 'Eliminar ficha').click();
        cy.contains('button', 'Sim, eliminar a ficha').click();

        cy.wait('@eliminar').its('response.statusCode').should('eq', 409);
        cy.contains('[role="alert"]', 'não pode ser eliminada; só pode ser arquivada');
        cy.contains('h1', 'Teresa Fictícia');
      });
    });
  });

  it('TS-W-F002-13 o Familiar consulta a ficha e os documentos, sem ações de alteração (RF-028, RF-048)', () => {
    contaNaApi('f002-cuidador').then((cuidador) => {
      contaNaApi('f002-familiar', 'Filipe Fictício').then((familiar) => {
        fichaNaApi(cuidador.token, 'Helena Fictícia').then((fichaId) => {
          associarNaApi(cuidador.token, fichaId, familiar, 'Familiar');
          entrar(familiar.conta);
          cy.visit('/fichas');
          cy.contains('tr', 'Helena Fictícia').should('contain', 'Familiar');
          cy.contains('a', 'Helena Fictícia').click();

          cy.contains('O meu papel: Familiar');
          cy.contains('Como Familiar, pode consultar a ficha e descarregar os documentos.');
          cy.contains('a', 'Editar ficha').should('not.exist');
          cy.contains('button', /Arquivar|Eliminar/).should('not.exist');
          cy.contains('button', 'Carregar documento').should('not.exist');
        });
      });
    });
  });
});
