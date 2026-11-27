import { campo, novaConta } from '../support/e2e';
import {
  aceitarConviteNaApi,
  arquivarNaApi,
  contaNaApi,
  convidarNaApi,
  entrar,
  estadoApi,
  fichaNaApi,
  pedidoApi,
} from '../support/dados';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v02.md (F004, convites: E05, E13, E14).
describe('F004 Convites (E05, E12 a E14)', () => {
  it('TS-W-F004-01 convida como Familiar; um convite repetido é recusado junto ao e-mail (RF-040, RF-043, RNF-028)', () => {
    contaNaApi('f004-convidar').then(({ conta, token }) => {
      fichaNaApi(token, 'Maria Fictícia').then((fichaId) => {
        const email = `convidado.${Date.now()}@exemplo.pt`;
        entrar(conta);
        cy.intercept('POST', `/api/v1/fichas/${fichaId}/convites`).as('convidar');
        cy.visit(`/fichas/${fichaId}`);
        cy.contains('a', 'Pessoas da ficha').click();
        cy.contains('a', 'Convidar pessoa').click();
        campo('E-mail da pessoa').type(email);
        cy.contains('label', 'Familiar').find('input').check();
        cy.contains('button', 'Enviar convite').click();
        cy.wait('@convidar').its('response.statusCode').should('eq', 201);
        cy.contains('[role="status"]', 'Convite enviado.');

        cy.contains('a', 'Convidar pessoa').click();
        campo('E-mail da pessoa').type(email);
        cy.contains('label', 'Familiar').find('input').check();
        cy.contains('button', 'Enviar convite').click();
        cy.wait('@convidar').its('response.statusCode').should('eq', 409);
        cy.contains('Já existe um convite pendente para este e-mail nesta ficha.');
        campo('E-mail da pessoa').should('have.value', email);
      });
    });
  });

  it('TS-W-F004-02 aceita o convite em E14; como Familiar só consulta as pessoas (RF-042, RF-045, RF-048)', () => {
    contaNaApi('f004-cuidador').then((cuidador) => {
      contaNaApi('f004-familiar', 'Bruno Fictício').then((convidado) => {
        fichaNaApi(cuidador.token, 'Rita Fictícia').then((fichaId) => {
          convidarNaApi(cuidador.token, fichaId, convidado.conta.email, 'Familiar');
          entrar(convidado.conta);
          cy.visit('/fichas');
          cy.contains('a', 'Convites recebidos (1)').click();
          cy.contains('tr', 'Rita Fictícia')
            .should('contain', 'Familiar')
            .and('contain', 'Ana Fictícia')
            .within(() => cy.contains('button', 'Aceitar').click());
          cy.contains('button', 'Sim, aceitar o convite').click();

          cy.contains('[role="status"]', 'Convite aceite.');
          cy.contains('a', 'Abrir a ficha').click();
          cy.contains('O meu papel: Familiar');
          cy.contains('a', 'Pessoas da ficha').click();
          cy.contains('Como Familiar, pode consultar as pessoas associadas à ficha.');
          cy.contains('a', 'Convidar pessoa').should('not.exist');
          cy.get('table button').should('not.exist');
        });
      });
    });
  });

  it('TS-W-F004-03 recusa o convite e pode ser convidado de novo (RF-043, RF-046)', () => {
    contaNaApi('f004-cuidador-r').then((cuidador) => {
      contaNaApi('f004-recusa', 'Carla Fictícia').then((convidado) => {
        fichaNaApi(cuidador.token, 'Vasco Fictício').then((fichaId) => {
          convidarNaApi(cuidador.token, fichaId, convidado.conta.email, 'Cuidador');
          entrar(convidado.conta);
          cy.visit('/convites');
          cy.contains('tr', 'Vasco Fictício').within(() => cy.contains('button', 'Recusar').click());
          cy.contains('button', 'Sim, recusar o convite').click();
          cy.contains('[role="status"]', 'Convite recusado.');
          cy.contains('Não tem convites pendentes.');

          convidarNaApi(cuidador.token, fichaId, convidado.conta.email, 'Cuidador');
          cy.reload();
          cy.contains('tr', 'Vasco Fictício').should('contain', 'Cuidador');
        });
      });
    });
  });

  it('TS-W-F004-12 um convite já respondido noutra sessão é recusado e a lista é atualizada (RF-026, RF-027, RF-045, RF-046)', () => {
    contaNaApi('f004-respondido-c').then((cuidador) => {
      contaNaApi('f004-respondido', 'Joana Fictícia').then((convidado) => {
        fichaNaApi(cuidador.token, 'Tiago Fictício').then((fichaId) => {
          convidarNaApi(cuidador.token, fichaId, convidado.conta.email, 'Familiar');
          entrar(convidado.conta);
          cy.visit('/convites');
          cy.contains('tr', 'Tiago Fictício').within(() => cy.contains('button', 'Aceitar').click());

          // Entretanto o convite é aceite noutra sessão (OP-21).
          aceitarConviteNaApi(convidado.token, fichaId);
          cy.intercept('POST', '/api/v1/convites/*/aceitacao').as('aceitar');
          cy.contains('button', 'Sim, aceitar o convite').click();

          cy.wait('@aceitar').its('response.statusCode').should('eq', 409);
          cy.contains('[role="alert"]', 'O convite já foi aceite ou recusado.');
          // A lista é carregada de novo e o convite já respondido deixa de aparecer.
          cy.contains('tr', 'Tiago Fictício').should('not.exist');
        });
      });
    });
  });

  it('TS-W-F004-04 os convites feitos antes do registo aparecem depois de criar a conta (RF-044)', () => {
    contaNaApi('f004-antes').then((cuidador) => {
      fichaNaApi(cuidador.token, 'Inês Fictícia').then((fichaId) => {
        const nova = { ...novaConta('f004-depois'), nome: 'Diana Fictícia' };
        convidarNaApi(cuidador.token, fichaId, nova.email, 'Familiar');
        cy.visit('/registo');
        campo('Nome').type(nova.nome);
        campo('E-mail').type(nova.email);
        campo('Palavra-passe').type(nova.palavraPasse, { log: false });
        cy.contains('button', 'Criar conta').click();

        cy.contains('[role="status"]', 'Tem 1 convite pendente para ver em Convites.');
        entrar(nova);
        cy.visit('/convites');
        cy.contains('tr', 'Inês Fictícia').should('contain', 'Familiar');
      });
    });
  });

  it('TS-W-F004-05 convite para Cuidador principal: aceitar transfere o papel e o criador sai da ficha (RF-025, RF-026)', () => {
    contaNaApi('f004-criador').then((criador) => {
      contaNaApi('f004-novo-principal', 'Eva Fictícia').then((convidado) => {
        fichaNaApi(criador.token, 'Óscar Fictício', { convidarPrincipal: convidado.conta.email }).then(
          (fichaId) => {
            entrar(convidado.conta);
            cy.visit('/convites');
            cy.contains('Aceitar o convite para Cuidador principal transfere esse papel');
            cy.contains('tr', 'Óscar Fictício')
              .should('contain', 'Cuidador principal')
              .within(() => cy.contains('button', 'Aceitar').click());
            cy.contains('button', 'Sim, aceitar o convite').click();
            cy.contains('a', 'Abrir a ficha').click();
            cy.contains('O meu papel: Cuidador principal');
            // O criador provisório deixa de estar associado e passa a receber 404 (RF-026, RNF-005).
            estadoApi(criador.token, 'GET', `/fichas/${fichaId}`).should('eq', 404);
          },
        );
      });
    });
  });

  it('TS-W-F004-06 convite para Cuidador principal recusado: o criador mantém-se principal (RF-027)', () => {
    contaNaApi('f004-mantem').then((criador) => {
      contaNaApi('f004-recusa-principal', 'Gil Fictício').then((convidado) => {
        fichaNaApi(criador.token, 'Pedro Fictício', { convidarPrincipal: convidado.conta.email }).then(
          (fichaId) => {
            entrar(convidado.conta);
            cy.visit('/convites');
            cy.contains('tr', 'Pedro Fictício').within(() => cy.contains('button', 'Recusar').click());
            cy.contains('button', 'Sim, recusar o convite').click();
            cy.contains('[role="status"]', 'Convite recusado.');

            pedidoApi<{ nome: string; principal: boolean; provisorio: boolean }[]>(
              criador.token,
              'GET',
              `/fichas/${fichaId}/pessoas`,
            )
              .its('body')
              .should((pessoas) => {
                const principal = pessoas.find((p) => p.principal);
                expect(principal?.nome).to.eq('Ana Fictícia');
                expect(principal?.provisorio).to.eq(false);
              });
          },
        );
      });
    });
  });

  it('TS-W-F004-07 numa ficha arquivada as pessoas só se consultam, mas o convite ainda pode ser aceite (RF-120)', () => {
    contaNaApi('f004-arquivada').then((cuidador) => {
      contaNaApi('f004-aceita-arquivada', 'Hugo Fictício').then((convidado) => {
        fichaNaApi(cuidador.token, 'Zita Fictícia').then((fichaId) => {
          convidarNaApi(cuidador.token, fichaId, convidado.conta.email, 'Familiar');
          arquivarNaApi(cuidador.token, fichaId);
          entrar(cuidador.conta);
          cy.visit(`/fichas/${fichaId}/pessoas`);
          cy.contains('Ficha arquivada: as pessoas associadas só podem ser consultadas.');
          cy.contains('a', 'Convidar pessoa').should('not.exist');
          cy.visit(`/fichas/${fichaId}/pessoas/convidar`);
          cy.contains('A ficha está arquivada e só pode ser consultada.');
          cy.location('pathname').should('eq', `/fichas/${fichaId}`);

          aceitarConviteNaApi(convidado.token, fichaId);
          pedidoApi(convidado.token, 'GET', `/fichas/${fichaId}`)
            .its('body.meuPapel')
            .should('eq', 'Familiar');
        });
      });
    });
  });
});
