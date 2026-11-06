# RELATÓRIO DE EVIDÊNCIAS DA PIPELINE CI/CD DO BACKEND

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m3 |
| Versão | v01 |
| Issue | I050 (Sprint 06) |
| Documento descrito | `m3-descricao-pipeline-ci-cd-v03.md` |
| Recolha das evidências | 2026-11-03, entre as 17:06 e as 17:55 (UTC) |

| Versão | Alterações |
|---|---|
| v01 | Primeira versão (Issue I050): execuções reais da `backend-ci` com sucesso e com falha bloqueante em cada quality gate (QG-01 a QG-05), prova do bloqueio do merge, regras de proteção da `main`, falha e correção da publicação contínua, publicação ignorada quando a CI não termina com sucesso, verificações do URL público e execução da collection Postman contra o URL público (tokens JWT ocultados) |

## 1. Finalidade e âmbito

Este relatório demonstra, com execuções reais do repositório `afonso24sousa/DoseCerta`, que a pipeline CI/CD descrita em `m3-descricao-pipeline-ci-cd-v03.md` (1) corre integralmente e com sucesso sobre a `main`; (2) termina sem sucesso e bloqueia o merge quando falha qualquer dos quality gates QG-01 a QG-05; (3) publica a versão integrada no ambiente de demonstração, com URL público. Cumpre o conteúdo mínimo do relatório de evidências da CCA (compromissos OF-M3-005 e OF-M3-006) e a secção 11.6 do Regulamento Interno do Grupo: um quality gate só é efetivo se o seu incumprimento faz a pipeline terminar sem sucesso e impede a integração.

## 2. Fontes e método

- Todos os números de execução, commits, datas e resultados vêm do GitHub, obtidos com o GitHub CLI: `gh run list --workflow <workflow>`, `gh run view <id> --json ...`, `gh run view <id> --log-failed`, `gh pr view <n> --json ...`, `gh api repos/{owner}/{repo}/branches/main/protection`, `gh api repos/{owner}/{repo}/rulesets/24299294`, `gh api repos/{owner}/{repo}/environments` e `gh secret list --env demonstracao`.
- As datas e horas estão em UTC, tal como o GitHub as devolve. Até 2026-10-25, a hora de Lisboa é UTC+1; a partir dessa data, é igual a UTC.
- Os commits estão abreviados a 7 carateres. A ligação de cada execução é `https://github.com/afonso24sousa/DoseCerta/actions/runs/<id>`.
- Não foi lido nem é reproduzido nenhum valor de segredo. Dos GitHub Secrets indicam-se só os nomes e a data da última atualização, que é o que o GitHub mostra.
- Os logs e os artefactos do GitHub Actions têm prazo de retenção. As evidências são guardadas em PDF e em capturas na pasta oficial (Regulamento Interno do Grupo, secção 15.2; secção 9 deste relatório).

## 3. Execuções reais

### 3.1 Integração contínua (`backend-ci`)

