import { defineConfig } from 'cypress';

/**
 * Testes de sistema do frontend web (I063, QG-06). O endereço da aplicação vem de `CYPRESS_baseUrl` (na
 * frontend-web-ci, `https://localhost:4200` com o proxy `/api` para a API da execução).
 */
export default defineConfig({
  e2e: {
    baseUrl: process.env['CYPRESS_baseUrl'] ?? 'https://localhost:4200',
    specPattern: 'cypress/e2e/**/*.cy.ts',
    supportFile: 'cypress/support/e2e.ts',
    video: false,
    defaultCommandTimeout: 10000,
  },
});
