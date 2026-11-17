import { campo } from '../support/e2e';
import { contaNaApi, entrar, fichaNaApi } from '../support/dados';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v02.md (F003).
describe('F003 Ficha própria (E06, E08, E12, E13)', () => {
  it('TS-W-F003-01 cria a ficha própria, com o titular como Cuidador principal (RF-030)', () => {
    contaNaApi('f003-criar').then(({ conta }) => {
      entrar(conta);
      cy.intercept('POST', '/api/v1/fichas').as('criar');
      cy.visit('/fichas/nova');
      cy.contains('label', 'Ficha própria').find('input').check();
      cy.contains('legend', 'Cuidador principal').should('not.exist');
      campo('Nome completo').type('Ana Fictícia');
      campo('Data de nascimento').type('1985-09-21');
      campo('Contacto de emergência').type('+351 910 000 008');
      cy.contains('button', 'Criar ficha').click();

      cy.wait('@criar').its('request.body.propria').should('eq', true);
      cy.contains('[role="status"]', 'Ficha criada.');
      cy.contains('span', 'Ficha própria');
      cy.contains('O meu papel: Cuidador principal');
    });
  });

  it('TS-W-F003-02 quem já tem ficha própria não a pode criar de novo (RF-031)', () => {
    contaNaApi('f003-segunda').then(({ conta, token }) => {
      fichaNaApi(token, 'Ana Fictícia', { propria: true }).then(() => {
        entrar(conta);
        cy.visit('/fichas/nova');
        cy.contains('label', 'Ficha própria').find('input').should('be.disabled');
        cy.contains('Já tem uma ficha própria e só pode ter uma; pode criar fichas de terceiros.');
      });
    });
  });

  it('TS-W-F003-03 na ficha própria convida-se como nas outras e o titular fica protegido (RF-032 a RF-034)', () => {
    contaNaApi('f003-convidar').then(({ conta, token }) => {
      fichaNaApi(token, 'Ana Fictícia', { propria: true }).then((fichaId) => {
        entrar(conta);
        cy.intercept('POST', `/api/v1/fichas/${fichaId}/convites`).as('convidar');
        cy.visit(`/fichas/${fichaId}/pessoas/convidar`);
        cy.contains('Oferecer o papel de Cuidador principal').should('not.exist');
        campo('E-mail da pessoa').type(`familiar.f003.${Date.now()}@exemplo.pt`);
        cy.contains('label', 'Familiar').find('input').check();
        cy.contains('button', 'Enviar convite').click();

        cy.wait('@convidar').its('response.statusCode').should('eq', 201);
        cy.contains('[role="status"]', 'Convite enviado.');
        cy.contains('tr', 'Ana Fictícia (eu)').within(() => {
          cy.contains('Cuidador principal');
          cy.get('button').should('not.exist');
        });
        cy.contains('Na ficha própria o titular é sempre o Cuidador principal');
      });
    });
  });
});