| Workflow | N.º | Commit | Branch / PR | Data e hora (UTC) | Resultado | Ligação |
|---|---|---|---|---|---|---|
| `backend-ci` | 56 | `6acc6a0` | `main` (push do squash merge do PR #75, I047) | 2026-10-29 00:34:15 a 00:36:01 | Sucesso | [40794915301](https://github.com/afonso24sousa/DoseCerta/actions/runs/40794915301) |
| `backend-ci` | 55 | `19ac833` | `feature/i047-notificacoes-atraso-tomas`, PR #75 | 2026-10-29 00:31:02 a 00:33:42 | Sucesso | [40794567699](https://github.com/afonso24sousa/DoseCerta/actions/runs/40794567699) |
| `backend-ci` | 5 | `2c3fe8c` | `feature/i030-modelo-dados-scripts`, PR #47 | 2026-10-16 17:32:09 a 17:33:06 | Falha no QG-01 | [38940430300](https://github.com/afonso24sousa/DoseCerta/actions/runs/38940430300) |
| `backend-ci` | 7 | `6966c58` | `main` (push do merge do PR #47) | 2026-10-16 17:38:26 a 17:39:35 | Falha no QG-01 | [38941233479](https://github.com/afonso24sousa/DoseCerta/actions/runs/38941233479) |
| `backend-ci` | 12 | `18cc085` | `main` (push do merge do PR #49) | 2026-10-19 12:31:59 a 12:32:55 | Falha no QG-01 | [39252791242](https://github.com/afonso24sousa/DoseCerta/actions/runs/39252791242) |
| `backend-ci` | 16 | `f1580e4` | `test/i031-falha-deliberada`, PR #52 | 2026-10-19 21:19:38 a 21:21:02 | Falha no QG-02 | [39326588469](https://github.com/afonso24sousa/DoseCerta/actions/runs/39326588469) |
| `backend-ci` | 57 | `96affdf` | `test/i050-falha-qg03`, PR #85 | 2026-11-03 17:33:29 a 17:35:45 | Falha no QG-03 | [41685398347](https://github.com/afonso24sousa/DoseCerta/actions/runs/41685398347) |
| `backend-ci` | 58 | `7afa2ae` | `test/i050-falha-qg04`, PR #86 | 2026-11-03 17:33:33 a 17:36:17 | Falha no QG-04 | [41685409390](https://github.com/afonso24sousa/DoseCerta/actions/runs/41685409390) |
| `backend-ci` | 27 | `63159ed` | `feature/i037-registo-medicacao`, PR #55 | 2026-10-23 14:49:11 a 14:52:02 | Falha no QG-05 | [39972884611](https://github.com/afonso24sousa/DoseCerta/actions/runs/39972884611) |
| `backend-ci` | 28 | `f42127e` | `feature/i037-registo-medicacao`, PR #55 | 2026-10-23 15:12:45 a 15:15:06 | Falha no QG-05 | [39976312542](https://github.com/afonso24sousa/DoseCerta/actions/runs/39976312542) |
| `backend-ci` | 29 | `2ec67cc` | `feature/i037-registo-medicacao`, PR #55 | 2026-10-23 15:18:40 a 15:21:02 | Falha no QG-05 | [39977164368](https://github.com/afonso24sousa/DoseCerta/actions/runs/39977164368) |
| `backend-ci` | 59 | `7bf212c` | `main`, `workflow_dispatch` (cancelada de propósito, secção 7.1) | 2026-11-03 17:53:10 a 17:54:44 | Cancelada | [41688514797](https://github.com/afonso24sousa/DoseCerta/actions/runs/41688514797) |

Histórico completo até 2026-11-03 (`gh run list --workflow backend-ci.yml --limit 200`): 58 execuções (n.º 2 a 59; a n.º 1 não consta da lista), das quais 45 com sucesso, 11 com falha e 2 canceladas (n.º 6 e 59). As falhas são as n.º 5, 7, 8, 11 e 12 (QG-01), 16 (QG-02), 27, 28 e 29 (QG-05), 57 (QG-03) e 58 (QG-04). Todas as execuções entre a n.º 30 e a n.º 56 terminaram com sucesso.

### 3.2 Execução integral com sucesso

A execução n.º 56, sobre a `main`, corre todos os passos da pipeline com sucesso, pela ordem da secção 4 da descrição:

| Passo | Resultado | Dados do log |
|---|---|---|
| `Verificar o segredo do SQL Server` | Sucesso | — |
| `QG-01 Compilação` | Sucesso | — |
| `QG-05 Análise estática (Roslyn Analyzers + StyleCop, avisos como erros)` | Sucesso | `Build succeeded.`, `0 Warning(s)`, `0 Error(s)` |
| `QG-05 Formatação (dotnet format --verify-no-changes)` | Sucesso | — |
| `QG-02 Testes unitários` | Sucesso | `Passed! - Failed: 0, Passed: 454, Skipped: 0, Total: 454` |
| `QG-03 Testes de integração (SQL Server em contentor de serviço)` | Sucesso | `Passed! - Failed: 0, Passed: 177, Skipped: 0, Total: 177` |
| `Relatório de cobertura (unitários + integração)` | Sucesso | — |
| `QG-04 Cobertura de linhas mínima` | Sucesso | `Cobertura de linhas: 98.8% (mínimo 70%)` |
| `Publicar relatórios de testes, cobertura e análise` | Sucesso | Artefacto `relatorios-backend-56` (1 086 320 bytes, disponível até 2027-01-27) |

## 4. Falhas bloqueantes por quality gate

Os 5 quality gates têm pelo menos uma execução real que termina sem sucesso no passo correspondente. Os passos seguintes ao que falha ficam *Skipped*, exceto a publicação dos relatórios, que corre sempre (`if: always()`).

| Quality gate | Execução | Origem da falha | Passo que falhou | Excerto do log |
|---|---|---|---|---|
| QG-01 | N.º 5 (também n.º 7, 8, 11 e 12) | Real: referências a `DbSet` ainda não criados no `DoseCertaDbContext` | `QG-01 Compilação` | `error CS1061: 'DoseCertaDbContext' does not contain a definition for 'Convites'` (12 erros) |
| QG-02 | N.º 16 | Deliberada, PR #52: a asserção `Assert.Equal` de um teste unitário existente foi alterada para falhar | `QG-02 Testes unitários` | `Failed DoseCerta.UnitTests.Infraestrutura.ConfiguracaoTestes.OpcoesJwtAceitaChaveComTamanhoMinimo`; `Failed! - Failed: 1, Passed: 48, Skipped: 0, Total: 49` |
| QG-03 | N.º 57 | Deliberada, PR #85: no teste de integração `SaudeRespondeSaudavelComBaseDeDados`, o código esperado passou de `HttpStatusCode.OK` para `HttpStatusCode.NoContent` | `QG-03 Testes de integração (SQL Server em contentor de serviço)` | `Expected: NoContent` / `Actual: OK`; `Failed! - Failed: 1, Passed: 176, Skipped: 0, Total: 177` |
| QG-04 | N.º 58 | Deliberada, PR #86: só nessa branch, `COBERTURA_MINIMA` subiu de `'70'` para `'100'`, acima da cobertura real; o código do backend não mudou e os QG-01, QG-02, QG-03 e QG-05 passaram | `QG-04 Cobertura de linhas mínima` | `Cobertura de linhas: 98.8% (mínimo 100%)`; `QG-04 falhou: cobertura de linhas 98.8% inferior a 100%.` |
| QG-05 | N.º 27 (também n.º 28 e 29) | Real: comentários de documentação mal formatados em `CodigosErro.cs` | `QG-05 Análise estática (Roslyn Analyzers + StyleCop, avisos como erros)` | `error SA1514: Element documentation header should be preceded by blank line`; `error SA1626` (n.º 27); `error SA1028: Code should not contain trailing whitespace` (n.º 29) |

Em todas estas execuções o passo termina com `Process completed with exit code 1`, o job `qualidade-backend` fica com o resultado `failure` e o check com o mesmo nome falha no Pull Request.

QG-04: a demonstração sobe o limiar em vez de baixar a cobertura. É a forma mais simples e não altera a `main` nem o código do backend: prova que o passo compara a cobertura medida (`Summary.json`) com o limiar e termina sem sucesso quando ela fica abaixo. Com o limiar real de 70%, a cobertura atual (98,8%) cumpre o critério.

## 5. Bloqueio do merge

| Pull Request | Branch | Check `qualidade-backend` | `mergeStateStatus` | `reviewDecision` | Estado final |
|---|---|---|---|---|---|
| [#52](https://github.com/afonso24sousa/DoseCerta/pull/52) | `test/i031-falha-deliberada` | `FAILURE` (execução n.º 16) | `BLOCKED` | `REVIEW_REQUIRED` | Fechado sem merge em 2026-10-19 21:24:15 |
| [#85](https://github.com/afonso24sousa/DoseCerta/pull/85) | `test/i050-falha-qg03` | `FAILURE` (execução n.º 57) | `BLOCKED` | `REVIEW_REQUIRED` | Fechado sem merge em 2026-11-03 17:37:31 |
| [#86](https://github.com/afonso24sousa/DoseCerta/pull/86) | `test/i050-falha-qg04` | `FAILURE` (execução n.º 58) | `BLOCKED` | `REVIEW_REQUIRED` | Fechado sem merge em 2026-11-03 17:37:33 |

Os valores foram lidos com `gh pr view <n> --json statusCheckRollup,mergeStateStatus,reviewDecision` e são a evidência do bloqueio: um Pull Request fechado já não mostra a caixa do merge, por isso não há captura dela. O check que falhou nos Pull Requests é o mesmo que a proteção da `main` exige: nome `qualidade-backend`, aplicação GitHub Actions `app_id` 15368, o mesmo valor de `required_status_checks.checks` (secção 6).

O merge destes Pull Requests está bloqueado por duas razões em simultâneo: o check obrigatório falhou e faltam as aprovações (`REVIEW_REQUIRED`). O check, sozinho, também bloqueia o merge. A regra de proteção exige o check `qualidade-backend` com sucesso (`required_status_checks`), e essa exigência aplica-se também aos administradores (`enforce_admins: true`). Por isso, um Pull Request aprovado continuaria `BLOCKED` enquanto o check estivesse em `failure`. Os 16 Pull Requests integrados depois do PR #52 (do #51 ao #77) tinham todos o check `qualidade-backend` com sucesso: os que alteram o backend pela `backend-ci`, os outros pela `backend-ci-sem-alteracoes`.

## 6. Regras de proteção da `main`

As duas camadas descritas na secção 6 da descrição, tal como a API do GitHub as devolve (lidas em 2026-11-03 17:37:57 UTC).

Regra de proteção da branch (`gh api repos/{owner}/{repo}/branches/main/protection`):

```json
{
  "url": "https://api.github.com/repos/afonso24sousa/DoseCerta/branches/main/protection",
  "required_status_checks": {
    "url": "https://api.github.com/repos/afonso24sousa/DoseCerta/branches/main/protection/required_status_checks",
    "strict": true,
    "contexts": [
      "qualidade-backend"
    ],
    "contexts_url": "https://api.github.com/repos/afonso24sousa/DoseCerta/branches/main/protection/required_status_checks/contexts",
    "checks": [
      {
        "context": "qualidade-backend",
        "app_id": 15368
      }
    ]
  },
  "required_pull_request_reviews": {
    "url": "https://api.github.com/repos/afonso24sousa/DoseCerta/branches/main/protection/required_pull_request_reviews",
    "dismiss_stale_reviews": false,
    "require_code_owner_reviews": false,
    "require_last_push_approval": false,
    "required_approving_review_count": 1
  },
  "required_signatures": {
    "url": "https://api.github.com/repos/afonso24sousa/DoseCerta/branches/main/protection/required_signatures",
    "enabled": false
  },
  "enforce_admins": {
    "url": "https://api.github.com/repos/afonso24sousa/DoseCerta/branches/main/protection/enforce_admins",
    "enabled": true
  },
  "required_linear_history": {
    "enabled": false
  },
  "allow_force_pushes": {
    "enabled": false
  },
  "allow_deletions": {
    "enabled": false
  },
  "block_creations": {
    "enabled": false
  },
  "required_conversation_resolution": {
    "enabled": false
  },
  "lock_branch": {
    "enabled": false
  },
  "allow_fork_syncing": {
    "enabled": false
  }
}
```

Ruleset `require-approvals` (`gh api repos/{owner}/{repo}/rulesets/24299294`):

```json
{
  "id": 24299294,
  "name": "require-approvals",
  "target": "branch",
  "source_type": "Repository",
  "source": "afonso24sousa/DoseCerta",
  "enforcement": "active",
  "conditions": {
    "ref_name": {
      "exclude": [],
      "include": [
        "~DEFAULT_BRANCH"
      ]
    }
  },
  "rules": [
    {
      "type": "deletion"
    },
    {
      "type": "non_fast_forward"
    },
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 2,
        "dismiss_stale_reviews_on_push": true,
        "required_reviewers": [],
        "require_code_owner_review": false,
        "require_last_push_approval": false,
        "required_review_thread_resolution": false,
        "require_extra_approval_for_unattributed_changes": true,
        "allowed_merge_methods": [
          "merge",
          "squash",
          "rebase"
        ]
      }
    }
  ],
  "node_id": "RRS_lACqUmVwb3NpdG9yec5S5GflzgFyxx4",
  "created_at": "2026-10-01T10:18:23.536+01:00",
  "updated_at": "2026-10-01T10:18:23.587+01:00",
  "bypass_actors": [],
  "current_user_can_bypass": "never",
  "_links": {
    "self": {
      "href": "https://api.github.com/repos/afonso24sousa/DoseCerta/rulesets/24299294"
    },
    "html": {
      "href": "https://github.com/afonso24sousa/DoseCerta/rules/24299294"
    }
  }
}
```

Em resumo: o check `qualidade-backend` é obrigatório e a branch tem de estar atualizada com a `main` (`strict: true`); as regras aplicam-se aos administradores (`enforce_admins: true`) e ninguém pode ignorar o ruleset (`bypass_actors` vazio, `current_user_can_bypass: never`); force push e eliminação estão bloqueados nas duas camadas. Quanto às aprovações, prevalece a camada mais exigente: o ruleset pede 2, a proteção da branch pede 1. Confirmação: os PRs #59, #75 e #77 foram integrados com 2 aprovações cada.

## 7. Publicação contínua (`backend-cd`)

### 7.1 Execuções

| Workflow | N.º | Commit | Branch / gatilho | Data e hora (UTC) | Resultado | Ligação |
|---|---|---|---|---|---|---|
| `backend-cd` | 1 | `9fd7a68` | `main`, `workflow_run` (backend-ci n.º 35, PR #59, I040) | 2026-10-23 17:03:53 a 17:04:02 | Falha em `Verificar os segredos` | [39991976489](https://github.com/afonso24sousa/DoseCerta/actions/runs/39991976489) |
| `backend-cd` | 2 | `a1b21d6` | `main`, `workflow_run` (backend-ci n.º 42) | 2026-10-28 13:33:46 a 13:33:56 | Falha em `Verificar os segredos` | [40697561329](https://github.com/afonso24sousa/DoseCerta/actions/runs/40697561329) |
| `backend-cd` | 3 | `172a7aa` | `main`, `workflow_run` (backend-ci n.º 44) | 2026-10-28 14:53:32 a 14:53:42 | Falha em `Verificar os segredos` | [40710418694](https://github.com/afonso24sousa/DoseCerta/actions/runs/40710418694) |
| `backend-cd` | 4 | `2cc947b` | `main`, `workflow_run` (backend-ci n.º 48) | 2026-10-28 23:34:49 a 23:34:59 | Falha em `Verificar os segredos` | [40788342593](https://github.com/afonso24sousa/DoseCerta/actions/runs/40788342593) |
| `backend-cd` | 5 | `d04619d` | `main`, `workflow_run` (backend-ci n.º 50) | 2026-10-28 23:44:30 a 23:44:40 | Falha em `Verificar os segredos` | [40789422693](https://github.com/afonso24sousa/DoseCerta/actions/runs/40789422693) |
| `backend-cd` | 6 | `df52d5b` | `main`, `workflow_run` (backend-ci n.º 52) | 2026-10-29 00:00:54 a 00:01:01 | Falha em `Verificar os segredos` | [40791220115](https://github.com/afonso24sousa/DoseCerta/actions/runs/40791220115) |
| `backend-cd` | 7 | `58921ec` | `main`, `workflow_run` (backend-ci n.º 54) | 2026-10-29 00:26:18 a 00:26:26 | Falha em `Verificar os segredos` | [40794052816](https://github.com/afonso24sousa/DoseCerta/actions/runs/40794052816) |
| `backend-cd` | 8 | `6acc6a0` | `main`, `workflow_run` (backend-ci n.º 56) | 2026-10-29 00:36:04 a 00:36:13 | Falha em `Verificar os segredos` | [40795113871](https://github.com/afonso24sousa/DoseCerta/actions/runs/40795113871) |
| `backend-cd` | 9 | `7bf212c` | `main`, `workflow_dispatch` | 2026-11-03 17:06:25 a 17:07:56 | Sucesso | [41680934294](https://github.com/afonso24sousa/DoseCerta/actions/runs/41680934294) |
| `backend-cd` | 10 | `7bf212c` | `main`, `workflow_run` (backend-ci n.º 59, cancelada) | 2026-11-03 17:54:47 a 17:54:49 | Ignorada (*Skipped*) | [41688770398](https://github.com/afonso24sousa/DoseCerta/actions/runs/41688770398) |

Cada execução n.º 1 a 8 foi desencadeada pela `backend-ci` com sucesso sobre o mesmo commit da `main`, como prevê a secção 10.2 da descrição. A `backend-cd` não tem nenhuma execução desencadeada por Pull Requests, nem pelas execuções n.º 57 e 58, porque a condição `branches: [main]` do `workflow_run` as exclui.

Publicação ignorada quando a CI não termina com sucesso, pelo procedimento da secção 10.7 da descrição: a `backend-ci` n.º 59 foi lançada na `main` com `workflow_dispatch` e cancelada durante o passo `QG-05 Formatação` (`conclusion: cancelled`). O fim dessa execução desencadeou a `backend-cd` n.º 10 (`workflow_run`), que terminou com `conclusion: skipped`: o job `publicar-demonstracao` ficou *Skipped*, sem executar nenhum passo (0 passos), porque a condição do job só aceita `conclusion == 'success'`. Nada foi enviado para o servidor. O resultado `failure` é tratado pela mesma condição.

### 7.2 Falha e causa

As 8 execuções sem sucesso terminam todas no passo `Verificar os segredos`, o segundo do job `publicar-demonstracao`. Em cada uma, `gh run view <id> --log-failed` mostra as mesmas 5 linhas de erro e o código de saída 1:

```text
##[error]Falta o GitHub Secret SSH_CHAVE_PRIVADA no environment demonstracao (ver m3-descricao-pipeline-ci-cd-v02.md).
##[error]Falta o GitHub Secret SSH_KNOWN_HOSTS no environment demonstracao (ver m3-descricao-pipeline-ci-cd-v02.md).
##[error]Falta o GitHub Secret MSSQL_SA_PASSWORD no environment demonstracao (ver m3-descricao-pipeline-ci-cd-v02.md).
##[error]Falta o GitHub Secret DOSECERTA_APP_PASSWORD no environment demonstracao (ver m3-descricao-pipeline-ci-cd-v02.md).
##[error]Falta o GitHub Secret JWT_CHAVE no environment demonstracao (ver m3-descricao-pipeline-ci-cd-v02.md).
##[error]Process completed with exit code 1.
```

Causa: os segredos tinham sido criados num environment chamado `demontracao` (sem o primeiro `s`), e o workflow usa `environment.name: demonstracao`. Quando um job referencia um environment que não existe, o GitHub cria-o vazio e o job corre sem segredos. Foi o que aconteceu aqui: segundo a API (`gh api repos/{owner}/{repo}/environments`), o environment `demonstracao` foi criado em 2026-10-23T17:03:54Z, um segundo depois de arrancar a execução n.º 1. Como o passo de verificação vem antes do SSH e do `rsync`, nenhuma destas execuções chegou ao servidor. A falha passou a constar da tabela de falhas da secção 10.6 da descrição.

### 7.3 Correção

Os 5 segredos obrigatórios foram recriados no environment `demonstracao`, com um novo par de chaves SSH dedicado à publicação. Não foi alterado nenhum ficheiro do repositório. Estado lido com `gh secret list --env demonstracao` (só nomes e data de atualização; o GitHub não devolve os valores):

| Segredo | Atualizado em (UTC) |
|---|---|
| `SSH_CHAVE_PRIVADA` | 2026-11-03 16:20:42 |
| `SSH_KNOWN_HOSTS` | 2026-11-03 16:41:10 |
| `MSSQL_SA_PASSWORD` | 2026-11-03 16:46:15 |
| `DOSECERTA_APP_PASSWORD` | 2026-11-03 16:47:10 |
| `JWT_CHAVE` | 2026-11-03 16:48:19 |

O repositório tem só um environment, `demonstracao`. A política de branches é personalizada, com uma única regra (`main`), e o environment não tem variáveis (`MSSQL_PID` não definida: SQL Server `Developer`). Os segredos SMTP opcionais não estão definidos. O segredo `CI_SQLSERVER_SA_PASSWORD` da integração contínua continua no repositório (2026-10-14 23:56:24).

### 7.4 Execução com sucesso depois da correção

A execução n.º 9 (`workflow_dispatch` na `main`, commit `7bf212c`) terminou com sucesso às 17:07:56. O job `publicar-demonstracao` correu das 17:06:30 às 17:07:56:

| Passo | Resultado |
|---|---|
| `Obter o código verificado pela backend-ci` | Sucesso |
| `Verificar os segredos` | Sucesso |
| `Configurar o acesso SSH ao servidor` | Sucesso |
| `Escrever o .env a partir dos segredos` | Sucesso |
| `Enviar backend/ e deploy/azure/ para o servidor` | Sucesso |
| `Publicar no servidor (SQL Server, init-db.sql, API e Caddy)` | Sucesso |
| `Esperar que /saude responda Saudavel` | Sucesso |
| `Aplicar os dados iniciais fictícios (só se ainda não existirem)` | Sucesso |
| `Resumo da publicação` | Sucesso |
| `Mostrar os logs da API (em caso de falha)` | *Skipped* (só corre em falha) |
| `Apagar a chave e o .env do runner` | Sucesso |

O commit `7bf212c` (PR #77, I041) só altera a documentação e a collection Postman. Por isso não desencadeou a `backend-ci`, e a publicação foi lançada à mão, como prevê a secção 10.2 da descrição. O backend publicado é o que a `backend-ci` n.º 56 verificou: entre `6acc6a0` e `7bf212c` não há alterações em `backend/**`.

### 7.5 Verificações do URL público

Feitas a partir de um PC fora do Azure em 2026-11-03, às 17:25 UTC:

| Verificação | Comando | Resultado |
|---|---|---|
| Estado da API e da base de dados | `curl -s -w '\nHTTP %{http_code}\n' https://dosecerta.spaincentral.cloudapp.azure.com/saude` | `{"estado":"Saudavel","verificacoes":{"baseDados":"Saudavel"}}`, `HTTP 200` |
| Redirecionamento do HTTP | `curl -s -o /dev/null -D - http://dosecerta.spaincentral.cloudapp.azure.com/saude` | `HTTP/1.1 308 Permanent Redirect`, `Location: https://dosecerta.spaincentral.cloudapp.azure.com/saude` |
| Porta 1433 fechada para fora | `Test-NetConnection dosecerta.spaincentral.cloudapp.azure.com -Port 1433` | `RemoteAddress : 158.158.32.64`, `TcpTestSucceeded : False` |
| Porta 443 aberta (controlo) | `Test-NetConnection dosecerta.spaincentral.cloudapp.azure.com -Port 443` | `TcpTestSucceeded : True` |

### 7.6 Collection Postman contra o URL público

Execução da collection `m3-collection-postman-v02.json` (Issue I041) com o ambiente `m3-ambiente-publico-postman-v01.json`, em 2026-11-03 das 17:27:08 às 17:27:50 UTC. Fica assim resolvida a pendência da I041 (execução contra o URL público). Comando, a partir de `docs/postman`:

```text
npx -p newman@6.2.1 -p newman-reporter-htmlextra@1.23.1 newman run m3-collection-postman-v02.json -e m3-ambiente-publico-postman-v01.json -r cli,htmlextra,junit
```

| | Executados | Falhados |
|---|---|---|
| Iterações | 1 | 0 |
| Pedidos | 130 | 0 |
| Scripts de teste | 128 | 0 |
| Scripts de pre-request | 131 | 0 |
| Asserções | 277 | 0 |

Duração total: 23,2 s; tempo médio de resposta: 66 ms (mínimo 31 ms, máximo 852 ms).

Relatórios, na mesma pasta deste documento:

- `m3-s06-i050-newman-publico-20261103.html` (relatório htmlextra);
- `m3-s06-i050-newman-publico-20261103.xml` (JUnit: 128 casos, 0 falhas).

Dados criados no servidor de demonstração (todos fictícios), com o prefixo `postman.publico.1793726846737` gerado pela collection nesta execução:

| E-mail | O que é |
|---|---|
| `postman.publico.1793726846737.a@exemplo.pt` | Conta do Utilizador A, registada no OP-01 |
| `postman.publico.1793726846737.a2@exemplo.pt` | Novo e-mail da conta do Utilizador A, depois do OP-05 (Atualizar dados da conta) |
| `postman.publico.1793726846737.b@exemplo.pt` | Conta do Utilizador B |
| `postman.publico.1793726846737.c@exemplo.pt` | Endereço só convidado, sem conta |

Estas contas, e as fichas e os registos que a collection cria, ficam no servidor depois da execução, como diz a descrição da collection. As palavras-passe são as fictícias do ambiente público versionado.

Ocultação dos tokens: o relatório HTML gerado pelo `newman` incluía os tokens JWT emitidos para estas contas fictícias (cabeçalhos `Authorization` e respostas do início de sessão). Antes da versão no repositório, os 119 tokens foram substituídos pelo texto `[JWT REMOVIDO]`. Uma pesquisa por `eyJ` (início de qualquer token JWT) devolve 0 ocorrências nos dois ficheiros; o relatório JUnit (`.xml`) não continha tokens. O resto do relatório não foi alterado.

## 8. Observações

| N.º | Observação | Tratamento |
|---|---|---|
| O1 | As mensagens de erro dos workflows remetem para versões anteriores da descrição: a `backend-ci` para `m3-descricao-pipeline-ci-cd-v01.md` e a `backend-cd` (cabeçalho e passo `Verificar os segredos`) para `m3-descricao-pipeline-ci-cd-v02.md`. O comportamento não é afetado | Os workflows não foram alterados na I050; atualizar as referências numa próxima alteração dos workflows |
| O2 | Os PRs #47 (I030) e #49 (revert do #48) foram integrados com o check `qualidade-backend` em `FAILURE` no QG-01 (execuções n.º 5 e 11): o #47 em 2026-10-16 17:37:34, o #49 em 2026-10-19 12:31:56. As execuções n.º 7 e 12, sobre a `main` depois desses merges, falharam também. Os dois tinham 2 aprovações: o ruleset `require-approvals`, que exige as aprovações e não o check, tem uma única versão, de 2026-10-01 10:18:23 (+01:00), ativa desde então (`gh api .../rulesets/24299294/history`). A API da proteção da branch não devolve a data em que o check obrigatório foi configurado. O histórico mostra que a exigência do check não foi aplicada a esses dois merges e que já estava em vigor em 2026-10-23 17:33:12, quando o PR #63 (I031) descreve o PR #60 bloqueado à espera do check obrigatório `qualidade-backend`. Os dois merges são, portanto, anteriores à proteção efetiva do check. O histórico não distingue se a regra ainda não existia ou se ainda não se aplicava a quem fez o merge (`enforce_admins`); a data exata só pode ser confirmada no registo de segurança da conta do dono do repositório (*Settings > Security log*, ação `protected_branch`). Desde o PR #52 (2026-10-19 21:19), todos os Pull Requests integrados tiveram o check `qualidade-backend` com sucesso (secção 5) | Registado como histórico, sem alteração; a configuração atual (secção 6) impede este caso, como provam os PRs #85 e #86 |
| O3 | Não havia nenhuma execução da `backend-cd` com o job *Skipped* depois de uma `backend-ci` sem sucesso na `main`, porque as falhas da `backend-ci` na `main` (n.º 7 e 12) são anteriores à criação da `backend-cd` | Fechada: evidência recolhida em 2026-11-03 com a `backend-ci` n.º 59 (cancelada) e a `backend-cd` n.º 10 (*Skipped*) (secção 7.1) |
| O4 | O `mergeStateStatus: BLOCKED` dos Pull Requests de demonstração tem duas causas em simultâneo: o check falhado e as aprovações em falta (`REVIEW_REQUIRED`) | Explicado na secção 5 |

## 9. Capturas a tirar pelo Executor

Cada captura é guardada em PNG na pasta oficial, com o nome indicado. Nas capturas das definições e dos environments, nenhum valor de segredo pode ficar visível (o GitHub mostra só os nomes).

| N.º | Ficheiro | O que mostra | Onde |
|---|---|---|---|
| 1 | `m3-s06-i050-ci-56-sucesso-main.png` | Execução n.º 56 com todos os passos a verde | Actions > backend-ci > n.º 56 > job `qualidade-backend` |
| 2 | `m3-s06-i050-ci-56-resumo-cobertura.png` | Separador Summary com o resumo de cobertura e o artefacto `relatorios-backend-56` | Actions > backend-ci > n.º 56 > Summary |
| 3 | `m3-s06-i050-ci-05-qg01-falha.png` | Passo `QG-01 Compilação` a vermelho, com o erro `CS1061` | Actions > backend-ci > n.º 5 |
| 4 | `m3-s06-i050-ci-16-qg02-falha.png` | Passo `QG-02 Testes unitários` a vermelho, com o teste falhado | Actions > backend-ci > n.º 16 |
| 5 | `m3-s06-i050-ci-57-qg03-falha.png` | Passo `QG-03 Testes de integração` a vermelho, com `Expected: NoContent` / `Actual: OK` | Actions > backend-ci > n.º 57 |
| 6 | `m3-s06-i050-ci-58-qg04-falha.png` | Passo `QG-04 Cobertura de linhas mínima` a vermelho, com `98.8% inferior a 100%` | Actions > backend-ci > n.º 58 |
| 7 | `m3-s06-i050-ci-27-qg05-falha.png` | Passo `QG-05 Análise estática` a vermelho, com `SA1514` | Actions > backend-ci > n.º 27 |
| 8 | `m3-s06-i050-protecao-main.png` | Regra de proteção da `main`, com o check obrigatório `qualidade-backend` | Settings > Branches > `main` > Edit |
| 9 | `m3-s06-i050-ruleset-require-approvals.png` | Ruleset `require-approvals` ativo, com 2 aprovações | Settings > Rules > Rulesets > `require-approvals` |
| 10 | `m3-s06-i050-cd-lista-execucoes.png` | Lista das execuções n.º 1 a 10 da `backend-cd` | Actions > backend-cd |
| 11 | `m3-s06-i050-cd-01-segredos-em-falta.png` | Passo `Verificar os segredos` a vermelho, com as 5 linhas `Falta o GitHub Secret` | Actions > backend-cd > n.º 1 |
| 12 | `m3-s06-i050-environment-demonstracao.png` | Environment `demonstracao` com a regra de branch `main` e os nomes dos 5 segredos | Settings > Environments > demonstracao |
| 13 | `m3-s06-i050-cd-09-sucesso.png` | Execução n.º 9 com todos os passos a verde | Actions > backend-cd > n.º 9 > job `publicar-demonstracao` |
| 14 | `m3-s06-i050-cd-09-resumo.png` | Separador Summary da n.º 9 (URL, commit `7bf212c` e gatilho) | Actions > backend-cd > n.º 9 > Summary |
| 15 | `m3-s06-i050-deployment-demonstracao.png` | Deployment ativo do environment `demonstracao` | Página principal do repositório > Deployments |
| 16 | `m3-s06-i050-ci-59-cancelada.png` | Execução n.º 59 da `backend-ci` na `main`, cancelada | Actions > backend-ci > n.º 59 |
| 17 | `m3-s06-i050-cd-10-skipped.png` | Execução n.º 10 da `backend-cd`, com o job `publicar-demonstracao` *Skipped* | Actions > backend-cd > n.º 10 |
| 18 | `m3-s06-i050-saude-https.png` | Resposta `Saudavel` de `https://dosecerta.spaincentral.cloudapp.azure.com/saude` | Browser ou terminal (`curl`) |
| 19 | `m3-s06-i050-http-308.png` | Resposta `308 Permanent Redirect` do HTTP | Terminal (`curl -D -`) |
| 20 | `m3-s06-i050-porta-1433-fechada.png` | `Test-NetConnection ... -Port 1433` com `TcpTestSucceeded : False` | PowerShell |
| 21 | `m3-s06-i050-newman-publico-resumo.png` | Resumo do relatório `m3-s06-i050-newman-publico-20261103.html` (130 pedidos, 277 asserções, 0 falhas) | Browser |

Não há capturas da caixa do merge: os Pull Requests #52, #85 e #86 estão fechados e o GitHub já não a mostra. A evidência do bloqueio é o `mergeStateStatus: BLOCKED` devolvido pela API, registado na secção 5.

## 10. Cobertura

| Exigência | Origem | Onde é cumprida |
|---|---|---|
| CA1: a descrição explica as etapas, os gatilhos, os quality gates QG-01 a QG-05, a gestão de segredos e a publicação no URL público | Backlog de Projeto, I050 | Descrição v03: secções 3, 4, 8 e 10 |
| CA2: o relatório apresenta execuções identificadas com sucesso e pelo menos uma falha bloqueante | Backlog de Projeto, I050 | Secções 3 e 4 (sucesso: n.º 55 e 56; falhas bloqueantes nos 5 quality gates) |
| CA2: o relatório apresenta as regras de proteção da `main` | Backlog de Projeto, I050 | Secção 6 |
| CA3: as evidências correspondem a execuções reais do repositório | Backlog de Projeto, I050 | Secções 2, 3, 5 e 7 (número, commit, data e ligação de cada execução, obtidos do GitHub) |
| CA4: US-I050-01, descrição com etapas, gatilhos, quality gates e publicação | Backlog de Projeto, I050 | Descrição v03: secções 3, 4, 6 e 10 |
| CA4: US-I050-02, evidências com sucesso e com falha bloqueante | Backlog de Projeto, I050 | Secções 3, 4 e 5 |
| Evidência: documentos na pasta de destino | Backlog de Projeto, I050 | `docs/ci-cd/` (pasta correspondente a `/projeto/04-artefactos-tecnicos/04.16-ci-cd`): descrição v03, este relatório e os relatórios do `newman` |
| Evidência: ligação do PR integrado | Backlog de Projeto, I050 | Pull Request da I050 (registado na Issue depois do merge) |
| Evidência: tabela de cobertura | Backlog de Projeto, I050 | Esta secção e a secção 11 da descrição v03 |
| Descrição: etapas, gatilhos, ambientes, testes, análise estática, quality gates e tratamento de falhas | CCA, OF-M3-005 | Descrição v03: secções 2 a 4, 7, 10.1 a 10.3 e 10.6 (correspondência na secção 11) |
| Relatório: demonstra a execução integral da pipeline | CCA, OF-M3-006 | Secção 3.2 (CI n.º 56, todos os passos) e secção 7.4 (CD n.º 9, todos os passos) |
| Relatório: demonstra o bloqueio correto das situações de erro | CCA, OF-M3-006 | Secções 4 e 5 (um quality gate de cada vez); secção 7.1 (a publicação é ignorada quando a CI não termina com sucesso: CI n.º 59 e CD n.º 10); secções 7.1 e 7.2 (a publicação para quando faltam os segredos) |
| Um quality gate só é efetivo se o seu incumprimento faz a pipeline terminar sem sucesso e impede a integração | Regulamento Interno do Grupo, secção 11.6 | Secção 4 (a pipeline termina sem sucesso em cada gate) e secções 5 e 6 (check obrigatório falhado e merge bloqueado); descrição v03, secção 6 |
| Pendência da I041: collection Postman contra o URL público | Issue I041 | Secção 7.6 |
