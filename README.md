# DoseCerta

Projeto da unidade curricular Laboratório de Desenvolvimento de Software (LEI — ESTG/IPP), desenvolvido pelo Grupo G07.

## Estrutura do repositório

O grupo utiliza um único repositório (monorepo). Cada componente ocupa uma pasta própria e tem uma pipeline própria.

| Pasta | Conteúdo |
|---|---|
| `backend/` | API em ASP.NET Core 8 + Entity Framework Core 8 (SQL Server 2022) |
| `frontend-web/` | Aplicação web em Angular 18 |
| `frontend-mobile/` | Aplicação mobile em Flutter 3 |
| `docs/` | Documentação técnica em Markdown e diagramas PlantUML |
| `.github/` | Modelo de Pull Request e workflows do GitHub Actions |

## Regras de contribuição (Regulamento Interno do Grupo, secção 10)

A branch `main` é a única branch permanente. Está protegida e contém apenas trabalho revisto, auditado e aceite. Não se faz push direto para a `main`.

As branches temporárias são sempre criadas a partir da `main` e seguem o padrão `<tipo>/i<nº da Issue>-<descricao-curta>`, com os tipos `feature`, `fix`, `test` e `docs`. Exemplo: `feature/i014-renovacao-token`.

As mensagens de commit seguem Conventional Commits 1.0.0, com o código da Issue no fim da primeira linha: `<tipo>(<âmbito>): <descrição> [I0xx]`. Os tipos admitidos são `feat`, `fix`, `test`, `docs`, `refactor`, `chore` e `ci`. Exemplo: `feat(api): implementar registo de toma de medicacao [I005]`.

A integração é feita por Pull Request com squash merge, usando o modelo `.github/pull_request_template.md`. O Executor nunca aprova nem integra o seu próprio Pull Request. O merge é executado pelo Revisor, apenas depois do registo `AUTORIZAÇÃO DE ACEITAÇÃO` do Auditor na Issue.

## Segredos e configuração local

Os segredos nunca são versionados. Na pipeline usam-se GitHub Secrets; localmente usam-se ficheiros não versionados (por exemplo `appsettings.Development.json`) ou variáveis de ambiente, já incluídos no `.gitignore`.