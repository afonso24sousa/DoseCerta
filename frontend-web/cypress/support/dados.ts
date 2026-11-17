/**
 * Preparação dos dados dos cenários de F002 a F005 pela API (documentação da API v04), sempre com dados fictícios e
 * e-mails únicos por execução: cada cenário cria as suas contas, fichas, convites e tratamentos e não depende de outro.
 */
import { ContaFicticia, criarContaNaApi, iniciarSessao, novaConta } from './e2e';

/** Conta fictícia já criada, com o token de uma sessão aberta pela API (OP-02). */
export interface ContaComToken {
  conta: ContaFicticia;
  token: string;
}

/** Pedido autenticado à API, pelo proxy `/api` da aplicação servida. */
export function pedidoApi<T>(
  token: string,
  method: string,
  url: string,
  body?: unknown,
): Cypress.Chainable<Cypress.Response<T>> {
  return cy.request<T>({
    method,
    url: `/api/v1${url}`,
    headers: { Authorization: `Bearer ${token}` },
    body,
  });
}

/** Código de estado de um pedido autenticado, sem falhar o cenário quando é um erro (por exemplo 404). */
export function estadoApi(token: string, method: string, url: string): Cypress.Chainable<number> {
  return cy
    .request({
      method,
      url: `/api/v1${url}`,
      headers: { Authorization: `Bearer ${token}` },
      failOnStatusCode: false,
    })
    .its('status');
}

/** Cria a conta (OP-01) e abre uma sessão pela API (OP-02). */
export function contaNaApi(prefixo: string, nome = 'Ana Fictícia'): Cypress.Chainable<ContaComToken> {
  const conta = { ...novaConta(prefixo), nome };
  criarContaNaApi(conta);
  return cy
    .request<{ token: string }>('POST', '/api/v1/auth/login', {
      email: conta.email,
      palavraPasse: conta.palavraPasse,
    })
    .then((resposta) => ({ conta, token: resposta.body.token }));
}

/** Inicia sessão pelo ecrã E02 com uma conta já criada. */
export function entrar(conta: ContaFicticia): void {
  iniciarSessao(conta.email, conta.palavraPasse);
}

/** Opções da ficha criada pela API (OP-09). */
export interface OpcoesFicha {
  /** Ficha própria (RF-030); por omissão, ficha de terceiro. */
  propria?: boolean;
  /** E-mail convidado para Cuidador principal (RF-025); por omissão, o criador é o principal (RF-024). */
  convidarPrincipal?: string;
}

/** Cria uma ficha pela API (OP-09) e devolve o identificador. */
export function fichaNaApi(token: string, nome: string, opcoes: OpcoesFicha = {}): Cypress.Chainable<string> {
  const propria = opcoes.propria ?? false;
  return pedidoApi<{ id: string }>(token, 'POST', '/fichas', {
    nomeCompleto: nome,
    dataNascimento: '1941-04-15',
    contactoEmergencia: '+351 910 000 003',
    grauAutonomiaId: null,
    condicoesSaudeIds: [],
    alergiasIds: [],
    propria,
    cuidadorPrincipal: propria
      ? undefined
      : opcoes.convidarPrincipal
        ? { eu: false, email: opcoes.convidarPrincipal }
        : { eu: true },
  }).then((resposta) => resposta.body.id);
}

/** Convida uma pessoa para a ficha (OP-19). */
export function convidarNaApi(
  token: string,
  fichaId: string,
  email: string,
  papel: 'Cuidador' | 'Familiar',
  paraPrincipal = false,
): void {
  pedidoApi(token, 'POST', `/fichas/${fichaId}/convites`, { email, papel, paraPrincipal })
    .its('status')
    .should('eq', 201);
}

/** Aceita pela API (OP-21) todos os convites pendentes do Utilizador para a ficha indicada. */
export function aceitarConviteNaApi(token: string, fichaId: string): void {
  pedidoApi<{ id: string; fichaId: string }[]>(token, 'GET', '/convites').then((resposta) => {
    const convites = resposta.body.filter((c) => c.fichaId === fichaId);
    expect(convites, 'convites pendentes para a ficha').to.have.length.greaterThan(0);
    for (const convite of convites) {
      pedidoApi(token, 'POST', `/convites/${convite.id}/aceitacao`);
    }
  });
}

/** Associa uma pessoa à ficha com o papel indicado: convite (OP-19) e aceitação (OP-21). */
export function associarNaApi(
  tokenCuidador: string,
  fichaId: string,
  convidado: ContaComToken,
  papel: 'Cuidador' | 'Familiar',
): void {
  convidarNaApi(tokenCuidador, fichaId, convidado.conta.email, papel);
  aceitarConviteNaApi(convidado.token, fichaId);
}

/** Dados de um tratamento fictício (OP-27). */
export interface TratamentoFicticio {
  medicamento: string;
  dose: string;
  grauImportancia: 'Leve' | 'Moderado' | 'Grave';
  dataInicio: string;
  dataFim: string | null;
  horarios: string[];
}

