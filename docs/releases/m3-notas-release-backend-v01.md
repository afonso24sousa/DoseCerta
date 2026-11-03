# NOTAS DE RELEASE DO BACKEND

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m3 |
| Versão do documento | v01 |
| Issue | I051 (Sprint 06) |
| Versão do backend | **v0.3.0-m3** |
| Data do documento | 2026-11-03 |
| Release prevista | Com a entrega da m3, a 2026-11-06 |
| Tag Git | `v0.3.0-m3`, por criar depois do controlo final (Regulamento Interno do Grupo, secção 10.5) |

| Versão | Alterações |
|---|---|
| v01 | Primeira versão (Issue I051): versão v0.3.0-m3 do backend, com as funcionalidades F001 a F011 sem a F007, as alterações em relação a m2, as correções, as limitações conhecidas, a compatibilidade e as instruções relevantes |

## 1. Identificação da versão

| Elemento | Valor |
|---|---|
| Componente | Backend (API REST): ASP.NET Core 8, Entity Framework Core 8 e SQL Server 2022 |
| Versão | v0.3.0-m3, primeira release do backend (Regulamento da UC, secção 19: uma release do backend em m3 e uma do sistema completo em m4) |
| Conteúdo | Os commits da `main` integrados até à tag. À data deste documento, a `main` está no commit `7bf212c` (PR #77, I041, 2026-10-30) |
| Repositório | https://github.com/afonso24sousa/DoseCerta, pasta `backend/` |
| Ambiente de demonstração | https://dosecerta.spaincentral.cloudapp.azure.com (verificação do estado em `/saude`) |
| Documentação da API | `m3-documentacao-api-v02.md` (`/projeto/04-artefactos-tecnicos/04.11-documentacao-de-api`); prefixo `/api/v1` |
| Manual de instalação e configuração | `docs/instalacao/m3-manual-instalacao-configuracao-v01.md` |
| Pipeline CI/CD | `docs/ci-cd/m3-descricao-pipeline-ci-cd-v03.md` e relatório de evidências `docs/ci-cd/m3-relatorio-evidencias-pipeline-ci-cd-v01.md` |

## 2. Funcionalidades entregues

A versão entrega as funcionalidades F001 a F011, sem a F007, com as operações da documentação da API v02 (secção 6). A F007 (tarefas de cuidados recorrentes) foi retirada do âmbito por alteração autorizada de âmbito de 2026-10-19 (Regulamento da UC, secção 7.5). A autorização do docente chegou por e-mail a 2026-10-20 e a migração `RetirarTarefasRecorrentes`, que retira a F007 do esquema, é dessa data. As operações OP-35 a OP-40 deixaram de existir e os seus identificadores não são reutilizados.

| Funcionalidade | Operações | Issues e Pull Requests |
|---|---|---|
| F001 Autenticação e gestão de conta | OP-01 Registar conta, OP-02 Iniciar sessão, OP-03 Terminar sessão, OP-04 Consultar dados da conta, OP-05 Atualizar dados da conta, OP-06 Alterar palavra-passe | I032, PR #51 |
| F002 Criação e gestão de fichas de utentes | OP-07 Listar fichas, OP-08 Obter as listas de necessidades, OP-09 Criar ficha, OP-10 Consultar ficha, OP-11 Editar ficha, OP-12 Arquivar ficha, OP-13 Reativar ficha, OP-14 Eliminar ficha, OP-15 Carregar documento, OP-16 Listar documentos, OP-17 Descarregar documento | I033, PR #53 |
| F003 Gestão da ficha própria | OP-09 com ficha própria (uma por Utilizador, RF-030 e RF-031); as restantes operações das fichas aplicam-se à ficha própria (RF-032); proteção do titular nas OP-19, OP-24, OP-25 e OP-26 (RF-033 e RF-034) | I034, PR #56; proteção pela API na I035 |
| F004 Associação de Cuidadores e Familiares a uma ficha | OP-18 Listar pessoas associadas, OP-19 Convidar pessoa, OP-20 Listar convites recebidos, OP-21 Aceitar convite, OP-22 Recusar convite, OP-23 Promover Familiar a Cuidador, OP-24 Despromover Cuidador a Familiar, OP-25 Remover acesso, OP-26 Transferir o papel de Cuidador principal | I035, PR #57 |
| F005 Registo de medicação | OP-27 Registar tratamento, OP-28 Listar tratamentos, OP-29 Consultar tratamento, OP-30 Atualizar tratamento, OP-31 Terminar tratamento antecipadamente | I037, PR #55 |
| F006 Horários e alarmes com confirmação de toma | OP-32 Listar tomas da ficha, OP-33 Confirmar toma, OP-34 Sincronizar o calendário de tomas | I038, PR #61 |
| F007 Tarefas de cuidados recorrentes | Retirada do âmbito (OP-35 a OP-40 não existem) | I037, PR #55; I043, PR #65 |
| F008 Agenda de consultas e calendário de cuidados | OP-41 Registar compromisso, OP-42 Listar compromissos, OP-43 Editar compromisso, OP-44 Remover compromisso, OP-45 Consultar agenda | I044, PR #74 |
| F009 Ocorrências diárias e notas partilhadas | OP-46 Registar ocorrência, OP-47 Registar nota partilhada, OP-48 Consultar ocorrências, OP-49 Consultar notas partilhadas | I045, PR #67 |
| F010 Lista de tarefas diária consolidada | OP-50 Consultar lista de tarefas diária | I046, PR #73 |
| F011 Notificações | OP-51 Consultar histórico de notificações; no servidor, o processo periódico de 30 em 30 segundos deteta as tomas em atraso e notifica os Cuidadores (e os Familiares, no grau Grave), envia os e-mails das notificações e dos convites com reenvio, termina os tratamentos com a data de fim ultrapassada e passa a não confirmadas as tomas com 24 horas | I047, PR #75 |

Também fazem parte da versão:

| Elemento | Issues e Pull Requests |
|---|---|
| Solução .NET 8 em camadas (Api, Application, Domain, Infrastructure), autenticação JWT, `GET /saude`, serviço de e-mail e projetos de testes | I029, PR #45 |
| Modelo de dados: 19 tabelas e 3 gatilhos, migrações do EF Core e scripts SQL com dados fictícios | I030, PR #47 e PR #50 |
| Pipeline de integração contínua com os quality gates QG-01 a QG-05 e bloqueio do merge | I031, PR #63 (check em PR sem alterações no backend) |
| Publicação contínua no ambiente de demonstração, com URL público | I040, PR #59 |
| Collection Postman de F001 a F011, sem a F007, com ambientes local e público | I036, PR #60; I041, PR #77 |

## 3. Alterações em relação a m2

Em m2 não houve código: foram entregues a especificação de requisitos, as especificações dos casos de uso, a documentação de arquitetura, a documentação da API v01, o modelo de classes do backend v01 e o modelo de dados v01. A v0.3.0-m3 é, por isso, a primeira versão com código do backend. Em relação ao que m2 especificava:

- O backend implementa a documentação da API v02, que substitui a v01 de m2.
- A F007 foi retirada do âmbito (secção 2): o esquema final não tem as 4 tabelas nem o gatilho da F007, e os requisitos RF-070 a RF-078, RF-103 e RF-114 estão descontinuados.
- O esquema segue o modelo de dados v02: 19 tabelas e 3 gatilhos (`TR_Toma_Historico`, `TR_Ocorrencia_Imutavel` e `TR_Nota_Imutavel`), criados pelas migrações `Inicial` e `RetirarTarefasRecorrentes` ou pelo script `01-criar-base-dados.sql`.
- Passou a existir a pipeline CI/CD: integração contínua em cada Pull Request e em cada integração na `main`, e publicação automática no ambiente de demonstração.

## 4. Correções relevantes

| Correção | Issue e Pull Request |
|---|---|
| Os testes, o contexto de dados e o README perdidos no merge da I030 foram repostos | I030, PR #50 (2026-10-19) |
| A primeira integração da autenticação (PR #48) foi revertida (PR #49) e integrada de novo, completa, no PR #51 | I032, PRs #48, #49 e #51 |
| Divergências entre a `main` e a documentação: das divergências levantadas para OP-01 a OP-31 e para o `/saude`, 2 eram do código e foram corrigidas. D01: os tratamentos com a data de fim ultrapassada passam a ser terminados pelo processo periódico (RF-056). D02: as credenciais terminadas já expiradas passam a ser eliminadas (RF-016). As restantes eram erros da documentação, encaminhados para a I042 | I054, PR #64 (2026-10-28) |
| Conclusão da retirada da F007 no código, nos testes e nos artefactos | I043, PR #65 (2026-10-28) |
| Os Pull Requests sem alterações no backend deixam de ficar com o check obrigatório pendente | I031, PR #63 (2026-10-23) |
| A publicação automática terminava sem sucesso desde 2026-10-23, porque os segredos estavam num environment com o nome errado. Os segredos foram recriados no environment `demonstracao` e a publicação voltou a concluir com sucesso (execução n.º 9 da `backend-cd`, 2026-11-03), sem alterações ao código | I050 (relatório de evidências da pipeline, secção 7) |

## 5. Limitações conhecidas

| N.º | Limitação | Pormenor |
|---|---|---|
| L1 | **RF-117 (notificar antes de um compromisso) por implementar** | Os compromissos guardam a antecedência do aviso (`antecedenciaAvisoMinutos`, OP-41 e OP-43) e o esquema aceita notificações `ProximoCompromisso`, mas nenhum serviço as cria. O PR #74 (I044, F008) deixou o envio do aviso para a I047 (F011), e o PR #75 (I047) não o implementou. As restantes notificações da F011 funcionam |
| L2 | **Sem cópias de segurança no ambiente de demonstração** | A base de dados e os documentos carregados estão em volumes e pastas da máquina virtual, sem nenhuma cópia de segurança automática. O PR #59 (I040) deixou-as fora do âmbito, para uma Issue futura (RNF-045) |
| L3 | SMTP não configurado no ambiente de demonstração | Os segredos SMTP do environment `demonstracao` não estão definidos: a API arranca sem enviar e-mails e regista um aviso no log, e os convites e as notificações são criados na mesma (descrição da pipeline v03, secção 8) |
| L4 | Sem notificações push nem SMS | As notificações são guardadas na aplicação (OP-51) e enviadas por e-mail. Os alarmes das tomas no telemóvel dependem da OP-34 e do frontend mobile (m4) |
| L5 | Sem frontends | O frontend web (Angular) e o frontend mobile (Flutter) são entregues em m4. Em m3, a API usa-se pela collection Postman, pelo Swagger (só no ambiente `Development`) ou diretamente |
| L6 | Publicação automática só com alterações no backend | A `backend-cd` só corre depois de uma `backend-ci` com sucesso na `main`, e a `backend-ci` só corre com alterações em `backend/**`. Uma alteração só em `deploy/azure/` exige lançar a `backend-cd` à mão (descrição da pipeline v03, secção 10.2). Não há reposição automática da versão anterior se uma publicação falhar |
| L7 | Collection Postman | Não deve ser executada nos últimos minutos do dia (hora de Lisboa) e não testa `TOMA_NAO_CONFIRMAVEL` nem as notificações que dependem de esperar pelas horas-limite (descrição da collection v02) |
| L8 | Divergências documentais em tratamento | As divergências classificadas como erro da documentação na I054 são tratadas na documentação da API v03 (I042, PR #80 por integrar à data deste documento) |

## 6. Compatibilidade

| Elemento | Versão |
|---|---|
| .NET SDK | 8.0.100 ou superior da linha 8.0 (`backend/global.json`, `rollForward: latestFeature`) |
| Runtime | ASP.NET Core 8 (`net8.0`), C# 12; imagem `mcr.microsoft.com/dotnet/aspnet:8.0` no ambiente de demonstração |
| Base de dados | SQL Server 2022, edição Developer (`mcr.microsoft.com/mssql/server:2022-latest` na demonstração e nos testes da pipeline) |
| Entity Framework Core | 8.0.31; ferramenta `dotnet-ef` 8.0.31 |
| Outros pacotes | Microsoft.AspNetCore.Authentication.JwtBearer 8.0.31, System.IdentityModel.Tokens.Jwt 7.1.2, Swashbuckle.AspNetCore 6.9.0 (`backend/Directory.Packages.props`) |
| Sistemas operativos | Windows e Linux para desenvolvimento (manual, secção 1); Ubuntu 24.04 no ambiente de demonstração |
| Clientes | Qualquer cliente HTTP com JSON; autenticação por token JWT no cabeçalho `Authorization: Bearer <token>` |

## 7. Instruções relevantes

- Instalação local, configuração dos segredos, base de dados, execução, verificação e testes: `docs/instalacao/m3-manual-instalacao-configuracao-v01.md`.
- Base de dados: as migrações aplicam-se ao arrancar a API, com `BaseDados:AplicarMigracoesNoArranque` a `true`. Os dados de demonstração vêm do script `02-dados-iniciais-ficticios.sql`, só com dados fictícios. Esta é a primeira release, por isso não há atualização a partir de uma versão anterior.
- Ambiente de demonstração: https://dosecerta.spaincentral.cloudapp.azure.com, com as contas fictícias da descrição da pipeline v03 (secção 10.5).
- Verificação rápida da versão publicada: `GET https://dosecerta.spaincentral.cloudapp.azure.com/saude` responde `{"estado":"Saudavel","verificacoes":{"baseDados":"Saudavel"}}`.

## 8. Lista de verificação para o Revisor

- [ ] 1. A versão, a data e a tag prevista coincidem com o Regulamento Interno do Grupo (secção 10.5) e com a data de entrega da m3.
- [ ] 2. Cada funcionalidade e cada operação da secção 2 existe na documentação da API v02 e tem um controller na `main`; a F007 não tem operações.
- [ ] 3. Os Pull Requests citados nas secções 2 e 4 estão integrados (`gh pr list --state merged`).
- [ ] 4. As correções da secção 4 correspondem às descrições dos Pull Requests #49, #50, #51, #63, #64 e #65 e ao relatório de evidências da pipeline.
- [ ] 5. As limitações L1 e L2 confirmam-se no código (nenhum serviço cria `ProximoCompromisso`) e na descrição do PR #59.
- [ ] 6. As versões da secção 6 coincidem com `backend/global.json`, `backend/Directory.Build.props`, `backend/Directory.Packages.props` e `backend/Dockerfile`.
- [ ] 7. As ligações da secção 1 abrem e o `/saude` público responde `Saudavel`.
- [ ] 8. Nenhum segredo real está no documento.

## 9. Cobertura

| Exigência | Origem | Onde é cumprida |
|---|---|---|
| CA3: identificam a versão | Issue I051 | Secção 1 e tabela de identificação |
| CA3: identificam as funcionalidades entregues (F001 a F011, sem a F007) | Issue I051 | Secção 2 |
| CA3: identificam as alterações em relação a m2 | Issue I051 | Secção 3 |
| CA3: identificam as limitações conhecidas | Issue I051 | Secção 5 |
| CA4: US-I051-02 (funcionalidades entregues, alterações e limitações conhecidas da versão de m3) | Issue I051 | Secções 2, 3 e 5 |
| Versão | Regulamento da UC, secção 19 (04.19) | Secção 1 |
| Alterações | Regulamento da UC, secção 19 (04.19) | Secções 2 e 3 |
| Correções | Regulamento da UC, secção 19 (04.19) | Secção 4 |
| Limitações | Regulamento da UC, secção 19 (04.19) | Secção 5 |
| Compatibilidade | Regulamento da UC, secção 19 (04.19) | Secção 6 |
| Instruções relevantes | Regulamento da UC, secção 19 (04.19) | Secção 7 |

O CA1, o CA2 e a US-I051-01 são cumpridos no manual de instalação e configuração (`docs/instalacao/m3-manual-instalacao-configuracao-v01.md`, secção 15).
