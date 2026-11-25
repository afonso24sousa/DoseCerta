import { campo } from '../support/e2e';
import {
  ContaComToken,
  arquivarNaApi,
  associarNaApi,
  compromissoNaApi,
  compromissosNaApi,
  contaNaApi,
  dataApresentada,
  dataLisboa,
  entrar,
  fichaNaApi,
  instanteLisboa,
  tratamentoNaApi,
} from '../support/dados';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v03.md (F008, E21 Agenda e E22 Compromisso da I071).
// Os compromissos e as tomas dos cenários ficam amanhã (calendário de Lisboa): os cenários não dependem da hora do dia
// nem do fuso da pipeline (UTC). As tomas de amanhã existem sempre numa ficha criada hoje (RF-060).
describe('F008 Agenda e compromissos', () => {
  const amanha = dataLisboa(1);

  /** Cuidador com uma ficha e um tratamento com toma amanhã às 09:00. */
  function cuidadorComFicha(
    prefixo: string,
    nomeFicha = 'Maria Fictícia Santos',
  ): Cypress.Chainable<{ cuidador: ContaComToken; fichaId: string }> {
    return contaNaApi(prefixo).then((cuidador) =>
      fichaNaApi(cuidador.token, nomeFicha).then((fichaId) =>
        tratamentoNaApi(cuidador.token, fichaId, {
          medicamento: 'Metformina',
          dose: '500 mg',
          grauImportancia: 'Moderado',
          horarios: ['09:00'],
        }).then(() => ({ cuidador, fichaId })),
      ),
    );
  }

  /** Abre a agenda no dia de amanhã. */
  function agendaDeAmanha(vista: 'dia' | 'semana' | 'mes' = 'dia'): void {
    cy.visit(`/agenda?vista=${vista}&data=${amanha}`);
  }

  it('TS-W-F008-01 regista um compromisso com a antecedência por omissão (RF-080, RF-116, RF-089)', () => {
    cuidadorComFicha('w-f008-registar').then(({ cuidador, fichaId }) => {
      entrar(cuidador.conta);
      cy.contains('a', 'Agenda').click();
      cy.location('pathname').should('eq', '/agenda');
      cy.contains('a', 'Novo compromisso').click();
      cy.location('pathname').should('eq', `/fichas/${fichaId}/compromissos/novo`);
      cy.contains('Ficha: Maria Fictícia Santos');
      cy.get('#compromisso-antecedencia').find('option:selected').should('contain', '24 horas (por omissão)');

      campo('Data').type(amanha);
      campo('Hora').type('10:30');
      campo('Local').type('Centro de Saúde Fictício');
      campo('Motivo').type('Consulta de rotina');
      cy.contains('button', 'Guardar').click();

      cy.location('search').should('include', `data=${amanha}`).and('include', 'resultado=registado');
      cy.contains('[role="status"]', 'Compromisso registado.');
      cy.contains('li', 'Compromisso: Consulta de rotina').within(() => {
        cy.contains('10:30');
        cy.contains('Maria Fictícia Santos');
      });
      compromissosNaApi(cuidador.token, fichaId).should((compromissos) => {
        expect(compromissos).to.have.length(1);
        expect(compromissos[0].antecedenciaAvisoMinutos).to.eq(1440);
        expect(compromissos[0].dataHora).to.eq(instanteLisboa(amanha, '10:30'));
      });
    });
  });

  it('TS-W-F008-02 recusa campos em falta, junto de cada campo, mantendo os dados (RF-080, RNF-028)', () => {
    cuidadorComFicha('w-f008-validar').then(({ cuidador, fichaId }) => {
      entrar(cuidador.conta);
      cy.visit(`/fichas/${fichaId}/compromissos/novo`);
      campo('Local').type('Centro de Saúde Fictício');
      cy.contains('button', 'Guardar').click();

      cy.contains('Data: é obrigatório.');
      cy.contains('Hora: é obrigatório.');
      cy.contains('Motivo: é obrigatório.');
      campo('Local').should('have.value', 'Centro de Saúde Fictício');
      cy.location('pathname').should('eq', `/fichas/${fichaId}/compromissos/novo`);
      compromissosNaApi(cuidador.token, fichaId).should('have.length', 0);
    });
  });

  it('TS-W-F008-03 edita o compromisso e a antecedência do aviso (RF-081, RF-116)', () => {
    cuidadorComFicha('w-f008-editar').then(({ cuidador, fichaId }) => {
      compromissoNaApi(cuidador.token, fichaId, {
        dataHora: instanteLisboa(amanha, '10:30'),
        motivo: 'Consulta de rotina',
      });
      entrar(cuidador.conta);
      agendaDeAmanha();
      cy.contains('li', 'Compromisso: Consulta de rotina').contains('a', 'Editar').click();

      cy.contains('h1', 'Editar compromisso');
      campo('Data').should('have.value', amanha);
      campo('Hora').should('have.value', '10:30');
      campo('Local').should('have.value', 'Centro de Saúde Fictício');
      campo('Hora').clear().type('11:00');
      campo('Motivo').clear().type('Consulta de cardiologia');
      cy.get('#compromisso-antecedencia').select('1 hora');
      cy.contains('button', 'Guardar').click();

      cy.contains('[role="status"]', 'Compromisso atualizado.');
      cy.contains('li', 'Compromisso: Consulta de cardiologia').should('contain', '11:00');
      cy.contains('Consulta de rotina').should('not.exist');
      compromissosNaApi(cuidador.token, fichaId).should((compromissos) => {
        expect(compromissos[0].antecedenciaAvisoMinutos).to.eq(60);
        expect(compromissos[0].dataHora).to.eq(instanteLisboa(amanha, '11:00'));
      });
    });
  });

  it('TS-W-F008-04 remove o compromisso só depois de confirmar (RF-082)', () => {
    cuidadorComFicha('w-f008-remover').then(({ cuidador, fichaId }) => {
      compromissoNaApi(cuidador.token, fichaId, {
        dataHora: instanteLisboa(amanha, '15:00'),
        motivo: 'Fisioterapia',
      });
      entrar(cuidador.conta);
      agendaDeAmanha();
      cy.contains('li', 'Compromisso: Fisioterapia').contains('a', 'Editar').click();

      cy.contains('button', 'Remover').click();
      cy.contains(`Remover o compromisso «Fisioterapia» de ${dataApresentada(amanha)} às 15:00?`);
      cy.get('section[aria-labelledby="remover-compromisso"]').within(() =>
        cy.contains('button', 'Cancelar').click(),
      );
      compromissosNaApi(cuidador.token, fichaId).should('have.length', 1);

      cy.contains('button', 'Remover').click();
      cy.contains('button', 'Sim, remover o compromisso').click();
      cy.contains('[role="status"]', 'Compromisso removido.');
      cy.contains('Fisioterapia').should('not.exist');
      compromissosNaApi(cuidador.token, fichaId).should('have.length', 0);
    });
  });

  it('TS-W-F008-05 mostra as vistas diária, semanal e mensal, cada uma num só pedido (RF-084 a RF-088, RNF-012)', () => {
    cuidadorComFicha('w-f008-vistas').then(({ cuidador, fichaId }) => {
      compromissoNaApi(cuidador.token, fichaId, {
        dataHora: instanteLisboa(amanha, '10:30'),
        motivo: 'Consulta de rotina',
      });
      cy.intercept('GET', '/api/v1/agenda?vista=dia*').as('dia');
      cy.intercept('GET', '/api/v1/agenda?vista=semana*').as('semana');
      cy.intercept('GET', '/api/v1/agenda?vista=mes*').as('mes');
      entrar(cuidador.conta);
      agendaDeAmanha();

      cy.wait('@dia');
      cy.get('.evento').should('have.length', 2);
      cy.get('.evento')
        .eq(0)
        .should('contain', '09:00')
        .and('contain', 'Toma: Metformina 500 mg (Pendente, só consulta)');
      cy.get('.evento').eq(1).should('contain', '10:30').and('contain', 'Compromisso: Consulta de rotina');
      cy.contains('button', 'Confirmar').should('not.exist');
      cy.get('@dia.all').should('have.length', 1);

      cy.contains('[role="tab"]', 'Semana').click();
      cy.wait('@semana');
      cy.contains('h2', /^Semana de \d{2}\/\d{2}\/\d{4} a \d{2}\/\d{2}\/\d{4}$/);
      cy.get('.dia-semana').should('have.length', 7);
      cy.get('.dia-semana').first().find('h3').should('contain', 'segunda-feira');
      cy.contains('.dia-semana', dataApresentada(amanha)).should('contain', 'Consulta de rotina');
      cy.get('@semana.all').should('have.length', 1);

      cy.contains('[role="tab"]', 'Mês').click();
      cy.wait('@mes');
      cy.get('table.mes thead th').first().should('have.text', 'Seg');
      cy.get('table.mes td').should('contain', 'Compromisso Consulta de rotina');
      cy.get('@mes.all').should('have.length', 1);
    });
  });

  it('TS-W-F008-06 reúne as fichas ativas do Utilizador, sem as arquivadas (RF-083, RF-089)', () => {
    cuidadorComFicha('w-f008-fichas').then(({ cuidador, fichaId }) => {
      compromissoNaApi(cuidador.token, fichaId, {
        dataHora: instanteLisboa(amanha, '10:30'),
        motivo: 'Consulta A',
      });
      fichaNaApi(cuidador.token, 'João Fictício Lopes').then((segunda) => {
        compromissoNaApi(cuidador.token, segunda, {
          dataHora: instanteLisboa(amanha, '11:00'),
          motivo: 'Consulta B',
        });
      });
      fichaNaApi(cuidador.token, 'Rosa Fictícia Pereira').then((arquivada) => {
        compromissoNaApi(cuidador.token, arquivada, {
          dataHora: instanteLisboa(amanha, '12:00'),
          motivo: 'Consulta C',
        });
        arquivarNaApi(cuidador.token, arquivada);
      });
      entrar(cuidador.conta);
      agendaDeAmanha();

      cy.contains('li', 'Consulta A').should('contain', 'Maria Fictícia Santos');
      cy.contains('li', 'Consulta B').should('contain', 'João Fictício Lopes');
      cy.contains('Consulta C').should('not.exist');
      cy.contains('Rosa Fictícia Pereira').should('not.exist');
      cy.get('#agenda-ficha-novo option').should('have.length', 2);
    });
  });

  it('TS-W-F008-07 o Familiar consulta a agenda sem registar, editar nem remover (RF-048)', () => {
    cuidadorComFicha('w-f008-familiar').then(({ cuidador, fichaId }) => {
      compromissoNaApi(cuidador.token, fichaId, {
        dataHora: instanteLisboa(amanha, '10:30'),
        motivo: 'Consulta de rotina',
      });
      contaNaApi('w-f008-familiar-rui', 'Rui Fictício').then((familiar) => {
        associarNaApi(cuidador.token, fichaId, familiar, 'Familiar');
        entrar(familiar.conta);
        agendaDeAmanha();
        cy.contains('li', 'Compromisso: Consulta de rotina').should('contain', 'Maria Fictícia Santos');
        cy.contains('li', 'Toma: Metformina 500 mg');
        cy.contains('a', 'Novo compromisso').should('not.exist');
        cy.contains('a', 'Editar').should('not.exist');

        cy.visit(`/fichas/${fichaId}/compromissos/novo`);
        cy.location('pathname').should('not.include', '/compromissos/novo');
      });
    });
  });

  it('TS-W-F008-08 numa ficha arquivada não regista: guarda da rota e recusa do servidor (RF-120)', () => {
    cuidadorComFicha('w-f008-arquivada').then(({ cuidador, fichaId }) => {
      entrar(cuidador.conta);
      cy.visit(`/fichas/${fichaId}/compromissos/novo`);
      campo('Data').type(amanha);
      campo('Hora').type('10:30');
      campo('Local').type('Centro de Saúde Fictício');
      campo('Motivo').type('Análises');
      // A ficha é arquivada depois de o formulário abrir: a recusa vem do servidor (OP-41, 409 FICHA_ARQUIVADA).
      arquivarNaApi(cuidador.token, fichaId);
      cy.contains('button', 'Guardar').click();

      cy.get('[role="alert"]').should('contain', 'A ficha está arquivada');
      campo('Motivo').should('have.value', 'Análises');
      compromissosNaApi(cuidador.token, fichaId).should('have.length', 0);

      cy.visit(`/fichas/${fichaId}/compromissos/novo`);
      cy.location('pathname').should('not.include', '/compromissos/novo');
    });
  });
});
