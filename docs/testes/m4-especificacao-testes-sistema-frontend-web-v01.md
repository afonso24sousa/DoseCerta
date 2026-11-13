# ESPECIFICAÇÃO DOS TESTES DE SISTEMA DO FRONTEND WEB

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m4 |
| Versão | v01 |
| Issue | I063 (Sprint 07) |
| Artefacto da Checklist de Controlo de Artefactos | OF-M4-012 Especificação dos testes de sistema do frontend web |
| Pasta oficial | `03-artefactos-de-qualidade-de-produto/03.02-frontend-web/03.02.02-testes-de-sistema` |
| Plano de referência | `m4-plano-testes-frontend-web-v01.md` (I062) |
| Código de referência | Frontend web da I057 (`feature/i057-frontend-web-autenticacao`); pipeline da I061 (`feature/i061-pipeline-frontends`) |

| Versão | Alterações |
|---|---|
| v01 | Primeira versão: cenários de sistema da F001 (RF-010 a RF-017), automatizados com Cypress |

## 1. Âmbito

Esta especificação descreve os cenários de sistema do frontend web, segundo o plano de testes (secções 4.1, 5.2 e 5.4). A v01 cobre a F001 Autenticação e gestão de conta, nos ecrãs E01 Registo, E02 Início de sessão e E03 Conta. As funcionalidades F002 a F011 entram em versões seguintes.

## 2. Execução

| Elemento | Definição |
|---|---|
| Ferramenta | Cypress 13.17.0 (versão fixa em `frontend-web/package.json`) |
| Código dos testes | `frontend-web/cypress/e2e/f001-*.cy.ts`, apoio em `frontend-web/cypress/support/e2e.ts`, configuração em `frontend-web/cypress.config.ts` |
| Endereço da aplicação | `CYPRESS_baseUrl`; na frontend-web-ci, `https://localhost:4200` (`ng serve` com proxy `/api` para a API da execução) |
| Backend | API real (`DoseCerta.Api`) e SQL Server 2022 criados e destruídos em cada execução da frontend-web-ci (QG-06) |
| Localização dos elementos | Rótulo exato do campo (`<label for>`), texto do botão e papel acessível (`role="alert"`, `role="status"`), nunca a posição nem as classes de estilo (exceto `.cabecalho`, a região do cabeçalho) |
| Esperas | Pelas respostas da API (`cy.intercept` com alias) e pelos elementos; sem esperas fixas |
| Duplos | Nenhum: todos os cenários da F001 correm contra o backend real |
| Comando local | `npx cypress run` em `frontend-web/`, com a aplicação servida e a API ativa |
| Quality gate | QG-06: 100% dos cenários passam. A falha de um cenário termina a pipeline sem sucesso |

## 3. Dados de teste

- Só dados fictícios: nomes «Ana Fictícia», «Bruno Fictício» e «Carla Fictícia»; e-mails `<prefixo>.<carimbo>@exemplo.pt`.
- Cada cenário cria as suas contas no início, por `POST /api/v1/auth/registo`, com e-mail único por execução (`Date.now()` e um número aleatório). Os cenários são independentes da ordem e podem ser repetidos.
- As palavras-passe são geradas em cada execução (`Teste<4 algarismos>a1`), cumprem RNF-003 e não ficam no repositório.

## 4. Cenários

