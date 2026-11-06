# PLANO DE TESTES DO BACKEND

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m3 |
| Versão | v01 |
| Issue | I048 (Sprint 06) |
| Código de referência | Branch `main`, commit `7bf212c` (2026-10-30) |
| Documento complementar | Especificação dos Testes do Backend `m3-especificacao-testes-backend-v01.md` |

| Versão | Alterações |
|---|---|
| v01 | Primeira versão: âmbito, níveis e técnicas de teste, ambiente, dados, critérios de entrada, saída, suspensão e retoma, entregáveis, responsabilidades, calendário e riscos dos testes do backend em m3 |

## 1. Finalidade e âmbito

Este documento define como é testado o backend do DoseCerta na milestone m3: o que se testa e o que fica de fora, a que níveis, com que técnicas, ferramentas, dados e ambientes, com que critérios se decide que os testes terminaram com sucesso e quem faz o quê. A especificação dos testes (documento complementar) descreve cada caso de teste e a sua rastreabilidade aos requisitos e às operações da API. Os resultados, o relatório de cobertura e o relatório de análise estática são produzidos na Issue I049, seguindo este plano.

O objeto de teste é o backend (API REST em ASP.NET Core 8, camadas de aplicação, domínio e infraestrutura, processo periódico e base de dados SQL Server 2022) nas funcionalidades F001 a F011, sem a F007, retirada do âmbito por alteração autorizada de âmbito. Os frontends web e mobile são testados em m4, num plano próprio.

## 2. Referências

| Referência | Utilização |
|---|---|
| Especificação de Requisitos `m3-especificacao-requisitos-v02.md` | Requisitos a verificar: 96 requisitos funcionais ativos (RF-010 a RF-142, sem os da F007) e os não funcionais RNF-001 a RNF-057 |
| Documentação da API `m3-documentacao-api-v03.md` (Issue I042) | Contrato das 45 operações (OP-01 a OP-34 e OP-41 a OP-51), códigos de erro e comportamentos do servidor; base dos resultados esperados |
| Matriz de Permissões `m3-matriz-permissoes-ficha-v02.md` | Regras RP-xx de autorização por papel, verificadas por tabelas de decisão |
| Diagrama de estados da toma e das notificações (AD-001, Issue I014) | Transições TR-xx, verificadas por testes de transição de estados |
| Modelo de Dados `m3-modelo-de-dados-dosecerta-v03.md` e Modelo de Classes do Backend `m3-modelo-classes-backend-v03.md` (Issue I055) | Restrições da base de dados e passos do processo periódico |
| Descrição da Pipeline CI/CD `docs/ci-cd/m3-descricao-pipeline-ci-cd-v02.md` | Quality gates QG-01 a QG-05 e artefactos da pipeline |
| Regulamento Interno do Grupo, secções 11.2 a 11.6 e 14.2 | Ferramentas, análise estática, cobertura mínima, pipeline e segredos |
| Collection Postman `docs/postman/m3-collection-postman-v02.json` (Issues I036 e I041) | Testes de sistema da API |
| README do backend `backend/README.md`, secção 7 | Instruções de execução dos testes num computador limpo |

## 3. Itens de teste

| Item | Localização | Descrição |
|---|---|---|
| `DoseCerta.Api` | `backend/src/DoseCerta.Api` | Controllers, contratos, tradução de erros, autenticação JWT, verificação do estado, processo periódico |
| `DoseCerta.Application` | `backend/src/DoseCerta.Application` | Serviços com as regras de negócio, validação de dados e autorização por ficha |
| `DoseCerta.Domain` | `backend/src/DoseCerta.Domain` | Entidades e agregados (ficha, toma, tratamento, compromisso, convite, envio de e-mail) e calendário de Lisboa |
| `DoseCerta.Infrastructure` | `backend/src/DoseCerta.Infrastructure` | EF Core (contexto, mapeamento, migrações, unidade de trabalho), armazenamento de documentos, e-mail SMTP, segurança |
| Base de dados | Migrações do EF Core e `backend/scripts-base-dados` | 19 tabelas, 3 gatilhos, restrições e índices únicos |

