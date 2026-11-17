import {
  ContaComToken,
  TomaApi,
  agoraLisboa,
  arquivarNaApi,
  associarNaApi,
  confirmarTomaNaApi,
  contaNaApi,
  dataApresentada,
  dataLisboa,
  entrar,
  fichaNaApi,
  tomasNaApi,
  tratamentoNaApi,
} from '../support/dados';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v02.md (F006, calendário de tomas da I068). Os textos são
// os do contrato de interface da F006 (Sprint 08). As horas e as datas são as de Lisboa, que é a hora do servidor.
describe('F006 Calendário e confirmação de tomas', () => {
  /** Medicamento único por cenário, para isolar as suas tomas na lista. */
  const medicamento = (id: string): string => `Metformina ${id} ${Date.now()}`;

  /** Hora certa de agora em Lisboa (por exemplo «14:00»): a toma de hoje já existe e está dentro da tolerância. */
  const horaCerta = (): string => `${agoraLisboa().hora.slice(0, 2)}:00`;

  /** Cuidador com uma ficha e um tratamento com tomas só no dia `data`, à `hora`. */
  function fichaComToma(
    prefixo: string,
    nome: string,
    dados: { data: string; hora: string; grau: 'Leve' | 'Moderado' | 'Grave' },
  ): Cypress.Chainable<{ cuidador: ContaComToken; fichaId: string }> {
    return contaNaApi(prefixo).then((cuidador) =>
      fichaNaApi(cuidador.token, 'Clara Fictícia').then((fichaId) =>
        tratamentoNaApi(cuidador.token, fichaId, {
          medicamento: nome,
          dose: '500 mg',
          grauImportancia: dados.grau,
          dataInicio: dados.data,
          dataFim: dados.data,
          horarios: [dados.hora],
        }).then(() => ({ cuidador, fichaId })),
      ),
    );
  }

  /** Abre o calendário e espera pela lista (OP-32). */
  function abrirCalendario(fichaId: string): void {
    cy.intercept('GET', '/api/v1/fichas/*/tomas*').as('tomas');
    cy.visit(`/fichas/${fichaId}/tomas`);
    cy.wait('@tomas');
  }

  it('TS-W-F006-01 mostra o calendário da ficha com hora, medicamento, dose, grau e estado (RF-063, RNF-027, RNF-029)', () => {
    const nome = medicamento('TS-W-F006-01');
    contaNaApi('f006-calendario').then(({ conta, token }) => {
      fichaNaApi(token, 'Clara Fictícia').then((fichaId) => {
        tratamentoNaApi(token, fichaId, {
          medicamento: nome,
          dose: '850 mg',
          grauImportancia: 'Moderado',
          dataInicio: dataLisboa(0),
          dataFim: null,
          horarios: ['08:00'],
        });
        entrar(conta);
        cy.intercept('GET', '/api/v1/fichas/*/tomas*').as('tomas');
        cy.visit(`/fichas/${fichaId}`);
        cy.contains('a', 'Calendário de tomas').click();
        cy.wait('@tomas');

        cy.contains(`Tomas de ${dataApresentada(dataLisboa(-1))} a ${dataApresentada(dataLisboa(5))}`);
        cy.contains('li', nome)
          .should('contain', '08:00')
          .and('contain', '850 mg')
          .and('contain', 'Moderado')
          .and('contain', 'Pendente');

        cy.contains('button', 'Período seguinte').click();
        cy.wait('@tomas');
        cy.contains(`Tomas de ${dataApresentada(dataLisboa(6))} a ${dataApresentada(dataLisboa(12))}`);
        cy.contains('li', nome);
      });
    });
  });

  it('TS-W-F006-02 confirma uma toma com um só clique e mostra quem confirmou e quando (RF-064, RF-065, RNF-021)', () => {
    const nome = medicamento('TS-W-F006-02');
    fichaComToma('f006-confirmar', nome, { data: dataLisboa(0), hora: horaCerta(), grau: 'Leve' }).then(
      ({ cuidador, fichaId }) => {
        entrar(cuidador.conta);
        abrirCalendario(fichaId);
        cy.intercept('POST', '/api/v1/tomas/*/confirmacao').as('confirmar');
        cy.contains('li', nome).within(() => cy.contains('button', 'Confirmar toma').click());

        cy.wait('@confirmar').its('response.statusCode').should('eq', 200);
        cy.contains('[role="status"]', 'Toma confirmada.');
        cy.contains('li', nome)
          .should('contain', 'Confirmada')
          .and('not.contain', 'Confirmada com atraso')
          .and('contain', `Confirmada por Ana Fictícia em ${dataApresentada(dataLisboa(0))} às`)
          .find('button')
          .should('not.exist');
      },
    );
  });

  it('TS-W-F006-03 uma toma confirmada fora da tolerância fica confirmada com atraso (RF-067)', () => {
    const nome = medicamento('TS-W-F006-03');
    // Grave (tolerância de 30 minutos): a toma de há 90 minutos já passou a tolerância. Entre as 00:00 e as 01:30 de
    // Lisboa essa hora é de ontem, que está no período por omissão (de ontem a hoje + 5 dias).
    const antes = agoraLisboa(-90);
    fichaComToma('f006-atraso', nome, { data: antes.data, hora: antes.hora, grau: 'Grave' }).then(
      ({ cuidador, fichaId }) => {
        entrar(cuidador.conta);
        abrirCalendario(fichaId);
        cy.contains('li', nome).within(() => cy.contains('button', 'Confirmar toma').click());

        cy.contains('[role="status"]', 'Toma confirmada com atraso.');
        cy.contains('li', nome)
          .should('contain', 'Confirmada com atraso')
          .and('contain', 'Confirmada por Ana Fictícia em');
      },
    );
  });

  it('TS-W-F006-04 recusa confirmar uma toma de amanhã antes das 00:00 desse dia (RF-064)', () => {
    const nome = medicamento('TS-W-F006-04');
    fichaComToma('f006-janela', nome, { data: dataLisboa(1), hora: '09:00', grau: 'Leve' }).then(
      ({ cuidador, fichaId }) => {
        entrar(cuidador.conta);
        abrirCalendario(fichaId);
        cy.intercept('POST', '/api/v1/tomas/*/confirmacao').as('confirmar');
        cy.contains('li', nome).within(() => cy.contains('button', 'Confirmar toma').click());

        cy.wait('@confirmar').its('response.statusCode').should('eq', 409);
        cy.contains('[role="alert"]', 'A toma só pode ser confirmada a partir das 00:00 do dia previsto.');
        cy.wait('@tomas');
        cy.contains('li', nome).should('contain', 'Pendente');
      },
    );
  });

  it('TS-W-F006-05 uma toma sem confirmação 24 horas depois fica não confirmada, sem «Confirmar toma» (RF-068)', () => {
    const nome = medicamento('TS-W-F006-05');
    const dia = dataLisboa(-3);
    fichaComToma('f006-nao-confirmada', nome, { data: dia, hora: '09:00', grau: 'Leve' }).then(
      ({ cuidador, fichaId }) => {
        // O processo periódico do servidor (de 30 em 30 segundos) passa a toma a NaoConfirmada; espera-se pela API.
        const esperarNaoConfirmada = (tentativas: number): void => {
          tomasNaApi(cuidador.token, fichaId, dia, dia).then((tomas: TomaApi[]) => {
            if (tomas.some((t) => t.estado === 'NaoConfirmada')) {
              return;
            }
            expect(tentativas, 'tentativas até a toma passar a NaoConfirmada').to.be.greaterThan(0);
            esperarNaoConfirmada(tentativas - 1);
          });
        };
        esperarNaoConfirmada(600);

        entrar(cuidador.conta);
        abrirCalendario(fichaId);
        cy.contains('button', 'Período anterior').click();
        cy.wait('@tomas');
        cy.contains('li', nome).should('contain', 'Não confirmada').find('button').should('not.exist');
      },
    );
  });

  it('TS-W-F006-06 confirmação repetida: a recusa indica quem confirmou e a lista mostra o estado atual (RF-105)', () => {
    const nome = medicamento('TS-W-F006-06');
    contaNaApi('f006-outra', 'Carla Fictícia').then((carla) => {
      fichaComToma('f006-repetida', nome, { data: dataLisboa(0), hora: horaCerta(), grau: 'Leve' }).then(
        ({ cuidador, fichaId }) => {
          associarNaApi(cuidador.token, fichaId, carla, 'Cuidador');
          entrar(cuidador.conta);
          abrirCalendario(fichaId);
          cy.contains('li', nome).within(() => cy.contains('button', 'Confirmar toma'));

          // Outro Cuidador confirma entretanto (OP-33).
          tomasNaApi(carla.token, fichaId, dataLisboa(0), dataLisboa(0)).then((tomas) => {
            confirmarTomaNaApi(carla.token, tomas.find((t) => t.medicamento === nome)!.id);
          });
          cy.intercept('POST', '/api/v1/tomas/*/confirmacao').as('confirmar');
          cy.contains('li', nome).within(() => cy.contains('button', 'Confirmar toma').click());

          cy.wait('@confirmar').its('response.statusCode').should('eq', 409);
          cy.contains('[role="alert"]', 'A toma já foi confirmada por Carla Fictícia em');
          cy.wait('@tomas');
          cy.contains('li', nome).should('contain', 'Confirmada por Carla Fictícia em');
        },
      );
    });
  });

  it('TS-W-F006-07 o Familiar consulta as tomas sem as poder confirmar (RF-048)', () => {
    const nome = medicamento('TS-W-F006-07');
    contaNaApi('f006-familiar', 'Filipe Fictício').then((familiar) => {
      fichaComToma('f006-cuidador', nome, { data: dataLisboa(0), hora: horaCerta(), grau: 'Leve' }).then(
        ({ cuidador, fichaId }) => {
          associarNaApi(cuidador.token, fichaId, familiar, 'Familiar');
          entrar(familiar.conta);
          abrirCalendario(fichaId);

          cy.contains('Como Familiar, pode consultar as tomas, mas não as confirmar.');
          cy.contains('li', nome).should('contain', 'Pendente').find('button').should('not.exist');
        },
      );
    });
  });

  it('TS-W-F006-08 numa ficha arquivada ainda se confirma a toma que ficou por confirmar (RF-120, RF-064)', () => {
    const nome = medicamento('TS-W-F006-08');
    fichaComToma('f006-arquivada', nome, { data: dataLisboa(0), hora: horaCerta(), grau: 'Leve' }).then(
      ({ cuidador, fichaId }) => {
        arquivarNaApi(cuidador.token, fichaId);
        entrar(cuidador.conta);
        abrirCalendario(fichaId);

        cy.contains('[role="status"]', 'Ficha arquivada:');
        cy.contains('li', nome).within(() => cy.contains('button', 'Confirmar toma').click());
        cy.contains('[role="status"]', 'Toma confirmada.');
      },
    );
  });
});
