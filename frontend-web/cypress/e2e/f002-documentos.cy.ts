import { campo } from '../support/e2e';
import {
  contaNaApi,
  entrar,
  fichaNaApi,
  pdfFicticio,
  pdfGrande,
  respostaErro,
  textoFicticio,
} from '../support/dados';

// Especificação: m4-especificacao-testes-sistema-frontend-web-v02.md (F002, documentos em E08).
describe('F002 Documentos da ficha (E08)', () => {
  beforeEach(() => {
    contaNaApi('f002-docs').then(({ conta, token }) => {
      fichaNaApi(token, 'Carlos Fictício').then((fichaId) => {
        cy.wrap(fichaId).as('fichaId');
        entrar(conta);
        cy.visit(`/fichas/${fichaId}`);
      });
    });
  });

  it('TS-W-F002-05 carrega um PDF com a categoria e descarrega-o (RF-022, RF-028, RNF-008)', () => {
    cy.intercept('POST', '/api/v1/fichas/*/documentos').as('carregar');
    cy.intercept('GET', '/api/v1/documentos/*').as('descarregar');
    campo('Categoria').select('Receita médica');
    campo('Ficheiro').selectFile(pdfFicticio('receita-ficticia.pdf'));
    cy.contains('button', 'Carregar documento').click();

    cy.wait('@carregar').its('response.statusCode').should('eq', 201);
    cy.contains('[role="status"]', 'Documento «receita-ficticia.pdf» carregado.');
    cy.contains('li', 'receita-ficticia.pdf').should('contain', 'Receita médica').and('contain', 'PDF');
    cy.contains('button', 'Descarregar receita-ficticia.pdf').click();
    cy.wait('@descarregar').its('response.statusCode').should('eq', 200);
  });

  it('TS-W-F002-06 recusa um ficheiro .txt e um PDF com mais de 10 MB antes de enviar (RNF-008)', () => {
    cy.intercept('POST', '/api/v1/fichas/*/documentos').as('carregar');
    campo('Categoria').select('Relatório clínico');
    campo('Ficheiro').selectFile(textoFicticio());
    cy.contains('não é PDF, JPG nem PNG');
    campo('Ficheiro').selectFile(pdfGrande());
    cy.contains('tem mais de 10 MB; o tamanho máximo é 10 MB');
    cy.contains('button', 'Carregar documento').click();
    cy.get('@carregar.all').should('have.length', 0);
  });

  it('TS-W-F002-07 [duplo 503] documento não guardado: mensagem e os dados escolhidos mantêm-se (RF-023)', () => {
    cy.intercept(
      'POST',
      '/api/v1/fichas/*/documentos',
      respostaErro(503, 'DOCUMENTO_NAO_GUARDADO', 'O documento não foi guardado.'),
    ).as('carregar');
    campo('Categoria').select('Documento de identificação');
    campo('Ficheiro').selectFile(pdfFicticio('cartao-ficticio.pdf'));
    cy.contains('button', 'Carregar documento').click();

    cy.wait('@carregar');
    cy.contains('[role="alert"]', 'Não foi possível guardar o documento e nada foi gravado.');
    campo('Categoria').find('option:selected').should('have.text', 'Documento de identificação');
    cy.contains('A ficha não tem documentos.');
  });

  it('TS-W-F002-08 [duplo 503] documento indisponível: erro no lugar dele, com nova tentativa (RF-124)', () => {
    campo('Categoria').select('Receita médica');
    campo('Ficheiro').selectFile(pdfFicticio('receita-indisponivel.pdf'));
    cy.contains('button', 'Carregar documento').click();
    cy.contains('[role="status"]', 'Documento «receita-indisponivel.pdf» carregado.');

    cy.intercept(
      'GET',
      '/api/v1/documentos/*',
      respostaErro(503, 'DOCUMENTO_INDISPONIVEL', 'O ficheiro não está disponível.'),
    ).as('descarregar');
    cy.contains('button', 'Descarregar receita-indisponivel.pdf').click();
    cy.wait('@descarregar');
    cy.contains('li', 'receita-indisponivel.pdf').within(() => {
      cy.contains('[role="alert"]', 'Não foi possível obter o ficheiro neste momento. Tente novamente.');
      cy.contains('button', 'Tentar novamente descarregar receita-indisponivel.pdf');
    });
  });
});