A versão de referência deste plano é o commit `7bf212c` da `main`. Os testes acompanham o código: cada Pull Request que altera o backend corre todas as suites (secção 5.5).

## 4. Funcionalidades a testar e excluídas

### 4.1 A testar

| Área | Funcionalidades e operações | Requisitos |
|---|---|---|
| Autenticação e conta | F001 (OP-01 a OP-06) | RF-010 a RF-017 |
| Fichas, documentos, arquivo e eliminação | F002 (OP-07 a OP-17) | RF-020 a RF-029, RF-120 a RF-124 |
| Ficha própria | F003 (OP-09 com `propria=true`; regras da ficha própria em OP-19 e OP-24 a OP-26) | RF-030 a RF-034 |
| Associação de Cuidadores e Familiares | F004 (OP-18 a OP-26) | RF-040 a RF-049, RF-130 a RF-136 |
| Registo de medicação | F005 (OP-27 a OP-31) | RF-050 a RF-057 |
| Horários e confirmação de toma | F006 (OP-32 a OP-34) | RF-060 a RF-069 |
| Compromissos e agenda | F008 (OP-41 a OP-45) | RF-080 a RF-089, RF-116, RF-117 |
| Ocorrências e notas partilhadas | F009 (OP-46 a OP-49) | RF-090 a RF-095, RF-115 |
| Lista de tarefas diária | F010 (OP-50) | RF-100 a RF-102, RF-104, RF-105 |
| Notificações | F011 (OP-51, processo periódico, e-mail) | RF-110 a RF-113, RF-118, RF-119, RF-140 a RF-142 |
| Transversal | Autorização por papel, isolamento entre fichas, formato dos erros, atomicidade, concorrência, fuso de Lisboa, configuração, modelo de dados, verificação do estado (`GET /saude`) | RNF-001, RNF-003 a RNF-009, RNF-018, RNF-020, RNF-028, RNF-029, RNF-040, RNF-042 a RNF-044, RNF-046, RNF-047, RNF-049, RNF-054 |

### 4.2 Excluídas

| Exclusão | Justificação |
|---|---|
| F007 (tarefas de cuidados recorrentes; RF-070 a RF-078, RF-103 e RF-114) | Retirada do âmbito. Só se verifica que a retirada não deixa tabelas, gatilhos nem valores de restrição (testes da Issue I043) |
| Frontends web e mobile, incluindo a emissão do aviso local da toma (RF-062, RNF-041) e a interface da agenda (RF-088) | Implementados e testados em m4. Em m3 testa-se só a parte do backend: os dados de OP-34 e a agenda só de leitura |
| Testes de carga e de resistência (RNF-015, 50 Utilizadores em simultâneo; RNF-031, primeira resposta depois de 60 minutos sem pedidos) | Exigem ferramenta e ambiente de carga que o grupo não tem em m3. Fica registado como risco (R-05) |
| Cópias de segurança (RNF-045) e cifra da comunicação (RNF-002) | Pertencem à infraestrutura do ambiente de demonstração. O HTTPS é verificado no relatório de evidências de CI/CD (Issue I050) e as cópias no manual de instalação (Issue I051) |
| Entrega real de e-mails por SMTP | Os testes usam serviços de e-mail falsos ou controlados. O envio real verifica-se com o comando `--enviar-email-teste` (README do backend, secção 6) |

## 5. Abordagem

### 5.1 Níveis de teste