/** Regista um tratamento pela API (OP-27) e devolve o identificador. */
export function tratamentoNaApi(
  token: string,
  fichaId: string,
  dados: Partial<TratamentoFicticio> = {},
): Cypress.Chainable<string> {
  return pedidoApi<{ id: string }>(token, 'POST', `/fichas/${fichaId}/tratamentos`, {
    medicamento: 'Paracetamol',
    dose: '1 g',
    grauImportancia: 'Leve',
    dataInicio: dataLisboa(0),
    dataFim: null,
    horarios: ['09:00'],
    ...dados,
  }).then((resposta) => resposta.body.id);
}

/** Arquiva a ficha pela API (OP-12). */
export function arquivarNaApi(token: string, fichaId: string): void {
  pedidoApi(token, 'POST', `/fichas/${fichaId}/arquivo`).its('status').should('be.oneOf', [200, 204]);
}

/** Toma devolvida por OP-32 e OP-33 (só os campos usados nos cenários). */
export interface TomaApi {
  id: string;
  medicamento: string;
  previstaEm: string;
  estado: 'Pendente' | 'Confirmada' | 'ConfirmadaComAtraso' | 'NaoConfirmada';
}

/** Tomas da ficha entre `de` e `ate` (OP-32; no máximo 31 dias). */
export function tomasNaApi(
  token: string,
  fichaId: string,
  de: string,
  ate: string,
): Cypress.Chainable<TomaApi[]> {
  return pedidoApi<TomaApi[]>(token, 'GET', `/fichas/${fichaId}/tomas?de=${de}&ate=${ate}`).its('body');
}

/** Confirma a toma pela API (OP-33). */
export function confirmarTomaNaApi(token: string, tomaId: string): void {
  pedidoApi(token, 'POST', `/tomas/${tomaId}/confirmacao`).its('status').should('eq', 200);
}

const PARTES_LISBOA = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'Europe/Lisbon',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
});

/** Data e hora de um instante na hora de Lisboa, que é a do servidor (a pipeline corre em UTC). */
export function agoraLisboa(deslocamentoMinutos = 0): { data: string; hora: string; minutos: number } {
  const instante = new Date(Date.now() + deslocamentoMinutos * 60_000);
  const partes = Object.fromEntries(PARTES_LISBOA.formatToParts(instante).map((p) => [p.type, p.value]));
  return {
    data: `${partes['year']}-${partes['month']}-${partes['day']}`,
    hora: `${partes['hour']}:${partes['minute']}`,
    minutos: Number(partes['hour']) * 60 + Number(partes['minute']),
  };
}

/** Data `AAAA-MM-DD` de hoje mais `dias`, contada no calendário de Lisboa. */
export function dataLisboa(dias: number): string {
  const [ano, mes, dia] = agoraLisboa().data.split('-').map(Number);
  return new Date(Date.UTC(ano, mes - 1, dia + dias)).toISOString().slice(0, 10);
}

/** Data `dd/mm/aaaa` (RNF-029) de uma data `AAAA-MM-DD`. */
export function dataApresentada(data: string): string {
  const [ano, mes, dia] = data.split('-');
  return `${dia}/${mes}/${ano}`;
}

/** Ficheiro fictício para `selectFile`, gerado em tempo de execução (nada fica no repositório). */
export interface FicheiroFicticio {
  contents: Cypress.Buffer;
  fileName: string;
  mimeType: string;
  lastModified: number;
}

function ficheiro(fileName: string, mimeType: string, contents: Cypress.Buffer): FicheiroFicticio {
  return { contents, fileName, mimeType, lastModified: Date.now() };
}

/** PDF fictício mínimo, que começa por `%PDF` como o servidor confirma pelo conteúdo. */
export function pdfFicticio(nome = 'documento-ficticio.pdf'): FicheiroFicticio {
  const texto =
    '%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n' +
    '3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n';
  return ficheiro(nome, 'application/pdf', Cypress.Buffer.from(texto));
}

/** Ficheiro de texto com a extensão `.txt`, que não é PDF, JPG nem PNG (RNF-008). */
export function textoFicticio(nome = 'notas-ficticias.txt'): FicheiroFicticio {
  return ficheiro(nome, 'text/plain', Cypress.Buffer.from('Texto fictício.'));
}

/** PDF com mais de 10 MB (RNF-008), gerado em memória. */
export function pdfGrande(nome = 'documento-grande.pdf'): FicheiroFicticio {
  const conteudo = Cypress.Buffer.alloc(10 * 1024 * 1024 + 1024, 0x20);
  conteudo.write('%PDF-1.4\n', 0);
  return ficheiro(nome, 'application/pdf', conteudo);
}

/** Resposta substituída de um erro da API (duplo), no formato `ProblemaApi` (secção 3.3 da API). */
export function respostaErro(
  estado: number,
  codigo: string,
  mensagem: string,
): {
  statusCode: number;
  body: { codigo: string; mensagem: string };
} {
  return { statusCode: estado, body: { codigo, mensagem } };
}
