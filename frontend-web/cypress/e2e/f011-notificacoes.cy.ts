import {
  ContaComToken,
  agoraLisboa,
  associarNaApi,
  compromissoNaApi,
  contaNaApi,
  convidarNaApi,
  entrar,
  fichaNaApi,
  instanteLisboa,
  ocorrenciaNaApi,
  pedidoApi,
} from '../support/dados';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v03.md (F011, E25 Notificações da I077).
// O convite (RF-041), a ocorrência (RF-115) e o compromisso próximo (RF-117) são provocados no backend real. Os atrasos
// de tomas (RF-110 a RF-113) só acontecem 30 minutos a 2 horas depois da hora prevista, e o e-mail só fica «não
// entregue» depois de 3 reenvios com 5 minutos de intervalo (RF-119): esses estados usam duplos da resposta da OP-51,
// identificados com «[duplo]» (plano de testes, secção 5.3).
describe('F011 Histórico de notificações', () => {
  interface NotificacaoApi {
    tipo: string;
    fichaId: string;
    criadaEm: string;
  }

  /** Espera, sem tempo fixo, que o Utilizador tenha `n` notificações (o aviso do compromisso demora até 60 s). */
  function esperarNotificacoes(conta: ContaComToken, n: number, tentativa = 0): void {
    pedidoApi<NotificacaoApi[]>(conta.token, 'GET', '/notificacoes').then(({ body }) => {
      if (body.length < n && tentativa < 30) {
        cy.wait(3000, { log: false });
        esperarNotificacoes(conta, n, tentativa + 1);
      } else {
        expect(body, 'notificações recebidas').to.have.length.of.at.least(n);
      }
    });
  }

  function notificacao(
    tipo: string,
    criadaEm: string,
    extra: Partial<Record<string, string | null>> = {},
  ): Record<string, string | null> {
    return {
      id: `00000000-0000-4000-8000-0000000009${criadaEm.slice(11, 13)}`,
      tipo,
      mensagem: 'A toma de Metformina de Maria Fictícia Santos das 08:00 não foi confirmada.',
      fichaId: '00000000-0000-4000-8000-000000000101',
      nomeFicha: 'Maria Fictícia Santos',
      medicamento: null,
      previstaEm: null,
      criadaEm,
      email: 'Enviado',
      ...extra,
    };
  }

  it('TS-W-F011-01 mostra o convite, a ocorrência e o compromisso próximo, do mais recente para o mais antigo (RF-140, RF-041, RF-115, RF-117)', () => {
    contaNaApi('w-f011-ana').then((ana) => {
      contaNaApi('w-f011-rui', 'Rui Fictício').then((rui) => {
        fichaNaApi(ana.token, 'Maria Fictícia Santos').then((fichaId) => {
          associarNaApi(ana.token, fichaId, rui, 'Cuidador');
          const ocorrida = agoraLisboa(-10);
          ocorrenciaNaApi(
            ana.token,
            fichaId,
            'Queda ligeira sem lesões.',
            instanteLisboa(ocorrida.data, ocorrida.hora),
          );
          // O momento do aviso (20 minutos antes de 30) já passou: o servidor avisa no ciclo seguinte (RNF-040).
          const consulta = agoraLisboa(20);
          compromissoNaApi(ana.token, fichaId, {
            dataHora: instanteLisboa(consulta.data, consulta.hora),
            motivo: 'Consulta de rotina',
            antecedenciaAvisoMinutos: 30,
          });
          esperarNotificacoes(rui, 3);

          entrar(rui.conta);
          cy.contains('a', 'Notificações').click();
          cy.location('pathname').should('eq', '/notificacoes');
          cy.get('.notificacao').should('have.length', 3);
          cy.get('.notificacao')
            .eq(0)
            .should('contain', 'Compromisso próximo')
            .and('contain', 'Consulta de rotina');
          cy.get('.notificacao')
            .eq(1)
            .should('contain', 'Nova ocorrência')
            .and('contain', 'Ana Fictícia registou uma ocorrência');
          cy.get('.notificacao')
            .eq(2)
            .should('contain', 'Convite recebido')
            .and('contain', 'Ver os convites');
          cy.get('.notificacao').each(($n) => {
            expect($n.text()).to.contain('Maria Fictícia Santos');
            expect($n.text()).to.match(/\d{2}\/\d{2}\/\d{4} \d{2}:\d{2}/);
            expect($n.text()).to.match(/E-mail (enviado|pendente \(reenvio em curso\)|não entregue)/);
          });
          cy.get('.notificacao').eq(0).find('a').should('have.attr', 'href', `/fichas/${fichaId}`);
        });
      });
    });
  });

  it('TS-W-F011-02 só mostra as notificações do próprio Utilizador: o autor da ocorrência não é notificado (RF-140, RF-115)', () => {
    contaNaApi('w-f011-autor').then((ana) => {
      contaNaApi('w-f011-familiar', 'Rui Fictício').then((rui) => {
        fichaNaApi(ana.token, 'Maria Fictícia Santos').then((fichaId) => {
          associarNaApi(ana.token, fichaId, rui, 'Familiar');
          const ocorrida = agoraLisboa(-5);
          ocorrenciaNaApi(ana.token, fichaId, 'Dormiu mal.', instanteLisboa(ocorrida.data, ocorrida.hora));
          esperarNotificacoes(rui, 2);

          entrar(ana.conta);
          cy.visit('/notificacoes');
          cy.contains('Não tem notificações.');
          cy.get('.notificacao').should('not.exist');
        });
      });
    });
  });

  it('TS-W-F011-03 indica que não há notificações e atualiza ao carregar em «Atualizar» (RF-140)', () => {
    contaNaApi('w-f011-vazio').then((sofia) => {
      contaNaApi('w-f011-convida').then((ana) => {
        entrar(sofia.conta);
        cy.visit('/notificacoes');
        cy.contains('Não tem notificações.');

        fichaNaApi(ana.token, 'João Fictício Lopes').then((fichaId) => {
          convidarNaApi(ana.token, fichaId, sofia.conta.email, 'Familiar');
        });
        cy.contains('button', 'Atualizar').click();
        cy.get('.notificacao').should('have.length', 1).and('contain', 'Convite recebido');
        cy.get('.notificacao').should('contain', 'João Fictício Lopes');
      });
    });
  });

  it('TS-W-F011-04 [duplo] tomas em atraso dos Cuidadores e dos Familiares e e-mail não entregue (RF-110 a RF-113, RF-119)', () => {
    cy.intercept('GET', '/api/v1/notificacoes*', {
      statusCode: 200,
      body: [
        notificacao('TomaEmAtraso', '2026-11-25T09:00:00+00:00', {
          medicamento: 'Metformina',
          previstaEm: '2026-11-25T08:00:00+00:00',
          email: 'Pendente',
        }),
        notificacao('TomaGraveFamiliares', '2026-11-25T11:00:00+00:00', {
          mensagem: 'A toma de Insulina de Maria Fictícia Santos das 10:00 não foi confirmada.',
          medicamento: 'Insulina',
          previstaEm: '2026-11-25T10:00:00+00:00',
          email: 'NaoEntregue',
        }),
      ],
    }).as('notificacoes');
    contaNaApi('w-f011-duplo').then((ana) => {
      entrar(ana.conta);
      cy.visit('/notificacoes');
      cy.wait('@notificacoes');
      cy.get('.notificacao')
        .eq(0)
        .within(() => {
          cy.contains('Toma de grau Grave em atraso prolongado');
          cy.contains('25/11/2026 11:00');
          cy.contains('Toma: Insulina, prevista para 25/11/2026 às 10:00');
          cy.contains('.notificacao__email--nao-entregue', 'E-mail não entregue');
        });
      cy.get('.notificacao')
        .eq(1)
        .within(() => {
          cy.contains('Toma em atraso');
          cy.contains('Toma: Metformina, prevista para 25/11/2026 às 08:00');
          cy.contains('E-mail pendente (reenvio em curso)');
        });
      cy.get('.notificacao__icone').should('have.length', 2);
    });
  });

  it('TS-W-F011-05 [duplo] sem ligação ao servidor mostra a mensagem de erro', () => {
    cy.intercept('GET', '/api/v1/notificacoes*', { forceNetworkError: true });
    contaNaApi('w-f011-rede').then((ana) => {
      entrar(ana.conta);
      cy.visit('/notificacoes');
      cy.get('[role="alert"]').should('be.visible');
      cy.get('.notificacao').should('not.exist');
    });
  });
});