| Nível | Objetivo | Objeto e isolamento | Ferramentas | Quality gate |
|---|---|---|---|---|
| Unitário | Verificar as regras de negócio, as invariantes das entidades, a tradução dos erros e a configuração, sem dependências externas | Classes do domínio, serviços da aplicação, componentes da API e da infraestrutura, com duplos de teste (secção 5.3); sem base de dados nem rede | xUnit, Coverlet | QG-02 |
| Integração | Verificar cada operação da API de ponta a ponta, com o pipeline HTTP completo (autenticação, validação, autorização, tradução dos erros) e a persistência real | API arrancada em memória (`WebApplicationFactory`) contra um SQL Server 2022 real, com base de dados exclusiva criada pelas migrações e eliminada no fim; pasta de documentos temporária | xUnit, `Microsoft.AspNetCore.Mvc.Testing`, SQL Server 2022 | QG-03 |
| Sistema (API) | Verificar o contrato da API sobre o servidor a correr, numa sequência realista de utilização, por funcionalidade | Backend local (`https://localhost:7180`) ou ambiente de demonstração (URL público) | Postman ou newman, collection v02 | Não é gate da pipeline; é evidência das Issues I041 e I050 |
| Análise estática | Detetar defeitos e desvios das regras de codificação sem executar o código | Toda a solução, exceto `Migrations/` | Roslyn Analyzers (`AnalysisMode` Recommended), StyleCop, `dotnet format` | QG-05 |

A cobertura de linhas (QG-04) mede-se sobre os testes unitários e de integração em conjunto.

### 5.2 Técnicas de conceção

| Técnica | Onde se aplica | Exemplo na especificação |
|---|---|---|
| Partições de equivalência | Dados de entrada válidos e inválidos de cada operação: campos em falta, formato errado, enumerados desconhecidos | Campos inválidos indicados um a um (RNF-028) em OP-27, OP-41, OP-46 |
| Valores-limite | Tolerâncias das tomas (Leve 2 h, Moderado 1 h, Grave 30 min), 24 horas para `NaoConfirmada`, 60 minutos para os Familiares, intervalo máximo de 31 dias, tamanho máximo dos documentos (10 MB), antecedências válidas do aviso | O segundo antes e o segundo depois de cada tolerância |
| Tabelas de decisão | Autorização por papel (Cuidador principal, Cuidador, Familiar, não associado, convite pendente) e estado da ficha (ativa ou arquivada), segundo as regras RP-xx da Matriz de Permissões | `403` ao Familiar, `404` a quem não está associado, `409 FICHA_ARQUIVADA` |
| Transição de estados | Toma (TR-03 a TR-07 e TR-13 a TR-15), convite, tratamento (`EmCurso`, `Terminado`), ficha (`Ativa`, `Arquivada`) e envio de e-mail (tentativas, reenvios, não entregue) | Uma toma `NaoConfirmada` já não pode ser confirmada |
| Testes de contrato de erros | Cada código de erro com o código HTTP e o corpo comum da documentação da API | Mapa completo código de erro → HTTP |
| Concorrência | Pedidos simultâneos sobre o mesmo recurso | Duas confirmações da mesma toma dão uma `200` e uma `409` (RNF-044) |
| Injeção de falhas | Falha do armazenamento de documentos, da base de dados e do e-mail, para verificar a atomicidade e os reenvios | Documento que não é guardado não cria a ficha (RF-023, RNF-046) |
| Tempo controlado | Relógio fixo nos testes unitários; datas fora da janela móvel nos testes de integração; hora de verão e de inverno em Lisboa | Limites do dia, da semana e do mês na mudança de hora (RNF-047) |

### 5.3 Duplos de teste

Os testes unitários substituem as dependências externas por implementações de teste em `backend/tests/DoseCerta.UnitTests/Suporte`: repositórios e unidade de trabalho em memória (com variantes que revertem ou falham ao gravar), armazenamento de documentos em memória, serviço de e-mail falso, relógio fixo, hasher simples e emissor de tokens falso. Os testes de integração usam a implementação real, exceto onde é preciso provocar uma falha ou controlar o tempo: armazenamento que falha, repositório de ocorrências que falha e serviço de e-mail controlado (`backend/tests/DoseCerta.IntegrationTests/Suporte`).

### 5.4 Regras de escrita dos testes

