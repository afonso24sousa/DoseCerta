import {
  ContaComToken,
  TomaApi,
  arquivarNaApi,
  associarNaApi,
  confirmarTomaNaApi,
  contaNaApi,
  dataApresentada,
  dataLisboa,
  entrar,
  fichaNaApi,
  respostaErro,
  tomasNaApi,
  tratamentoNaApi,
} from '../support/dados';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v02.md (F006, calendário de tomas da I068). Os textos são
// os do contrato de interface da F006 (Sprint 08). As horas e as datas são as de Lisboa, que é a hora do servidor.
//
// O servidor só gera tomas a partir do momento em que a ficha foi criada (Ficha.AtivaDesdeUtc, RF-060). Numa ficha
// criada pelo cenário não há tomas passadas: a toma de hoje confirmável é a das 23:59, confirmada antes da hora prevista
// (dentro da tolerância). Os estados que precisam de tempo real a passar (confirmada com atraso, mais de 30 minutos;
// não confirmada, 24 horas) usam duplos da resposta da API, identificados com «[duplo]» (especificação, secção 6.2).
describe('F006 Calendário e confirmação de tomas', () => {
  /** Medicamento único por cenário, para isolar as suas tomas na lista. */
  const medicamento = (id: string): string => `Metformina ${id} ${Date.now()}`;

  /** Hora da toma de hoje: já existe na ficha acabada de criar e a confirmação fica dentro da tolerância. */
  const HORA_DE_HOJE = '23:59';

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

  /** Toma de hoje às 23:59 de um tratamento só de hoje. */
  const tomaDeHoje = (
    prefixo: string,
    nome: string,
    grau: 'Leve' | 'Grave' = 'Leve',
  ): Cypress.Chainable<{ cuidador: ContaComToken; fichaId: string }> =>
    fichaComToma(prefixo, nome, { data: dataLisboa(0), hora: HORA_DE_HOJE, grau });

  /** Abre E15 diretamente no separador «Calendário de tomas» e espera pela lista (OP-32). */
  function abrirCalendario(fichaId: string): void {
    cy.intercept('GET', '/api/v1/fichas/*/tomas*').as('tomas');
    cy.visit(`/fichas/${fichaId}/medicacao?vista=calendario`);
    cy.wait('@tomas');
  }

  it('TS-W-F006-01 mostra o calendário da ficha com hora, medicamento, dose, grau e estado (RF-063, RNF-027, RNF-029)', () => {
    const nome = medicamento('TS-W-F006-01');
    contaNaApi('f006-calendario').then(({ conta, token }) => {
      fichaNaApi(token, 'Clara Fictícia').then((fichaId) => {
        // Tratamento contínuo: a toma das 08:00 existe hoje (se ainda não passou) ou amanhã, sempre no período.
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
        cy.contains('a', 'Medicação').click();
        cy.contains('[role="tab"]', 'Calendário de tomas')
          .click()
          .should('have.attr', 'aria-selected', 'true');
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
    tomaDeHoje('f006-confirmar', nome).then(({ cuidador, fichaId }) => {
      entrar(cuidador.conta);
      abrirCalendario(fichaId);
      cy.intercept('POST', '/api/v1/tomas/*/confirmacao').as('confirmar');
      cy.contains('li', nome).within(() => cy.contains('button', 'Confirmar toma').click());

      cy.wait('@confirmar').its('response.statusCode').should('eq', 200);
      cy.contains('[role="status"]', 'Resultado esperado errado de propósito.');
      cy.contains('li', nome)
        .should('contain', 'Confirmada')
        .and('not.contain', 'Confirmada com atraso')
        .and('contain', `Confirmada por Ana Fictícia em ${dataApresentada(dataLisboa(0))} às`)
        .find('button')
        .should('not.exist');
    });
  });

  it('TS-W-F006-03 [duplo] uma toma confirmada fora da tolerância fica confirmada com atraso (RF-067)', () => {
    const nome = medicamento('TS-W-F006-03');
    tomaDeHoje('f006-atraso', nome, 'Grave').then(({ cuidador, fichaId }) => {
      tomasNaApi(cuidador.token, fichaId, dataLisboa(0), dataLisboa(0)).then((tomas) => {
        const toma = tomas.find((t) => t.medicamento === nome)!;
        // Duplo da OP-33: a resposta que o servidor dá quando a confirmação passa a tolerância de 30 minutos do grau Grave.
        cy.intercept('POST', `/api/v1/tomas/${toma.id}/confirmacao`, {
          statusCode: 200,
          body: {
            ...toma,
            estado: 'ConfirmadaComAtraso',
            confirmadaPor: { id: '00000000-0000-0000-0000-000000000001', nome: 'Ana Fictícia' },
            confirmadaEm: toma.previstaEm.replace(/T\d{2}:\d{2}/, 'T23:59'),
          },
        }).as('confirmar');

        entrar(cuidador.conta);
        abrirCalendario(fichaId);
        cy.contains('li', nome).within(() => cy.contains('button', 'Confirmar toma').click());

        cy.wait('@confirmar');
        cy.contains('[role="status"]', 'Toma confirmada com atraso.');
        cy.contains('li', nome)
          .should('contain', 'Confirmada com atraso')
          .and('contain', `Confirmada por Ana Fictícia em ${dataApresentada(dataLisboa(0))} às 23:59`);
      });
    });
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

  it('TS-W-F006-05 [duplo] uma toma sem confirmação 24 horas depois é recusada e fica não confirmada (RF-068)', () => {
    const nome = medicamento('TS-W-F006-05');
    tomaDeHoje('f006-nao-confirmada', nome).then(({ cuidador, fichaId }) => {
      // Duplo da OP-33 (409 TOMA_NAO_CONFIRMAVEL) e, a partir da recusa, da OP-32 com o estado que o servidor passa a
      // devolver (NaoConfirmada): a toma real só passaria a esse estado 24 horas depois da hora prevista.
      let recusada = false;
      cy.intercept('POST', '/api/v1/tomas/*/confirmacao', (req) => {
        recusada = true;
        req.reply(
          respostaErro(
            409,
            'TOMA_NAO_CONFIRMAVEL',
            'A toma já não pode ser confirmada: passaram 24 horas da hora prevista.',
          ),
        );
      }).as('confirmar');
      cy.intercept('GET', '/api/v1/fichas/*/tomas*', (req) => {
        req.continue((res) => {
          if (recusada) {
            res.body = (res.body as TomaApi[]).map((t) =>
              t.medicamento === nome ? { ...t, estado: 'NaoConfirmada' } : t,
            );
          }
        });
      }).as('tomas');

      entrar(cuidador.conta);
      cy.visit(`/fichas/${fichaId}/medicacao?vista=calendario`);
      cy.wait('@tomas');
      cy.contains('li', nome).within(() => cy.contains('button', 'Confirmar toma').click());

      cy.wait('@confirmar');
      cy.contains('[role="alert"]', 'A toma ficou não confirmada e já não pode ser confirmada.');
      cy.wait('@tomas');
      cy.contains('li', nome).should('contain', 'Não confirmada').find('button').should('not.exist');
    });
  });

  it('TS-W-F006-06 confirmação repetida: a recusa indica quem confirmou e a lista mostra o estado atual (RF-105)', () => {
    const nome = medicamento('TS-W-F006-06');
    contaNaApi('f006-outra', 'Carla Fictícia').then((carla) => {
      tomaDeHoje('f006-repetida', nome).then(({ cuidador, fichaId }) => {
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
      });
    });
  });

  it('TS-W-F006-07 o Familiar consulta as tomas sem as poder confirmar (RF-048)', () => {
    const nome = medicamento('TS-W-F006-07');
    contaNaApi('f006-familiar', 'Filipe Fictício').then((familiar) => {
      tomaDeHoje('f006-cuidador', nome).then(({ cuidador, fichaId }) => {
        associarNaApi(cuidador.token, fichaId, familiar, 'Familiar');
        entrar(familiar.conta);
        abrirCalendario(fichaId);

        cy.contains('[role="status"]', 'Como Familiar, pode consultar as tomas, mas não as confirmar.');
        cy.contains('li', nome).should('contain', 'Pendente').find('button').should('not.exist');
      });
    });
  });

  it('TS-W-F006-08 [duplo] numa ficha arquivada ainda se confirma a toma que ficou por confirmar (RF-120, RF-064)', () => {
    const nome = medicamento('TS-W-F006-08');
    tomaDeHoje('f006-arquivada', nome).then(({ cuidador, fichaId }) => {
      tomasNaApi(cuidador.token, fichaId, dataLisboa(0), dataLisboa(0)).then((tomas) => {
        const toma = tomas.find((t) => t.medicamento === nome)!;
        // O arquivo (real) remove as tomas futuras por confirmar; só ficam as que já passaram, que uma ficha acabada de
        // criar não tem. Duplos da OP-32 (a toma que ficou por confirmar antes do arquivo) e da OP-33 (a confirmação).
        arquivarNaApi(cuidador.token, fichaId);
        cy.intercept('GET', '/api/v1/fichas/*/tomas*', (req) => {
          req.continue((res) => {
            const outras = (res.body as TomaApi[]).filter((t) => t.id !== toma.id);
            res.body = [...outras, toma];
          });
        }).as('tomas');
        cy.intercept('POST', `/api/v1/tomas/${toma.id}/confirmacao`, {
          statusCode: 200,
          body: {
            ...toma,
            estado: 'Confirmada',
            confirmadaPor: { id: '00000000-0000-0000-0000-000000000001', nome: 'Ana Fictícia' },
            confirmadaEm: toma.previstaEm.replace(/T\d{2}:\d{2}/, 'T23:58'),
          },
        }).as('confirmar');

        entrar(cuidador.conta);
        cy.visit(`/fichas/${fichaId}/medicacao?vista=calendario`);
        cy.wait('@tomas');

        cy.contains('[role="status"]', 'Ficha arquivada:');
        cy.contains('li', nome).within(() => cy.contains('button', 'Confirmar toma').click());
        cy.wait('@confirmar');
        cy.contains('[role="status"]', 'Toma confirmada.');
        cy.contains('li', nome).should('contain', 'Confirmada por Ana Fictícia em');
      });
    });
  });
});
