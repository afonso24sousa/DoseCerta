**I062 - Elaborar os planos de testes do frontend web e do frontend mobile**

Tipo: Teste · Prioridade: 5 - Muito baixa · Milestone: m4 · Sprint: S07 · Orçamento: 30 (Executor 18 / Revisor 9 / Auditor 3)
Executor: Ricardo André Santos Ribeiro · Revisor: _(Sprint Planning)_ · Auditor: _(Sprint Planning)_ · Prazo do Executor: _(Sprint Planning)_

**User Stories**
- US-I062-01 - Como Revisor ou avaliador, quero um plano de testes do frontend web com o âmbito, a estratégia, o ambiente, os dados e os critérios de entrada e de saída, para saber como e quando o frontend web é considerado testado.
- US-I062-02 - Como Revisor ou avaliador, quero um plano de testes do frontend mobile com o âmbito, a estratégia, o ambiente, os dados e os critérios de entrada e de saída, para saber como e quando a aplicação mobile é considerada testada.

**Descrição técnica**
Elaborar os planos de testes v01 do frontend web e do frontend mobile, com o âmbito (F001 a F011, sem a F007, na parte de cada componente), a estratégia (testes de sistema automáticos obrigatórios com Cypress e integration_test e simulação dos testes de aceitação; testes unitários opcionais, conforme a secção 11.5 do Regulamento Interno), o ambiente (condições de referência da especificação de requisitos), os dados fictícios, as responsabilidades, os critérios de entrada e de saída e a ligação aos quality gates QG-06 e QG-07.

**Objetivo**
Definir antes da implementação dos testes como os dois frontends vão ser verificados em m4.

**Resultado esperado**
m4-plano-testes-frontend-web-v01.md e m4-plano-testes-frontend-mobile-v01.md.

**Critérios de aceitação**
- [ ] Cada plano define o âmbito, a estratégia, o ambiente, os dados, as responsabilidades e os critérios de entrada e de saída.
- [ ] Cada plano identifica as funcionalidades e os requisitos do componente abrangidos pelos testes de sistema e de aceitação.
- [ ] Cada plano relaciona os testes com os quality gates QG-06 (web) e QG-07 (mobile).
- [ ] Cada plano usa apenas dados fictícios.
- [ ] Cada plano é coerente com a especificação de requisitos v02 e com o Regulamento Interno do Grupo (secções 11.5 e 11.6).
- [ ] Todas as User Stories da Descrição (US-I062-01 a US-I062-02) estão satisfeitas.

**Evidências do Executor**
Documentos finais nas pastas de destino; ligação do Pull Request integrado; tabela de cobertura que indica a secção do documento que responde a cada exigência do conteúdo mínimo (Regulamento da UC, secção 19).

**Dependências**
I011 - Especificação de requisitos; I019 - Casos de uso; I057 e I058 - Estruturas base dos frontends.

**DoD aplicável**
DoD geral do grupo (m1-definition-of-done-v02.pdf); nome e pasta conformes com o Regulamento da UC (secções 18 e 19); relatório de revisão e checklist de auditoria arquivados; aceitação e confirmação final registadas na Issue.

**Normas, modelos e referências**
Regulamento de Funcionamento da UC (secções 16, 17, 18 e 19); Regulamento Interno do Grupo (secções 10, 11.5 e 11.6); m3-especificacao-requisitos-v02.md; m3-especificacoes-casos-uso-v02.md; m3-descricao-pipeline-ci-cd-v03.md (I050).

**Pasta de destino**
/projeto/03-artefactos-de-qualidade-de-produto/03.02-frontend-web/03.02.01-plano-de-testes
/projeto/03-artefactos-de-qualidade-de-produto/03.03-frontend-mobile/03.03.01-plano-de-testes