- Cada teste tem um comentário XML `<summary>` que indica a condição testada, o resultado esperado e, quando se aplica, os requisitos (RF, RNF), as operações (OP), as regras de permissão (RP) e as transições (TR) que verifica. Este comentário é a fonte da rastreabilidade da especificação.
- Estrutura preparar, executar e verificar, com um único comportamento por teste. As variações do mesmo comportamento usam `[Theory]` com `[InlineData]`.
- Os testes são independentes entre si e da ordem de execução. Nos testes de integração, as classes partilham a mesma base de dados de execução (coleção xUnit `API com SQL Server`), mas cada teste cria os seus próprios Utilizadores e fichas, com e-mails únicos.
- Só se usam dados fictícios (secção 7).
- Não se desativa nenhum teste (`Skip`). Um teste que falha corrige-se no código ou no teste, nunca se remove (descrição da pipeline, secção 7).
- Os testes passam pela mesma análise estática do código de produção (QG-05).

### 5.5 Regressão

A pipeline `backend-ci` corre todas as suites em cada Pull Request para a `main` que altere `backend/**` e em cada integração na `main`. A suite completa é, portanto, a suite de regressão. A collection Postman é executada contra o backend local antes da entrega ao Revisor, sempre que a Issue altera a API, e contra o URL público depois da publicação.

## 6. Ambiente e ferramentas

| Elemento | Local (computador de cada aluno) | Pipeline (`backend-ci`) |
|---|---|---|
| Sistema e SDK | Windows ou Linux, .NET 8 SDK (`global.json`: 8.0.100 com `rollForward` `latestFeature`) | `ubuntu-latest`, .NET 8 SDK `8.0.x` |
| Base de dados dos testes de integração | SQL Server 2022 local ou contentor `mcr.microsoft.com/mssql/server:2022-latest`, indicado na variável `DOSECERTA_TESTES_SQLSERVER` | Contentor de serviço SQL Server 2022, criado e destruído em cada execução |
| Framework de testes | xUnit 2.9.3, `xunit.runner.visualstudio` 3.1.5, `Microsoft.NET.Test.Sdk` 17.14.1, `Microsoft.AspNetCore.Mvc.Testing` 8.0.31 | Igual |
| Cobertura | Coverlet (`coverlet.collector` 6.0.4) com `backend/coverage.runsettings`; ReportGenerator 5.5.11 | Igual, com o limiar de 70% verificado por `jq` |
| Análise estática | Roslyn Analyzers e StyleCop.Analyzers 1.2.0-beta.556, `TreatWarningsAsErrors`; `dotnet format` | Igual (`-warnaserror`, `--verify-no-changes`) |
| Testes de sistema | Postman ou newman, ambiente `m3-ambiente-local-postman-v02.json` (`--insecure`, certificado de desenvolvimento) | Não corre na pipeline; execução contra o URL público com `m3-ambiente-publico-postman-v01.json` |

A configuração dos testes de integração é fixada pela própria fábrica de testes (`FabricaApi`): ambiente `Testes`, processo periódico desligado (`appsettings.Testes.json`), migrações aplicadas pela fábrica, chave JWT de teste e pasta de documentos temporária. Os testes que precisam do processo periódico executam os seus passos diretamente.

## 7. Dados de teste

- Todos os dados são fictícios: nomes como «Maria Fictícia Santos», e-mails dos domínios reservados `exemplo.pt` e `example.com`, únicos em cada execução, contactos `+351 910 000 00x` e ficheiros mínimos gerados pelos próprios testes (PDF, PNG, JPG e texto, em `FicheirosTeste`).
- Os testes de integração criam uma base de dados `DoseCerta_Testes_<guid>` aplicando as migrações e eliminam-na no fim, juntamente com a pasta temporária dos documentos. Nenhum teste usa a base de dados de desenvolvimento nem a de demonstração.
- A collection Postman regista contas novas em cada execução, com o prefixo do ambiente e a data e hora. Não usa as contas do script de dados fictícios nem guarda tokens.
- Nenhum segredo está nos testes nem no repositório: a palavra-passe do SQL Server de testes vem da variável de ambiente local ou do GitHub Secret `CI_SQLSERVER_SA_PASSWORD`, e a chave JWT dos testes é uma constante que não é usada em nenhum ambiente real (Regulamento Interno do Grupo, secção 14.2).

