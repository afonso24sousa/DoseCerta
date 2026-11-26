import { campo } from '../support/e2e';
import {
  arquivarNaApi,
  associarNaApi,
  contaNaApi,
  dataApresentada,
  dataLisboa,
  entrar,
  fichaNaApi,
  pdfGrande,
  pedidoApi,
  pdfFicticio,
  respostaErro,
  textoFicticio,
  tratamentoNaApi,
} from '../support/dados';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v02.md (F005: E15 a E17).
describe('F005 Registo de medicação (E15 a E17)', () => {
  /** Conta com uma ficha ativa, já em E16 (novo tratamento). */
  function novoTratamento(prefixo: string): void {
    contaNaApi(prefixo).then(({ conta, token }) => {
      fichaNaApi(token, 'Clara Fictícia').then((fichaId) => {
        cy.wrap(fichaId).as('fichaId');
        entrar(conta);
        cy.visit(`/fichas/${fichaId}`);
        cy.contains('a', 'Medicação').click();
        cy.contains('a', 'Novo tratamento').click();
      });
    });
  }

  function acrescentarHora(hora: string): void {
    campo('Nova hora').type(hora);
    cy.contains('button', 'Acrescentar hora').click();
  }

  it('TS-W-F005-01 regista um tratamento com fim, dois horários e receita PDF (RF-050 a RF-052, RF-054)', () => {
    novoTratamento('f005-registar');
    cy.intercept('POST', '/api/v1/fichas/*/tratamentos').as('registar');
    campo('Medicamento').type('Metformina');
    campo('Dose').type('500 mg');
    acrescentarHora('20:00');
    acrescentarHora('08:00');
    campo('Data de início').type(dataLisboa(0));
    campo('Data de fim').type(dataLisboa(30));
    cy.contains('label', 'Moderado (tolerância 1 h)').find('input').check();
    campo('Receita médica (opcional)').selectFile(pdfFicticio('receita-ficticia.pdf'));
    cy.contains('button', 'Guardar').click();

    cy.wait('@registar').its('response.statusCode').should('eq', 201);
    cy.contains('[role="status"]', 'Tratamento registado.');
    cy.contains('tr', 'Metformina 500 mg')
      .should('contain', '08:00 e 20:00')
      .and('contain', 'Moderado (tolerância 1 h)')
      .and('contain', `${dataApresentada(dataLisboa(0))} a ${dataApresentada(dataLisboa(30))}`);
    cy.contains('a', 'Metformina 500 mg').click();
    cy.contains('button', 'Abrir a receita');
  });

  it('TS-W-F005-02 regista um tratamento contínuo, sem data de fim (RF-050, RF-060)', () => {
    novoTratamento('f005-continuo');
    campo('Medicamento').type('Insulina');
    campo('Dose').type('10 UI');
    acrescentarHora('12:00');
    campo('Data de início').type(dataLisboa(0));
    cy.contains('label', 'Grave (tolerância 30 min)').find('input').check();
    cy.contains('button', 'Guardar').click();

    cy.contains('[role="status"]', 'Tratamento registado.');
    cy.contains('tr', 'Insulina 10 UI').should(
      'contain',
      `desde ${dataApresentada(dataLisboa(0))}, contínuo`,
    );
  });

  it('TS-W-F005-03 recusa obrigatórios em falta, hora repetida e fim anterior ao início, mantendo os dados (RF-050, RF-051, RNF-028)', () => {
    novoTratamento('f005-validar');
    cy.intercept('POST', '/api/v1/fichas/*/tratamentos').as('registar');
    cy.contains('button', 'Guardar').click();
    cy.contains('Medicamento: é obrigatório.');
    cy.contains('Indique pelo menos uma hora de toma.');
    cy.contains('Escolha o grau de importância.');

    campo('Medicamento').type('Varfarina');
    acrescentarHora('09:00');
    acrescentarHora('09:00');
    cy.contains('A hora 09:00 já está na lista; não pode repetir a mesma hora.');
    campo('Data de início').type(dataLisboa(0));
    campo('Data de fim').type(dataLisboa(-2));
    cy.contains('button', 'Guardar').click();
    cy.contains('Data de fim: não pode ser anterior à data de início.');
    campo('Medicamento').should('have.value', 'Varfarina');
    cy.get('@registar.all').should('have.length', 0);
  });

  it('TS-W-F005-04 recusa uma receita .txt e uma com mais de 10 MB antes de enviar (RF-052, RNF-008)', () => {
    novoTratamento('f005-receita');
    campo('Receita médica (opcional)').selectFile(textoFicticio());
    cy.contains('não é PDF, JPG nem PNG');
    campo('Receita médica (opcional)').selectFile(pdfGrande());
    cy.contains('tem mais de 10 MB; o tamanho máximo é 10 MB');
  });

  it('TS-W-F005-05 [duplo 503] receita não guardada: nada é registado e os dados mantêm-se (RF-053)', () => {
    novoTratamento('f005-503');
    cy.intercept(
      'POST',
      '/api/v1/fichas/*/tratamentos',
      respostaErro(503, 'DOCUMENTO_NAO_GUARDADO', 'A receita não foi guardada.'),
    ).as('registar');
    campo('Medicamento').type('Atorvastatina');
    campo('Dose').type('20 mg');
    acrescentarHora('21:00');
    campo('Data de início').type(dataLisboa(0));
    cy.contains('label', 'Leve (tolerância 2 h)').find('input').check();
    campo('Receita médica (opcional)').selectFile(pdfFicticio('receita-atorvastatina.pdf'));
    cy.contains('button', 'Guardar').click();

    cy.wait('@registar');
    cy.contains('[role="alert"]', 'A receita não foi guardada. O tratamento não foi registado.');
    campo('Medicamento').should('have.value', 'Atorvastatina');
    campo('Receita médica (opcional)').should(($input) => {
      expect(($input[0] as HTMLInputElement).files?.[0]?.name).to.eq('receita-atorvastatina.pdf');
    });
    cy.location('pathname').should('match', /\/medicacao\/novo$/);
  });

  it('TS-W-F005-06 edita um tratamento em curso sem alterar a data de início; um terminado não se edita (RF-055)', () => {
    contaNaApi('f005-editar').then(({ conta, token }) => {
      fichaNaApi(token, 'Rui Fictício').then((fichaId) => {
        tratamentoNaApi(token, fichaId, {
          medicamento: 'Metformina',
          dose: '500 mg',
          horarios: ['08:00'],
        }).then((tratamentoId) => {
          entrar(conta);
          cy.intercept('PUT', `/api/v1/tratamentos/${tratamentoId}`).as('atualizar');
          cy.visit(`/fichas/${fichaId}/medicacao/${tratamentoId}`);
          cy.contains('a', 'Editar').click();
          campo('Data de início').should('be.disabled');
          cy.contains('Alterar os horários só afeta as tomas futuras');
          campo('Dose').clear().type('850 mg');
          acrescentarHora('20:00');
          cy.contains('button', 'Guardar').click();

          cy.wait('@atualizar')
            .its('request.body')
            .should('deep.equal', {
              dose: '850 mg',
              horarios: ['08:00', '20:00'],
            });
          cy.contains('[role="status"]', 'Alterações guardadas.');
          cy.contains('h1', 'Metformina 850 mg');
          cy.contains('08:00 e 20:00');

          pedidoApi(token, 'POST', `/tratamentos/${tratamentoId}/termino`);
          cy.visit(`/fichas/${fichaId}/medicacao/${tratamentoId}/editar`);
          cy.contains('O tratamento já terminou e não pode ser editado');
          cy.location('pathname').should('eq', `/fichas/${fichaId}/medicacao/${tratamentoId}`);
        });
      });
    });
  });

  it('TS-W-F005-07 termina um tratamento, que continua consultável nos terminados (RF-054, RF-056, RF-057)', () => {
    contaNaApi('f005-terminar').then(({ conta, token }) => {
      fichaNaApi(token, 'Rui Fictício').then((fichaId) => {
        tratamentoNaApi(token, fichaId, { medicamento: 'Amoxicilina', dose: '1 g' }).then((tratamentoId) => {
          entrar(conta);
          cy.visit(`/fichas/${fichaId}/medicacao/${tratamentoId}`);
          cy.contains('button', 'Terminar tratamento').click();
          cy.contains('As tomas futuras deixam de existir');
          cy.contains('button', 'Sim, terminar o tratamento').click();

          cy.contains('[role="status"]', 'Tratamento terminado.');
          cy.contains('span', 'Terminado');
          cy.contains('a', 'Editar').should('not.exist');
          cy.contains('a', 'Voltar à medicação').click();
          cy.contains('Não há tratamentos em curso.');
          cy.contains('label', 'Terminados').find('input').check();
          cy.contains('a', 'Amoxicilina 1 g');
        });
      });
    });
  });

  it('TS-W-F005-08 o Familiar consulta a medicação sem Registar, Editar nem Terminar (RF-048, RF-054)', () => {
    contaNaApi('f005-cuidador').then((cuidador) => {
      contaNaApi('f005-familiar', 'Filipe Fictício').then((familiar) => {
        fichaNaApi(cuidador.token, 'Rui Fictício').then((fichaId) => {
          associarNaApi(cuidador.token, fichaId, familiar, 'Familiar');
          tratamentoNaApi(cuidador.token, fichaId, { medicamento: 'Omeprazol', dose: '20 mg' });
          entrar(familiar.conta);
          cy.visit(`/fichas/${fichaId}/medicacao`);
          cy.contains('a', 'Omeprazol 20 mg');
          cy.contains('a', 'Novo tratamento').should('not.exist');
          cy.contains('a', 'Omeprazol 20 mg').click();
          cy.contains('Grau de importância');
          cy.contains('a', 'Editar').should('not.exist');
          cy.contains('button', 'Terminar tratamento').should('not.exist');

          cy.visit(`/fichas/${fichaId}/medicacao/novo`);
          cy.location('pathname').should('eq', `/fichas/${fichaId}`);
          cy.contains('Como Familiar, só pode consultar esta ficha.');
        });
      });
    });
  });

  it('TS-W-F005-10 quem não está associado à ficha não vê a medicação e volta às fichas (RF-054, RNF-005)', () => {
    contaNaApi('f005-dono').then((dono) => {
      contaNaApi('f005-estranho', 'Gil Fictício').then((estranho) => {
        fichaNaApi(dono.token, 'Rui Fictício').then((fichaId) => {
          tratamentoNaApi(dono.token, fichaId, { medicamento: 'Lisinopril', dose: '10 mg' });
          entrar(estranho.conta);
          cy.visit(`/fichas/${fichaId}/medicacao`);
          cy.location('pathname').should('eq', '/fichas');
          cy.contains('[role="status"]', 'A ficha não existe ou não tem acesso a ela.');
          cy.contains('Lisinopril').should('not.exist');
        });
      });
    });
  });

  it('TS-W-F005-09 numa ficha arquivada não se regista nem edita, mas termina-se um tratamento (RF-056, RF-120)', () => {
    contaNaApi('f005-arquivada').then(({ conta, token }) => {
      fichaNaApi(token, 'Rui Fictício').then((fichaId) => {
        tratamentoNaApi(token, fichaId, { medicamento: 'Paracetamol', dose: '1 g' }).then((tratamentoId) => {
          arquivarNaApi(token, fichaId);
          entrar(conta);
          cy.visit(`/fichas/${fichaId}/medicacao`);
          cy.contains('Ficha arquivada: a medicação só pode ser consultada');
          cy.contains('a', 'Novo tratamento').should('not.exist');
          cy.visit(`/fichas/${fichaId}/medicacao/${tratamentoId}`);
          cy.contains('a', 'Editar').should('not.exist');
          cy.contains('button', 'Terminar tratamento').click();
          cy.contains('button', 'Sim, terminar o tratamento').click();
          cy.contains('[role="status"]', 'Tratamento terminado.');
        });
      });
    });
  });
});
