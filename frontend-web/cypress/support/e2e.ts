/** Dados fictícios e ações comuns dos cenários da F001. */

export interface ContaFicticia {
  nome: string;
  email: string;
  palavraPasse: string;
}

/** Conta fictícia com e-mail único por execução, para os cenários poderem ser repetidos. */
export function novaConta(prefixo: string): ContaFicticia {
  const sufixo = `${Date.now()}${Math.floor(Math.random() * 1000)}`;
  return {
    nome: 'Ana Fictícia',
    email: `${prefixo}.${sufixo}@exemplo.pt`,
    palavraPasse: `Teste${sufixo.slice(-4)}a1`,
  };
}

/** Campo do formulário pelo rótulo exato (`<label for>`). */
export function campo(rotulo: string): Cypress.Chainable<JQuery<HTMLElement>> {
  return cy
    .contains('label', new RegExp(`^${rotulo}$`))
    .invoke('attr', 'for')
    .then((id) => cy.get(`#${id}`));
}

/** Cria a conta diretamente na API (precondição dos cenários). */
export function criarContaNaApi(conta: ContaFicticia): void {
  cy.request('POST', '/api/v1/auth/registo', conta).its('status').should('eq', 201);
}

/** Inicia sessão pelo ecrã E02 e espera pela lista diária. */
export function iniciarSessao(email: string, palavraPasse: string): void {
  cy.visit('/entrar');
  campo('E-mail').type(email);
  campo('Palavra-passe').type(palavraPasse, { log: false });
  cy.contains('button', 'Entrar').click();
  cy.location('pathname').should('eq', '/lista-diaria');
}