## 8. Critérios

### 8.1 Entrada

- O código compila sem erros (QG-01) e sem avisos dos analisadores (QG-05).
- O SQL Server de testes está disponível e a variável `DOSECERTA_TESTES_SQLSERVER` está definida (local) ou o GitHub Secret existe (pipeline).
- Para os testes de sistema, o backend está a correr com a base de dados atualizada e responde `200` em `GET /saude`.

### 8.2 Saída (os testes do backend em m3 terminam com sucesso quando)

| N.º | Critério | Verificação |
|---|---|---|
| CS-01 | 100% dos testes unitários passam | QG-02, relatório TRX |
| CS-02 | 100% dos testes de integração passam | QG-03, relatório TRX |
| CS-03 | Cobertura de linhas igual ou superior a 70%, sobre os testes unitários e de integração em conjunto, sem `Migrations/` nem `Program.cs` | QG-04, relatório de cobertura |
| CS-04 | 0 ocorrências de severidade erro na análise estática e 0 diferenças de formatação | QG-05, logs da análise estática |
| CS-05 | Cada um dos 96 requisitos funcionais ativos tem pelo menos um caso de teste, ou uma justificação na especificação | Especificação, secção 8.1 |
| CS-06 | Cada uma das 45 operações da API é chamada por pelo menos um caso de teste de integração | Especificação, secção 8.3 |
| CS-07 | A collection Postman termina sem verificações falhadas contra o backend local e contra o URL público | Relatório do newman ou do Collection Runner |
| CS-08 | Os defeitos e as lacunas conhecidos estão registados com a decisão tomada (corrigir o código ou documentar) | Especificação, secção 9 |

### 8.3 Suspensão e retoma

| Situação | Suspensão | Retoma |
|---|---|---|
| A compilação ou a análise estática falha | Os testes não correm (a pipeline termina antes) | Depois da correção na branch do Executor |
| O SQL Server de testes não está disponível ou o segredo falta | Os testes de integração não correm | Quando o contentor ou a instância responder |
| Mais de um teste de integração falha pela mesma causa de ambiente (por exemplo, tempo esgotado na ligação) | A execução é repetida uma vez; se voltar a falhar, é investigada como defeito | Depois de corrigida a causa |
| O URL público não responde `200` em `GET /saude` | A execução da collection contra o URL público é adiada | Depois de uma publicação bem-sucedida da `backend-cd` |

## 9. Medição de desempenho

Os objetivos de desempenho da documentação da API (secção 3.4: RNF-010 a RNF-014 e RNF-017) não são quality gates da pipeline. Em m3 são verificados de forma indicativa, com os tempos de resposta registados pelo newman na execução da collection contra o URL público: OP-02 (2 s), OP-50 (2 s), OP-33 (1 s), OP-45 (3 s) e OP-34 (5 s). A collection só carrega ficheiros mínimos, por isso o carregamento de um documento de 5 MB em OP-15 (15 s) mede-se com um pedido isolado ao URL público, com um PDF fictício desse tamanho. Uma medição acima do objetivo é registada como observação no relatório de resultados (Issue I049), não como falha de teste. RNF-015 e RNF-031 não são medidos em m3 (secção 4.2, risco R-05).

## 10. Entregáveis

| Entregável | Issue | Localização no repositório |
|---|---|---|
| Plano de testes do backend (este documento) | I048 | `docs/testes/m3-plano-testes-backend-v01.md` |
| Especificação dos testes do backend | I048 | `docs/testes/m3-especificacao-testes-backend-v01.md` |
| Código dos testes unitários e de integração | Issues de cada funcionalidade | `backend/tests/` |
| Resultados dos testes (TRX), relatório de cobertura e relatório de análise estática | I049 | Artefacto `relatorios-backend-<n.º>` da pipeline, guardado em PDF na pasta oficial |
| Collection Postman e ambientes | I036, I041 | `docs/postman/` |
| Execução da collection contra o URL público | I050 | Relatório de evidências de CI/CD |
| Rastreabilidade dos requisitos aos testes | I052 | Requirements Traceability Matrix v03, a partir da secção 8 da especificação |