| ID | Requisito | Precondições | Dados fictícios | Passos | Resultado esperado |
|---|---|---|---|---|---|
| TS-W-F001-01 | RF-010 Criar conta | Sem sessão; e-mail ainda não registado | Nome «Ana Fictícia»; e-mail `registo.<carimbo>@exemplo.pt`; palavra-passe gerada válida | 1. Abrir `/registo` (E01). 2. Preencher Nome, E-mail e Palavra-passe. 3. Carregar em «Criar conta» | A API responde 201; a aplicação abre `/entrar` (E02) com a mensagem «Conta criada. Inicie sessão para continuar.» |
| TS-W-F001-02 | RF-011 Impedir conta com e-mail já registado | Sem sessão; conta criada pela API com o mesmo e-mail | Nome «Bruno Fictício»; e-mail `repetido.<carimbo>@exemplo.pt` já registado | 1. Abrir `/registo`. 2. Preencher os campos com o e-mail já registado. 3. Carregar em «Criar conta» | A API responde 409 (`EMAIL_JA_REGISTADO`); a aplicação fica em `/registo` e mostra «Já existe uma conta com este e-mail.» |
| TS-W-F001-03 | RF-012 Iniciar sessão (credenciais válidas) | Conta criada pela API; sem sessão | E-mail `login.<carimbo>@exemplo.pt` e a palavra-passe da conta | 1. Abrir `/entrar` (E02). 2. Preencher E-mail e Palavra-passe. 3. Carregar em «Entrar» | A aplicação abre `/hoje`; o cabeçalho mostra «Ana Fictícia»; a sessão fica em `sessionStorage` (`dosecerta.sessao`, RNF-054) |
| TS-W-F001-04 | RF-012 Iniciar sessão (credenciais inválidas) | Conta criada pela API; sem sessão | E-mail `invalido.<carimbo>@exemplo.pt`; palavra-passe errada `Errada123` | 1. Abrir `/entrar`. 2. Preencher o e-mail certo e a palavra-passe errada. 3. Carregar em «Entrar» | A API responde 401 (`CREDENCIAIS_INVALIDAS`); a aplicação fica em `/entrar` e mostra «E-mail ou palavra-passe incorretos.», sem indicar qual dos dados está errado |
| TS-W-F001-05 | RF-013 Consultar dados da conta; RF-014 Atualizar nome e e-mail | Conta criada pela API; sessão iniciada | Nome novo «Carla Fictícia»; e-mail novo `conta-nova.<carimbo>@exemplo.pt` | 1. Abrir `/conta` (E03). 2. Confirmar o nome e o e-mail atuais. 3. Substituir o Nome e o E-mail. 4. Carregar em «Guardar». 5. Recarregar a página | Os campos mostram os dados atuais; `PUT /api/v1/conta` responde 200; aparece «Dados da conta guardados.»; depois de recarregar, os campos mostram o nome e o e-mail novos |
| TS-W-F001-06 | RF-015 Alterar palavra-passe | Conta criada pela API; sessão iniciada | Palavra-passe atual da conta; nova palavra-passe gerada válida | 1. Abrir `/conta`. 2. Preencher «Palavra-passe atual» e «Nova palavra-passe». 3. Carregar em «Alterar palavra-passe». 4. Terminar sessão. 5. Iniciar sessão com a nova palavra-passe | Aparece «Palavra-passe alterada.»; o início de sessão com a nova palavra-passe abre `/hoje` |
| TS-W-F001-07 | RF-016 Terminar sessão; RF-017 Exigir sessão iniciada | Conta criada pela API; sessão iniciada | E-mail `logout.<carimbo>@exemplo.pt` | 1. Carregar em «Terminar sessão» no cabeçalho. 2. Abrir `/conta` diretamente | A aplicação abre `/entrar`; `dosecerta.sessao` deixa de existir no `sessionStorage`; o acesso a `/conta` sem sessão volta a `/entrar` |

## 5. Rastreabilidade

| Requisito | Cenários |
|---|---|
| RF-010 | TS-W-F001-01 |
| RF-011 | TS-W-F001-02 |
| RF-012 | TS-W-F001-03, TS-W-F001-04 |
| RF-013 | TS-W-F001-05 |
| RF-014 | TS-W-F001-05 |
| RF-015 | TS-W-F001-06 |
| RF-016 | TS-W-F001-07 |
| RF-017 | TS-W-F001-07 |
| RNF-054 | TS-W-F001-03, TS-W-F001-07 |

Os oito requisitos funcionais da F001 (RF-010 a RF-017) têm pelo menos um cenário (critério CS-W04 do plano, para a F001).