## 11. Responsabilidades

| Papel | Responsabilidade nos testes |
|---|---|
| Executor de cada Issue de implementação | Escreve os testes unitários e de integração da funcionalidade com o código, segundo este plano, e confirma localmente QG-01 a QG-05 antes de abrir o Pull Request (Definition of Done) |
| Revisor | Confirma que os testes cobrem os critérios de aceitação da Issue, que os resultados esperados correspondem à documentação da API e que a pipeline passou |
| Auditor | Confirma que as evidências de testes exigidas estão registadas |
| Executor da I048 (Ricardo André Santos Ribeiro) | Mantém este plano e a especificação |
| Executor da I049 (Eduardo Alves Santos) | Executa as suites sobre o código final integrado na `main` e produz os resultados, a cobertura e a análise estática |
| Pipeline `backend-ci` | Executa automaticamente QG-01 a QG-05 e guarda os relatórios durante 90 dias |

## 12. Calendário

| Data e hora | Marco | Issue |
|---|---|---|
| 2026-11-03 22:00 | Entrega do plano e da especificação ao Revisor | I048 |
| 2026-11-03 22:00 | Prazo das restantes tarefas que alteram documentação do backend | I042, I050, I051, I055 |
| 2026-11-04 15:00 | Resultados dos testes, cobertura e análise estática sobre o código final; RTM v03 | I049, I052 |
| 2026-11-06 15:00 | Sprint Review do Sprint 06 | - |
| 2026-11-06 23:59 | Data-limite da entrega da m3 | I053 |

## 13. Riscos e contingências

| N.º | Risco | Probabilidade e impacto | Mitigação ou contingência |
|---|---|---|---|
| R-01 | SQL Server de testes indisponível no computador de um aluno | Média, impacto baixo | Contentor Docker descrito no README; a pipeline corre sempre os testes de integração num contentor próprio |
| R-02 | Testes instáveis por dependerem da hora atual (meia-noite em Lisboa, mudança de hora) | Média, impacto médio | Relógio fixo nos testes unitários; datas de 2027 ou fora da janela móvel nos testes de integração; a collection não é executada nos últimos minutos do dia |
| R-03 | Alteração ao código depois da I048 sem atualização dos testes ou da especificação | Média, impacto médio | A pipeline bloqueia o merge se um teste falhar; a especificação indica o commit de referência e é atualizada numa nova versão se o código mudar antes da entrega |
| R-04 | Requisito implementado só em parte (RF-117) ou comportamento que diverge do contrato | Confirmado, impacto médio | Registado na secção 9 da especificação, com o caso de teste planeado; a correção é decidida pelo grupo numa Issue própria |
| R-05 | Desempenho com carga (RNF-015) e após inatividade (RNF-031) não verificado | Confirmado, impacto médio | Medição indicativa (secção 9); registado como limitação de m3 |
| R-06 | Cobertura elevada sem verificação suficiente dos resultados | Baixa, impacto médio | Cada teste verifica o código HTTP, o código de erro e o estado persistido; a revisão de cada Pull Request confirma as verificações |
| R-07 | Resultados oficiais (I049) dependentes do código final na `main` antes de 2026-11-04 15:00 | Média, impacto alto | Plano e especificação entregues em 2026-11-03; a I049 corre as suites logo que as Issues de código estejam integradas |

## 14. Situação de partida

Para confirmar que a especificação descreve testes que existem e se executam, as duas suites foram executadas em 2026-11-03 sobre o commit `7bf212c`, num computador Windows com o SQL Server 2022 em contentor Docker: passaram os 454 casos unitários e os 177 de integração (631 no total, 0 falhados, 0 ignorados), com 98,9% de cobertura de linhas e 93,8% de ramos (ReportGenerator 5.5.11). Estes números servem só de verificação da especificação; os resultados oficiais da m3 são os da Issue I049.
