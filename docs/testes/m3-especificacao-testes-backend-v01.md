# ESPECIFICAÇÃO DOS TESTES DO BACKEND

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m3 |
| Versão | v01 |
| Issue | I048 (Sprint 06) |
| Código de referência | Branch `main`, commit `7bf212c` (2026-10-30) |
| Documento complementar | Plano de Testes do Backend `m3-plano-testes-backend-v01.md` |

| Versão | Alterações |
|---|---|
| v01 | Primeira versão: condições de teste por funcionalidade, procedimentos de execução, os 520 casos de teste automatizados (631 execuções), os testes de sistema da collection Postman, a rastreabilidade aos requisitos e às operações da API e as lacunas conhecidas |

## 1. Finalidade e âmbito

Este documento especifica os testes do backend do DoseCerta definidos pelo plano de testes: as condições de teste de cada funcionalidade, os procedimentos para os executar, cada caso de teste com o resultado esperado e os requisitos que verifica, e a rastreabilidade dos requisitos e das operações da API aos casos de teste. Abrange os testes unitários (`backend/tests/DoseCerta.UnitTests`), os testes de integração (`backend/tests/DoseCerta.IntegrationTests`) e os testes de sistema da collection Postman (`docs/postman`), no código do commit `7bf212c` da `main`. As funcionalidades são F001 a F011, sem a F007, retirada do âmbito.

Os casos de teste automatizados foram levantados diretamente do código dos testes, e não de memória: cada linha do catálogo corresponde a um método de teste existente, com o nome exato que aparece nos relatórios TRX da pipeline. A secção 10 descreve como esta correspondência foi verificada.

## 2. Referências

| Referência | Utilização |
|---|---|
| Plano de Testes do Backend `m3-plano-testes-backend-v01.md` | Níveis, técnicas, ambiente, dados e critérios de saída |
| Documentação da API `m3-documentacao-api-v03.md` (Issue I042) | Resultados esperados: rotas, códigos HTTP, códigos de erro e corpos; lista das 45 operações (secção 6) e dos requisitos funcionais ativos (secção 7) |
| Especificação de Requisitos `m3-especificacao-requisitos-v02.md` | Requisitos e critérios de aceitação (CA) citados nos casos de teste |
| Matriz de Permissões `m3-matriz-permissoes-ficha-v02.md` | Regras RP-xx e decisões DP-xx citadas nos casos de teste |
| Diagrama de estados da toma e das notificações (AD-001) | Transições TR-xx citadas nos casos de teste |
| Modelo de Dados `m3-modelo-de-dados-dosecerta-v03.md` | Restrições (CK, UQ, gatilhos) e regras DM-xx citadas nos testes do modelo de dados |
| Collection Postman `docs/postman/m3-collection-postman-v02.json` | Testes de sistema (secção 7.3) |

## 3. Convenções

### 3.1 Identificadores

- `Unn` identifica uma classe de testes unitários e `Inn` uma classe de testes de integração, numeradas pela ordem das pastas (`Dominio`, `Aplicacao`, `Api` e `Infraestrutura` nos unitários; `Api`, `Dados` e `Seguranca` nos de integração) e, dentro de cada pasta, pela ordem alfabética dos ficheiros.
- `Unn.mm` e `Inn.mm` identificam o caso de teste `mm` da classe, pela ordem em que aparece no ficheiro.
- O identificador de referência para os resultados é o nome completo do método (`DoseCerta.<projeto>.<pasta>.<classe>.<método>`), que é o que aparece nos relatórios TRX. Os identificadores `Unn.mm` e `Inn.mm` são usados nesta especificação e na RTM. Numa nova versão desta especificação, as classes e os métodos acrescentados recebem os números seguintes, para não alterar os existentes.

### 3.2 Colunas do catálogo

| Coluna | Conteúdo |
|---|---|
| ID | Identificador do caso de teste |
| Método | Nome do método de teste |
| Condição testada e resultado esperado | Texto do comentário `<summary>` do método: a condição de entrada (pré-condições, dados e ação) e o resultado esperado. Os pormenores de cada caso (dados exatos, verificações) estão no próprio método |
| Casos | Número de execuções do método: 1 num `[Fact]`; o número de linhas `[InlineData]` num `[Theory]` |
| Requisitos | Requisitos verificados. São os citados no comentário do método; quando o método não cita nenhum, são os citados no comentário da classe, que se aplicam a todos os seus métodos. Os intervalos («RF-110 a RF-113») são expandidos. «-» indica um caso que não verifica um requisito RF ou RNF, mas uma restrição do modelo de dados (CK, UQ, DM-xx), a configuração da solução ou um mecanismo técnico (por exemplo, a tradução de um erro) |

As pré-condições comuns a todos os casos de uma classe estão no texto que precede a tabela da classe e no nível de teste (plano, secção 5.1): os casos unitários usam duplos de teste e um relógio fixo; os casos de integração usam a API completa contra o SQL Server, com Utilizadores e fichas criados pelo próprio caso.

### 3.3 Correções de rastreabilidade

Na confrontação dos comentários com a documentação da API, foram encontrados casos que verificam um requisito sem o citar. Esta especificação acrescenta esse requisito ao caso. A correção dos comentários no código é uma melhoria a fazer numa Issue de código e não altera o comportamento dos testes.

| Caso de teste | Requisito acrescentado | Motivo |
|---|---|---|
| `TomaTestes.ConfirmarDepoisDaToleranciaFicaComAtraso` e `TomaTestes.ConfirmarAntesDasVinteEQuatroHorasFicaComAtraso` | RF-067 | Verificam o estado `ConfirmadaComAtraso` (RF-067); o primeiro cita só o RF-066, que é o estado `Pendente` |
| `TomasApiTestes.ListarTomasDaFicha` | RF-063, RF-065 | Verifica o calendário de tomas da ficha e quem confirmou e quando (OP-32) |
| `TomasApiTestes.ConfirmarToma` | RF-064, RF-065, RF-105 | Verifica a confirmação, o autor e o momento, e a recusa da segunda confirmação (OP-33) |
| `TomasApiTestes.TomaDeAmanhaFicaForaDaJanela` | RF-064 | Verifica a janela de confirmação (OP-33) |
| `TomasApiTestes.TomaComVinteEQuatroHorasNaoEConfirmavel` | RF-068 | Verifica a passagem a `NaoConfirmada` às 24 horas (OP-33) |
| `TomasApiTestes.CalendarioDeTomas` | RF-061, RF-069 | Verifica a sincronização das 48 horas seguintes, sem fichas arquivadas (OP-34) |

## 4. Condições de teste por funcionalidade

| Funcionalidade | Condições de teste | Técnicas | Classes |
|---|---|---|---|
| F001 Autenticação e conta | Registo com e-mail único (sem distinguir maiúsculas), regras da palavra-passe, início e fim de sessão, revogação do token, sessão obrigatória, atualização dos dados e da palavra-passe, hash das palavras-passe e emissão e validação dos tokens JWT | Partições de equivalência, valores-limite da palavra-passe | U18, U19, U47, I04, I30 |
| F002 Fichas de utentes | Criação de ficha de terceiro com necessidades e documentos, consulta isolada por associação, edição, arquivo e reativação, eliminação só sem registos, documentos (formatos, tamanho, ficheiro vazio, falha do armazenamento, acesso restrito, falha ao descarregar) | Partições, valores-limite (10 MB), tabelas de decisão por papel, injeção de falhas | U10, U23, U26, U27, U42, I07, I11 |
| F003 Ficha própria | Uma só ficha própria por Utilizador (também em concorrência), proteção do titular (não é despromovido, removido nem perde o papel de Cuidador principal), aplicação das restantes regras das fichas | Tabelas de decisão, transição de estados | U08, U37, U45, I10, I29 |
| F004 Associação | Convites (duplicados, convidado já associado, ficha própria, e-mail e notificação), área de convites, aceitação e recusa, promoção e despromoção, remoção de acesso, transferência do Cuidador principal, garantia de um Cuidador, concorrência na associação, reenvios do e-mail de convite | Tabelas de decisão (RP-xx, DP-xx), transição de estados do convite, concorrência, injeção de falhas no e-mail | U03, U06, U07, U09, U11, U20, U21, U22, U25, U32, U36, U44, I03, I05, I06, I08, I09, I14, I15, I17, I18, I21, I28 |
| F005 Registo de medicação | Registo com grau de importância, horários e receita, consulta incluindo os terminados, atualização só de tratamentos em curso, término antecipado e automático, geração das tomas (com e sem data de fim, janela móvel), falha ao guardar a receita | Partições, valores-limite das datas, transição de estados do tratamento, injeção de falhas | U15, U35, I22, I26 |
| F006 Horários e confirmação de toma | Listagem das tomas com intervalo máximo de 31 dias, janela de confirmação (desde as 00:00 de Lisboa do dia previsto até às 24 horas), confirmação com e sem atraso por grau, confirmação repetida e em paralelo, passagem a `NaoConfirmada`, calendário das 48 horas seguintes, fichas arquivadas | Valores-limite das tolerâncias, transição de estados (TR-03 a TR-15), concorrência, tempo controlado (hora de verão e de inverno) | U12, U14, U34, U41, I20 |
| F008 Compromissos e agenda | Registo, edição e remoção de compromissos, antecedências válidas do aviso, recusa de confirmação de compromissos, agenda diária, semanal e mensal com tomas e compromissos das fichas ativas, nome da ficha em cada evento, limites dos períodos em Lisboa | Partições, valores-limite das antecedências e dos períodos, tempo controlado | U01, U02, U16, U17, I01, I02 |
| F009 Ocorrências e notas | Registo de ocorrências e de notas só por Cuidadores, notas invisíveis aos Familiares, autor e momento, histórico ordenado e filtrado pelo dia de Lisboa, imutabilidade (`405`), notificação de nova ocorrência aos outros, atomicidade | Tabelas de decisão por papel, injeção de falhas, tempo controlado | U31, U38, I16 |
| F010 Lista diária | Tomas e compromissos do dia de todas as fichas ativas de que o Utilizador é Cuidador, nome da ficha, confirmações de outros Cuidadores, toma confirmável, data por omissão em Lisboa, data inválida | Partições, valores-limite do dia, tempo controlado | U28, I12 |
| F011 Notificações | Tolerância de cada grau e destinatários (Cuidadores; Familiares no grau Grave aos 60 minutos), sem notificações duplicadas, notificação criada mesmo quando o e-mail falha, reenvios e e-mail não entregue, histórico de notificações com filtro de datas, suspensão em fichas arquivadas, textos em português e datas no formato de Lisboa | Valores-limite das tolerâncias, transição de estados do envio de e-mail, injeção de falhas, tempo controlado | U05, U13, U24, U29, U30, U33, U40, I13 |
| Transversal | Tradução de cada código de erro para o código HTTP e o corpo do contrato, erros de validação por campo, erros inesperados sem detalhes internos, configuração da solução, mapeamento EF Core, migrações (19 tabelas, 3 gatilhos), restrições e cascatas, unidade de trabalho atómica e conversão das violações de unicidade, retirada da F007, verificação do estado do servidor | Testes de contrato, injeção de falhas | U04, U39, U43, U46, I19, I23, I24, I25, I27 |

## 5. Procedimentos de teste

Os comandos executam-se na pasta `backend`, depois de compilar a solução em Release (`dotnet build DoseCerta.sln --configuration Release`). Os relatórios ficam em `artefactos/`, pasta ignorada pelo Git.

| N.º | Procedimento | Pré-condições | Passos | Resultado esperado |
|---|---|---|---|---|
| P-01 | Testes unitários | .NET 8 SDK | `dotnet test tests/DoseCerta.UnitTests/DoseCerta.UnitTests.csproj --no-build --configuration Release --logger "trx;LogFileName=testes-unitarios.trx" --results-directory artefactos/testes-unitarios` | 454 casos passados, 0 falhados, 0 ignorados |
| P-02 | Testes de integração | SQL Server 2022 a correr; variável `DOSECERTA_TESTES_SQLSERVER` com a connection string sem nome de base de dados (README do backend, secção 7) | `dotnet test tests/DoseCerta.IntegrationTests/DoseCerta.IntegrationTests.csproj --no-build --configuration Release --logger "trx;LogFileName=testes-integracao.trx" --results-directory artefactos/testes-integracao` | 177 casos passados, 0 falhados, 0 ignorados; a base de dados `DoseCerta_Testes_<guid>` não fica no servidor |
| P-03 | Cobertura | P-01 e P-02 executados com `--settings coverage.runsettings --collect "XPlat Code Coverage"`; ReportGenerator 5.5.11 | `reportgenerator -reports:"artefactos/testes-*/**/coverage.cobertura.xml" -targetdir:artefactos/cobertura -reporttypes:"Html;Cobertura;JsonSummary"` | Cobertura de linhas igual ou superior a 70% em `artefactos/cobertura/Summary.json` |
| P-04 | Análise estática e formatação | .NET 8 SDK | `dotnet build DoseCerta.sln --no-incremental --configuration Release -warnaserror` e `dotnet format DoseCerta.sln --verify-no-changes` | 0 erros e 0 diferenças |
| P-05 | Collection contra o backend local | Backend a correr em `https://localhost:7180` (README do backend, secção 5), com o processo periódico desligado; newman instalado; fora dos últimos minutos do dia | Na pasta `docs/postman`: `newman run m3-collection-postman-v02.json -e m3-ambiente-local-postman-v02.json --insecure` | 128 pedidos executados e 0 verificações falhadas |
| P-06 | Collection contra o URL público | `GET https://dosecerta.spaincentral.cloudapp.azure.com/saude` responde `200` | Na pasta `docs/postman`: `newman run m3-collection-postman-v02.json -e m3-ambiente-publico-postman-v01.json` | 128 pedidos executados e 0 verificações falhadas; tempos de resposta registados para a medição de desempenho (plano, secção 9) |
| P-07 | Executar um caso ou uma classe | Como em P-01 ou P-02 | Acrescentar `--filter "FullyQualifiedName~<classe>.<método>"` ao comando do nível | Só os casos filtrados são executados |

Na pipeline, os procedimentos P-01 a P-04 são executados automaticamente pelos passos QG-01 a QG-05 da `backend-ci` (descrição da pipeline, secção 4).

## 6. Resumo dos casos de teste

| Nível | Pasta | Classes | Métodos | Casos |
|---|---|---|---|---|
| Unitário | `Dominio/` (Domínio) | 15 | 108 | 146 |
| Unitário | `Aplicacao/` (Aplicação) | 20 | 188 | 214 |
| Unitário | `Api/` (API) | 6 | 20 | 50 |
| Unitário | `Infraestrutura/` (Infraestrutura) | 6 | 35 | 44 |
| **Unitário, total** | | **47** | **351** | **454** |
| Integração | `Api/` (API) | 22 | 144 | 149 |
| Integração | `Dados/` (Dados) | 7 | 23 | 24 |
| Integração | `Seguranca/` (Segurança) | 1 | 2 | 4 |
| **Integração, total** | | **30** | **169** | **177** |
| **Total** | | **77** | **520** | **631** |

Cada caso de teste é independente dos outros e pode ser executado sozinho (P-07). Nenhum caso está marcado como ignorado (`Skip`).

## 7. Casos de teste

### 7.1 Testes unitários

#### 7.1.1 Domínio

**U01 `CalendarioLisboaTestes`** (12 métodos, 14 casos): Testes unitários de CalendarioLisboa (RNF-047, RNF-029). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/CalendarioLisboaTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U01.01 | `ParaUtcNoVeraoSubtraiUmaHora` | No horário de verão, as 08:00 de Lisboa correspondem às 07:00 UTC. | 1 | RNF-029, RNF-047 |
| U01.02 | `ParaUtcNoInvernoMantemAHora` | No inverno, a hora de Lisboa coincide com UTC. | 1 | RNF-029, RNF-047 |
| U01.03 | `ParaUtcNaHoraRepetidaUsaAPrimeiraOcorrencia` | Na hora repetida da mudança para o inverno usa a primeira ocorrência (ainda em horário de verão). | 1 | RNF-029, RNF-047 |
| U01.04 | `ParaUtcNaHoraInexistenteAvancaOSalto` | Na hora inexistente da mudança para o verão avança pela duração do salto. | 1 | RNF-029, RNF-047 |
| U01.05 | `DiaLocalSegueAHoraDeLisboa` | O dia local de um instante perto da meia-noite segue a hora de Lisboa. | 1 | RNF-029, RNF-047 |
| U01.06 | `InicioDoDiaSeguinteEstaVinteQuatroHorasDepois` | O início do dia seguinte está 24 horas depois do início do dia, fora das mudanças de hora. | 1 | RNF-029, RNF-047 |
| U01.07 | `ParaLocalIncluiODesvio` | A conversão para hora local inclui o desvio de Lisboa. | 1 | RNF-029, RNF-047 |
| U01.08 | `FormatarDataHoraUsaOFormatoPortugues` | A formatação usa dd/mm/aaaa e hh:mm em hora de Lisboa. | 1 | RNF-029, RNF-047 |
| U01.09 | `DiasDaMudancaDeHoraTem23E25Horas` | RNF-047: nas mudanças de hora, o dia de Lisboa tem 23 horas (último domingo de março) ou 25 horas (último domingo de outubro). | 1 | RNF-047 |
| U01.10 | `SemanaComecaNaSegundaFeira` | RF-085: a semana vai de segunda-feira a domingo; a semana do último domingo de outubro começa ainda em hora de verão. | 3 | RF-085 |
| U01.11 | `SemanaQueAtravessaOMes` | RF-085: a semana que atravessa o mês começa na segunda-feira do mês anterior. | 1 | RF-085 |
| U01.12 | `MesEmHoraDeLisboa` | RF-086: o mês vai das 00:00 de Lisboa do dia 1 às 00:00 do dia 1 do mês seguinte, com a mudança de hora pelo meio. | 1 | RF-086 |

**U02 `CompromissoTestes`** (6 métodos, 14 casos): Testes unitários de Compromisso e da antecedência do aviso (RF-080, RF-081, RF-116, RF-117). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/CompromissoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U02.01 | `AntecedenciaValida` | RF-116: só são aceites as antecedências de 30 minutos, 1, 2, 24 e 48 horas. | 8 | RF-116 |
| U02.02 | `AvisoDevidoDesdeOInstanteDoAvisoAteSerEnviado` | RF-117: o aviso é devido a partir da data e hora menos a antecedência, e uma só vez. | 1 | RF-117 |
| U02.03 | `AvisoJaPassadoFicaDevidoDeImediato` | RF-117: registado depois do instante do aviso, o aviso fica devido de imediato. | 1 | RF-117 |
| U02.04 | `AtualizarDataOuAntecedenciaRepoeOAviso` | RF-081 e RF-117: ao alterar a data ou a antecedência, o aviso volta a ficar por enviar, com os valores novos. | 2 | RF-081, RF-117 |
| U02.05 | `AtualizarSoLocalEMotivoMantemOAviso` | Alterar só o local ou o motivo mantém o aviso já enviado. | 1 | RF-080, RF-081, RF-116, RF-117 |
| U02.06 | `AtualizarComAntecedenciaInvalidaERecusado` | A antecedência inválida também é recusada na edição, sem alterar o compromisso. | 1 | RF-080, RF-081, RF-116, RF-117 |

**U03 `ConviteTestes`** (4 métodos, 12 casos): Testes unitários da resposta a um Convite e do destinatário do convite (F004, RF-042 a RF-046). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/ConviteTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U03.01 | `AceitarDeixaOConviteAceite` | RF-045 CA1: aceitar deixa o convite aceite, com quem respondeu e quando; o papel oferecido não muda. | 1 | RF-045 |
| U03.02 | `RecusarDeixaOConviteRecusado` | RF-046 CA1: recusar deixa o convite recusado, com quem respondeu e quando. | 1 | RF-046 |
| U03.03 | `SegundaRespostaERecusada` | UC-14 E1: um convite já aceite ou recusado não pode ser respondido outra vez, e a primeira resposta mantém-se. | 4 | RF-042, RF-043, RF-044, RF-045, RF-046 |
| U03.04 | `DirigidoAComparaOEmailSemDistinguirMaiusculas` | RF-042, RF-044: o convite é dirigido ao e-mail convidado, sem distinguir maiúsculas nem espaços nas pontas, como a collation da coluna. | 6 | RF-042, RF-044 |

**U04 `EntidadesTestes`** (7 métodos, 7 casos): Testes unitários dos estados iniciais e das invariantes de criação das entidades do modelo de dados (I030). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/EntidadesTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U04.01 | `FichaDeTerceiroFicaAtivaSemTitular` | Uma ficha de terceiro fica ativa desde a criação e não tem titular (DM-19, CK_Ficha_Tipo). | 1 | - |
| U04.02 | `FichaComTitularEPropria` | Uma ficha com titular é a ficha própria desse Utilizador (CK_Ficha_Tipo). | 1 | - |
| U04.03 | `AssociacaoNovaNaoEPrincipal` | Uma associação nova não é a do Cuidador principal (valores predefinidos de EPrincipal e EProvisorio). | 1 | - |
| U04.04 | `ConviteNovoFicaPendente` | Um convite novo fica pendente, sem resposta (CK_Convite_Resposta). | 1 | - |
| U04.05 | `EnvioEmailSemOrigemERecusado` | Um e-mail tem de ter origem numa notificação ou num convite (CK_EnvioEmail_Origem). | 1 | - |
| U04.06 | `EnvioEmailNovoFicaPendente` | Um e-mail novo fica pendente, sem reenvios, com a primeira tentativa marcada para o instante da criação. | 1 | - |
| U04.07 | `RegistosNovosComecamNoEstadoInicial` | Os registos gerados começam nos estados iniciais do modelo de dados (toma pendente e tratamento em curso). | 1 | - |

**U05 `EnvioEmailTestes`** (6 métodos, 6 casos): Testes unitários do ciclo de vida de um EnvioEmail: tentativas, reenvios e não entregue (RF-118, RF-119, RF-136, RNF-018). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/EnvioEmailTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U05.01 | `EmailNovoFicaPendenteEDevidoDeImediato` | RNF-018: o e-mail novo fica pendente e devido de imediato, sem tentativas nem reenvios. | 1 | RNF-018 |
| U05.02 | `PrimeiraTentativaComSucessoDeixaEnviado` | RNF-018: a primeira tentativa bem-sucedida deixa o e-mail enviado, com o instante da primeira tentativa, sem reenvios. | 1 | RNF-018 |
| U05.03 | `TresReenviosFalhadosDeixamNaoEntregue` | RF-136 CA1 e CA2: depois da primeira falha há no máximo três reenvios, com 5 minutos de intervalo; se o terceiro reenvio também falhar, o e-mail fica não entregue e não há mais tentativas. | 1 | RF-136 |
| U05.04 | `FalhaAgendaOReenvioCincoMinutosDepois` | RF-136: uma falha agenda o reenvio para 5 minutos depois; antes disso o e-mail não está devido. | 1 | RF-136 |
| U05.05 | `ReenvioComSucessoTerminaOEnvio` | RF-118: se um reenvio for bem-sucedido, não há mais tentativas; o reenvio conta. | 1 | RF-118 |
| U05.06 | `TentativaForaDeTempoOuDepoisDeTerminarEUmDefeito` | Uma tentativa antes do momento devido, ou depois de o envio terminar, é um defeito do chamador. | 1 | RF-118, RF-119, RF-136, RNF-018 |

**U06 `FichaConvitePrincipalTestes`** (9 métodos, 10 casos): Testes unitários do convite para Cuidador principal no agregado Ficha (F002, F004, RF-025 a RF-027, RF-034; OP-19, OP-21, OP-22). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/FichaConvitePrincipalTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U06.01 | `AceitarDeixaOConvidadoComoPrincipalERetiraOCriador` | RF-026 CA1: aceitar o convite deixa o convidado como Cuidador principal, não provisório, e o criador provisório deixa de estar associado. | 1 | RF-026 |
| U06.02 | `RecusarMantemOCriadorComoPrincipalEPermiteNovoConvite` | RF-027 CA1: recusar o convite mantém o criador como Cuidador principal, que deixa de ser provisório e pode convidar outra pessoa. | 1 | RF-027 |
| U06.03 | `ConviteParaPrincipalDeixaOPrincipalAtualProvisorio` | D2a, RF-025: o convite para Cuidador principal feito na OP-19 deixa provisório o Cuidador principal atual. | 1 | RF-025 |
| U06.04 | `SegundoConviteParaPrincipalERecusado` | D2b: com um convite para Cuidador principal pendente (principal provisório), um segundo é recusado com 409. | 1 | RF-025, RF-026, RF-027, RF-034 |
| U06.05 | `FichaPropriaNaoTemConviteParaPrincipal` | RF-034, UC-13 E6: na ficha própria não há convite para Cuidador principal. | 1 | RF-034 |
| U06.06 | `ConvidadoJaAssociadoNaoAceita` | D4: se o convidado já estiver associado à ficha, a aceitação é recusada com 409 e nada muda. | 2 | RF-025, RF-026, RF-027, RF-034 |
| U06.07 | `AceitarDepoisDeUmaTransferenciaRetiraOPrincipalProvisorio` | D3: depois de uma transferência com o convite pendente, a aceitação retira o novo principal provisório e mantém o criador como Cuidador. | 1 | RF-025, RF-026, RF-027, RF-034 |
| U06.08 | `ResponderSemPrincipalProvisorioEUmDefeito` | Sem Cuidador principal provisório não existe convite para Cuidador principal pendente: responder é um defeito do chamador. | 1 | RF-025, RF-026, RF-027, RF-034 |
| U06.09 | `SoAPessoaNaoAssociadaPodeSerAssociada` | OP-19, OP-21: Ficha.ExigirQueNaoEstaAssociado recusa uma pessoa associada com qualquer papel e aceita uma não associada. | 1 | RF-025, RF-026, RF-027, RF-034 |

**U07 `FichaPapeisTestes`** (8 métodos, 11 casos): Testes unitários da promoção e da despromoção no agregado Ficha (F004, RF-049, RF-130, RF-133, RF-134; OP-23, OP-24). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/FichaPapeisTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U07.01 | `PromoverFamiliarDaPermissoesDeCuidador` | RF-049 CA1: o Familiar promovido passa a Cuidador, com permissão de escrita, sem ser Cuidador principal. | 1 | RF-049 |
| U07.02 | `DespromoverCuidadorDeixaSoAsPermissoesDeFamiliar` | RF-130 CA1: um Cuidador que não é o principal nem o titular passa a Familiar e perde a permissão de escrita. | 1 | RF-130 |
| U07.03 | `PessoaNaoAssociadaNaoEEncontrada` | UC-15 E5: a pessoa visada tem de estar associada à ficha, para qualquer papel pretendido. | 2 | RF-049, RF-130, RF-133, RF-134 |
| U07.04 | `UnicoCuidadorNaoEDespromovido` | RF-134 CA1: numa ficha com um único Cuidador e dois Familiares, despromover esse Cuidador é recusado e nada muda. | 1 | RF-134 |
| U07.05 | `PrincipalNaoEDespromovidoSemSubstituto` | RF-133: com outro Cuidador na ficha, o Cuidador principal só é despromovido depois de transferir o papel (OP-26), mesmo a pedido próprio. | 1 | RF-133 |
| U07.06 | `TitularNaoEDespromovido` | RF-033: o titular da ficha própria não é despromovido; a proteção vem antes das outras regras, com ou sem outro Cuidador. | 2 | RF-033 |
| U07.07 | `OutroCuidadorDaFichaPropriaEDespromovido` | RF-032, RF-130: na ficha própria, outro Cuidador pode ser despromovido como nas restantes fichas. | 1 | RF-032, RF-130 |
| U07.08 | `PedidoComOPapelAtualNaoAlteraNada` | D7: um pedido com o papel atual não altera nada e não é recusado, incluindo sobre o Cuidador principal e o titular. | 2 | RF-049, RF-130, RF-133, RF-134 |

**U08 `FichaPropriaTestes`** (5 métodos, 5 casos): Testes unitários da proteção do titular da ficha própria no agregado Ficha (F003, RF-033, RF-034). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/FichaPropriaTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U08.01 | `SoOTitularDaFichaPropriaETitular` | Só o titular de uma ficha própria é titular; numa ficha de terceiro ninguém o é. | 1 | RF-033, RF-034 |
| U08.02 | `TitularNaoPodeSerDespromovidoNemRemovido` | RF-033 CA1 e CA2: despromover ou remover o titular é recusado, seja quem for que o peça (UC-15 E1). | 1 | RF-033 |
| U08.03 | `OutroCuidadorDaFichaPropriaNaoEProtegido` | RF-033: a proteção é só do titular; outro Cuidador da ficha própria pode ser despromovido ou removido. | 1 | RF-033 |
| U08.04 | `PrincipalDeFichaDeTerceiroNaoEProtegidoPorEstaRegra` | Numa ficha de terceiro o Cuidador principal não é protegido por esta regra (as regras dele são as do RF-133, da F004). | 1 | RF-133 |
| U08.05 | `PrincipalDaFichaPropriaNaoSeTransfere` | RF-034 CA1: na ficha própria o papel de Cuidador principal não se transfere, nem a pedido do titular (UC-16 E1). | 1 | RF-034 |

**U09 `FichaRemocaoAcessoTestes`** (7 métodos, 9 casos): Testes unitários da remoção de acesso no agregado Ficha (F004, RF-131, RF-133, RF-134; OP-25). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/FichaRemocaoAcessoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U09.01 | `FamiliarRemovidoDeixaDeEstarAssociado` | RF-131 CA1: o Familiar removido deixa de estar associado à ficha; as outras pessoas mantêm-se. | 1 | RF-131 |
| U09.02 | `CuidadorQueNaoEOPrincipalERemovido` | RF-131: um Cuidador que não é o principal pode ser removido, também por sua iniciativa (Matriz de Permissões, secção 6). | 1 | RF-131 |
| U09.03 | `PessoaNaoAssociadaNaoEEncontrada` | UC-15 E5: a pessoa a remover tem de estar associada à ficha. | 1 | RF-131, RF-133, RF-134 |
| U09.04 | `UnicoCuidadorNaoERemovido` | RF-133 CA1 e RF-134 CA1: o único Cuidador, que é o principal, não é removido, mesmo a pedido próprio, e é indicado que deve primeiro promover um Familiar ou convidar um novo Cuidador. | 2 | RF-133, RF-134 |
| U09.05 | `PrincipalNaoERemovidoSemSubstituto` | RF-133, R-06: com outro Cuidador na ficha, o Cuidador principal só é removido depois de transferir o papel (OP-26). | 1 | RF-133 |
| U09.06 | `TitularNaoERemovido` | RF-033 CA2: o titular da ficha própria não é removido; a proteção vem antes das outras regras, com ou sem outro Cuidador. | 2 | RF-033 |
| U09.07 | `OutraPessoaDaFichaPropriaERemovida` | RF-032, RF-131: na ficha própria, as outras pessoas são removidas como nas restantes fichas. | 1 | RF-032, RF-131 |

**U10 `FichaTestes`** (9 métodos, 11 casos): Testes unitários das regras do agregado Ficha (F002, F003). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/FichaTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U10.01 | `AssociarCuidadorPrincipalMarcaOPrincipal` | RF-024: o criador fica associado como Cuidador principal, não provisório. | 1 | RF-024 |
| U10.02 | `AssociarCuidadorPrincipalProvisorio` | RF-025: com convite para Cuidador principal, o criador fica Cuidador principal provisório. | 1 | RF-025 |
| U10.03 | `NaoAceitaSegundoPrincipalNemAssociacaoRepetida` | RF-132: a ficha nunca tem dois Cuidadores principais e um Utilizador só tem uma associação. | 1 | RF-132 |
| U10.04 | `FichaPropriaNaoTemPrincipalProvisorio` | RF-034: na ficha própria o titular nunca é Cuidador principal provisório. | 1 | RF-034 |
| U10.05 | `FamiliarAssociadoNaoPodeEscrever` | RF-048: o Familiar está associado mas não pode escrever. | 1 | RF-048 |
| U10.06 | `ArquivarEReativarAtualizamOEstado` | RF-120 e RF-121: arquivar e reativar mudam o estado e o instante a partir do qual a ficha está ativa (DM-19). | 1 | RF-120, RF-121 |
| U10.07 | `ArquivoEReativacaoRepetidosSaoRecusados` | OP-12 e OP-13: arquivar uma ficha arquivada e reativar uma ficha ativa são recusados. | 1 | - |
| U10.08 | `AtualizarDadosEDefinirNecessidades` | RF-021 e RF-029: os dados e as necessidades são substituídos, sem valores repetidos. | 1 | RF-021, RF-029 |
| U10.09 | `PodeSerEliminadaSoSemRegistos` | RF-123: a ficha só pode ser eliminada sem medicação nem histórico. | 3 | RF-123 |

**U11 `FichaTransferenciaPrincipalTestes`** (7 métodos, 10 casos): Testes unitários da transferência do papel de Cuidador principal no agregado Ficha (F004, RF-034, RF-132, RF-133; OP-26). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/FichaTransferenciaPrincipalTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U11.01 | `TransferenciaDeixaUmUnicoPrincipal` | RF-132 CA1: numa ficha de terceiro com dois Cuidadores, o segundo passa a Cuidador principal, o anterior continua Cuidador e a ficha tem um único Cuidador principal. | 1 | RF-132 |
| U11.02 | `GravacaoAMeioAconteceSemNenhumPrincipalMarcado` | `UQ_Associacao_Principal`: a gravação a meio é chamada uma só vez, quando o anterior já não tem a marca e o novo ainda não a tem, para que nenhuma escrita deixe dois Cuidadores principais na base de dados. | 1 | RF-034, RF-132, RF-133 |
| U11.03 | `NaFichaPropriaOPapelNaoSeTransfere` | RF-034, UC-16 E1: na ficha própria o papel não se transfere, nem para outro Cuidador nem a pedido do titular; nada é gravado. | 2 | RF-034 |
| U11.04 | `DestinatarioTemDeSerCuidador` | UC-16 E2: o destinatário tem de ser Cuidador da ficha; um Familiar ou uma pessoa não associada é recusado e nada muda. | 2 | RF-034, RF-132, RF-133 |
| U11.05 | `TransferirParaOPrincipalAtualNaoAlteraNada` | D7: transferir o papel para quem já é Cuidador principal não altera nada nem grava. | 1 | RF-034, RF-132, RF-133 |
| U11.06 | `DepoisDaTransferenciaOAnteriorPodeSair` | RF-133 CA2, R-02: depois da transferência, o anterior Cuidador principal pode ser despromovido ou removido. | 2 | RF-133 |
| U11.07 | `TransferenciaComPrincipalProvisorioPassaAMarca` | D3: com um convite para Cuidador principal pendente, a transferência leva a marca de provisório para o novo Cuidador principal, para que a aceitação do convite continue a substituir o principal provisório. | 1 | RF-034, RF-132, RF-133 |

**U12 `PoliticaToleranciaTestes`** (3 métodos, 5 casos): Testes unitários de PoliticaTolerancia (RF-065 a RF-068). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/PoliticaToleranciaTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U12.01 | `ToleranciaDeCadaGrau` | RF-066: as tolerâncias são exatamente Leve 2 h, Moderado 1 h e Grave 30 min. | 3 | RF-066 |
| U12.02 | `GrauDesconhecidoERecusado` | Um grau fora do enumerado é recusado. | 1 | RF-065, RF-066, RF-067, RF-068 |
| U12.03 | `LimitesDeTempoDaToma` | RF-068: a toma passa a não confirmada 24 horas depois da hora prevista; os Familiares de uma toma Grave são avisados aos 60 minutos. | 1 | RF-068 |

**U13 `TomaAtrasoTestes`** (7 métodos, 10 casos): Testes unitários das regras de tolerância das notificações de atraso em Toma (RF-110 a RF-113; TR-05 e TR-07 do diagrama de estados da toma). A toma está prevista para 7 de outubro de 2026, 10:00 de Lisboa (09:00 UTC). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/TomaAtrasoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U13.01 | `CuidadoresSoDepoisDaToleranciaExataDoGrau` | RF-110 a RF-112: os Cuidadores só são avisados estritamente depois do fim da tolerância exata do grau (Leve 2 h, Moderado 1 h, Grave 30 min); no último instante da tolerância ainda não. | 3 | RF-110, RF-111, RF-112 |
| U13.02 | `FamiliaresDoGraveSoDepoisDeSessentaMinutos` | RF-113: no grau Grave os Familiares são avisados estritamente depois dos 60 minutos, ou seja, 30 minutos depois do fim da tolerância dos Cuidadores. | 1 | RF-113 |
| U13.03 | `FamiliaresNuncaNosGrausLeveEModerado` | RF-110 CA3 e RF-111 CA3: nos graus Leve e Moderado os Familiares nunca são avisados, a qualquer hora. | 2 | RF-110, RF-111 |
| U13.04 | `MarcarCuidadoresMantemPendenteENaoRepete` | TR-05 e RNF-043: marcar o aviso aos Cuidadores não muda o estado da toma (continua `Pendente`, RF-066) e o aviso deixa de ser devido; marcar antes do tempo não faz nada. | 1 | RF-066, RNF-043 |
| U13.05 | `MarcarFamiliaresMantemPendenteENaoRepete` | TR-07: marcar o aviso aos Familiares de uma toma Grave mantém-na `Pendente` e não se repete. | 1 | RF-110, RF-111, RF-112, RF-113 |
| U13.06 | `TomaConfirmadaNaoGeraAvisos` | RF-110 CA2 e RF-113 CA2: uma toma confirmada, dentro ou fora da tolerância, já não gera avisos. | 1 | RF-110, RF-113 |
| U13.07 | `TomaNaoConfirmadaNaoGeraAvisos` | TR-06: uma toma já não confirmada (24 horas) não gera avisos. | 1 | RF-110, RF-111, RF-112, RF-113 |

**U14 `TomaTestes`** (10 métodos, 14 casos): Testes unitários do comportamento de Toma, segundo o diagrama de estados da toma (TR-03, TR-04, TR-06, TR-13 a TR-15). Usa uma toma em hora de verão (7 de outubro, 08:00 de Lisboa = 07:00 UTC) e outra em hora de inverno (10 de novembro, 08:00 = 08:00 UTC). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/TomaTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U14.01 | `ConfirmarDentroDaToleranciaFicaConfirmada` | TR-03 e RF-065: dentro da tolerância, incluindo o instante exato do fim, fica `Confirmada` com o autor e o instante. | 3 | RF-065 |
| U14.02 | `ConfirmarAntesDaHoraNoMesmoDiaFicaConfirmada` | TR-03: a janela abre às 00:00 de Lisboa do dia da toma, que na hora de verão é 23:00 UTC do dia anterior. | 1 | - |
| U14.03 | `ConfirmarDepoisDaToleranciaFicaComAtraso` | TR-04 e RF-066: um segundo depois do fim da tolerância fica `ConfirmadaComAtraso`, em cada grau. | 3 | RF-066, RF-067 |
| U14.04 | `ConfirmarAntesDasVinteEQuatroHorasFicaComAtraso` | TR-04: pouco antes das 24 horas ainda se confirma, com atraso. | 1 | RF-067 |
| U14.05 | `ConfirmarNoDiaAnteriorFicaForaDaJanela` | TR-13: às 23:59 do dia anterior (Lisboa) recusa com `TOMA_FORA_DA_JANELA` e a toma fica pendente, no verão e no inverno. | 1 | - |
| U14.06 | `MarcarNaoConfirmadaSoAPartirDasVinteEQuatroHoras` | TR-06: Toma.MarcarNaoConfirmada só muda o estado a partir das 24 horas, inclusive. | 1 | - |
| U14.07 | `MarcarNaoConfirmadaNaoAlteraUmaTomaConfirmada` | TR-06: uma toma já confirmada não passa a não confirmada. | 1 | - |
| U14.08 | `ConfirmarUmaTomaNaoConfirmadaERecusado` | TR-14: uma toma já `NaoConfirmada` não pode ser confirmada. | 1 | - |
| U14.09 | `ConfirmarUmaTomaPendenteComVinteEQuatroHorasERecusado` | TR-06 e TR-14: uma toma ainda pendente com 24 horas passa a `NaoConfirmada` e a confirmação é recusada. | 1 | - |
| U14.10 | `SegundaConfirmacaoMantemAPrimeira` | TR-15: uma segunda confirmação é recusada e mantém a primeira. | 1 | - |

**U15 `TratamentoTestes`** (8 métodos, 8 casos): Testes unitários de Tratamento (RF-050, RF-055, RF-056, RF-060). Ficheiro: `backend/tests/DoseCerta.UnitTests/Dominio/TratamentoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U15.01 | `GeraUmaTomaPorHorarioEPorDia` | RF-060 CA1: com as horas 08:00 e 20:00, de 02 a 04 de novembro, geram-se 6 tomas, que copiam o medicamento, a dose e o grau. | 1 | RF-060 |
| U15.02 | `RespeitaOIntervaloPedido` | O intervalo pedido limita a geração: o início é inclusive e o fim é exclusive. | 1 | RF-050, RF-055, RF-056, RF-060 |
| U15.03 | `SegueAHoraDeLisboaENaoDuplicaInstantes` | RNF-047: no verão a hora de Lisboa é UTC+1; na mudança de hora, duas horas que dão o mesmo instante geram uma só toma. | 1 | RNF-047 |
| U15.04 | `TerminadoNaoGeraNemMuda` | RF-056: um tratamento terminado não gera tomas, não pode ser terminado de novo nem alterado (RF-055). | 1 | RF-055, RF-056 |
| U15.05 | `AtualizarSubstituiOsHorariosSemRecriarOsQueContinuam` | RF-055: a atualização mantém os horários que continuam, acrescenta os novos e retira os que saem. | 1 | RF-055 |
| U15.06 | `RecusaDadosIncoerentes` | O domínio recusa um tratamento sem horas ou com a data de fim anterior à de início (RF-050 CA4). | 1 | RF-050 |
| U15.07 | `DataFimUltrapassadaSoDepoisDoUltimoDia` | A data de fim fica ultrapassada no dia seguinte, só num tratamento em curso. | 1 | RF-050, RF-055, RF-056, RF-060 |
| U15.08 | `DocumentoEReceitaSoNaCategoriaReceitaMedica` | RF-052: só os documentos da categoria receita médica são receitas. | 1 | RF-052 |

#### 7.1.2 Aplicação

**U16 `AgendaServiceTestes`** (8 métodos, 10 casos): Testes unitários de AgendaService (F008, OP-45; RF-083 a RF-089, RNF-047). A F007 foi retirada: a agenda só tem tomas e compromissos. Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Agenda/AgendaServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U16.01 | `VistaEDataSaoObrigatorias` | A vista e a data são obrigatórias; a vista só aceita dia, semana ou mes. | 1 | RF-083, RF-084, RF-085, RF-086, RF-087, RF-088, RF-089, RNF-047 |
| U16.02 | `LerVista` | A vista lê dia, semana e mes, sem distinguir maiúsculas. | 3 | RF-083, RF-084, RF-085, RF-086, RF-087, RF-088, RF-089, RNF-047 |
| U16.03 | `SemFichasAAgendaEVazia` | RF-083: sem fichas associadas, a agenda é vazia. | 1 | RF-083 |
| U16.04 | `VistaDiariaJuntaTomasECompromissosPorOrdem` | RF-083, RF-084 e RF-089: a vista diária junta tomas e compromissos do dia de Lisboa, por ordem, com o nome da ficha. | 1 | RF-083, RF-084, RF-089 |
| U16.05 | `ConfirmavelSoEmTomasPendentesDoCuidador` | RF-087 e RF-048: o Familiar vê os eventos, mas nada é confirmável; uma toma já confirmada também não. | 1 | RF-048, RF-087 |
| U16.06 | `SoFichasAtivasDoUtilizador` | RF-083 e RNF-005: só as fichas ativas do Utilizador; as arquivadas e as de outras pessoas não aparecem. | 1 | RF-083, RNF-005 |
| U16.07 | `VistaSemanalRespeitaOsLimitesDeLisboa` | RF-085 e RNF-047: a semana do último domingo de outubro vai de segunda, 19, às 00:00 de domingo, 26, em hora de Lisboa. | 1 | RF-085, RNF-047 |
| U16.08 | `VistaMensalTemOMesInteiro` | RF-086 e RF-083: a vista mensal mostra os compromissos de todo o mês, mesmo para lá da janela das tomas. | 1 | RF-083, RF-086 |

**U17 `CompromissoServiceTestes`** (10 métodos, 12 casos): Testes unitários de CompromissoService (F008, OP-41 a OP-44; RF-080 a RF-082, RF-116, RF-117). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Compromissos/CompromissoServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U17.01 | `RegistarComAntecedenciaPorOmissao` | OP-41 e RF-080: o Cuidador regista; sem antecedência fica 24 horas e o instante fica guardado em UTC, ao segundo. | 1 | RF-080 |
| U17.02 | `RegistarSemCamposObrigatoriosIndicaOsCampos` | RF-080 e RNF-028: data e hora, local e motivo são obrigatórios; a resposta indica cada campo. | 1 | RF-080, RNF-028 |
| U17.03 | `RegistarComAntecedenciaInvalidaERecusado` | RF-116: uma antecedência fora dos valores permitidos é recusada, com o campo em erro. | 3 | RF-116 |
| U17.04 | `RegistarComTextoDemasiadoLongoERecusado` | O local e o motivo respeitam o comprimento máximo do modelo de dados. | 1 | RF-080, RF-081, RF-082, RF-116, RF-117 |
| U17.05 | `RegistarSoPorCuidadorDeFichaAtiva` | RP-11, RP-13 e RNF-005: o Familiar recebe 403, quem não está associado 404 e a ficha arquivada 409. | 1 | RNF-005 |
| U17.06 | `ListarComIntervaloEmDiasDeLisboa` | OP-42: o Familiar lista, também numa ficha arquivada; o intervalo usa os dias de Lisboa, inclusive. | 1 | RF-080, RF-081, RF-082, RF-116, RF-117 |
| U17.07 | `ListarValidaOIntervaloEAAssociacao` | OP-42: `ate` anterior a `de` é recusado; quem não está associado recebe 404. | 1 | RF-080, RF-081, RF-082, RF-116, RF-117 |
| U17.08 | `EditarAlteraOsDadosERepoeOAviso` | OP-43 e RF-117: a edição altera os dados e repõe o aviso para ser calculado com a data nova. | 1 | RF-117 |
| U17.09 | `EditarERemoverRecusamSemAcesso` | OP-43 e OP-44: compromisso inexistente ou de ficha não associada dá 404 `RECURSO_NAO_ENCONTRADO`, o Familiar 403 e a ficha arquivada 409, sem alterar nem remover o compromisso. | 1 | RF-080, RF-081, RF-082, RF-116, RF-117 |
| U17.10 | `RemoverApagaOCompromisso` | OP-44 e RF-082: o Cuidador remove o compromisso. | 1 | RF-082 |

**U18 `AuthServiceTestes`** (9 métodos, 9 casos): Testes unitários das regras de negócio de AuthService (F001). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Contas/AuthServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U18.01 | `RegistarComDadosValidosCriaAConta` | RF-010 CA1: o registo com dados válidos cria a conta, com a palavra-passe só em hash (RNF-001). | 1 | RF-010, RNF-001 |
| U18.02 | `RegistarComDadosInvalidosIndicaOsCampos` | RF-010 CA2 e RNF-028: campos em falta ou inválidos são todos indicados e a conta não é criada. | 1 | RF-010, RNF-028 |
| U18.03 | `RegistarComEmailJaRegistadoERecusado` | RF-011: um e-mail já registado, mesmo com maiúsculas diferentes, é recusado. | 1 | RF-011 |
| U18.04 | `RegistarIndicaOsConvitesPendentes` | RF-044: o registo indica os convites pendentes dirigidos ao e-mail da conta nova. | 1 | RF-044 |
| U18.05 | `AutenticarComCredenciaisCorretasEmiteToken` | RF-012 CA1: credenciais corretas emitem um token válido por 8 horas. | 1 | RF-012 |
| U18.06 | `AutenticarComCredenciaisErradasNaoIndicaQualEstaErrado` | RF-012 CA2: e-mail inexistente e palavra-passe errada dão o mesmo erro e a mesma mensagem. | 1 | RF-012 |
| U18.07 | `AutenticarSemCamposIndicaOsCampos` | RNF-028: o início de sessão sem campos indica os campos em falta. | 1 | RNF-028 |
| U18.08 | `TerminarSessaoRevogaACredencial` | RF-016: terminar a sessão regista a credencial, que deixa de ser aceite. | 1 | RF-016 |
| U18.09 | `SessaoRevogadaIndicaQuandoExpirou` | As credenciais terminadas expiradas podem ser eliminadas. | 1 | - |

**U19 `ContaServiceTestes`** (10 métodos, 10 casos): Testes unitários das regras de negócio de ContaService (F001). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Contas/ContaServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U19.01 | `ObterDevolveAConta` | RF-013: a consulta devolve a conta do Utilizador autenticado. | 1 | RF-013 |
| U19.02 | `ObterContaInexistenteDaNaoAutenticado` | Uma conta que deixou de existir é tratada como sessão inválida. | 1 | - |
| U19.03 | `AtualizarGuardaNomeEEmail` | RF-014 CA1: o nome e o e-mail válidos ficam guardados. | 1 | RF-014 |
| U19.04 | `AtualizarMantendoOProprioEmailEPermitido` | O Utilizador pode manter o seu próprio e-mail ao alterar o nome. | 1 | - |
| U19.05 | `AtualizarComEmailDeOutraContaERecusado` | RF-014 CA2: um e-mail de outra conta é recusado e o e-mail anterior mantém-se. | 1 | RF-014 |
| U19.06 | `AtualizarComDadosInvalidosIndicaOsCampos` | RNF-028: dados inválidos na atualização indicam os campos. | 1 | RNF-028 |
| U19.07 | `AlterarPalavraPasseSubstituiOHash` | RF-015 CA1: com a palavra-passe atual correta, a nova passa a ser aceite e a anterior deixa de o ser. | 1 | RF-015 |
| U19.08 | `AlterarPalavraPasseInvalidaERecusada` | RF-015 CA2: uma nova palavra-passe que não cumpre o RNF-003 é recusada. | 1 | RF-015, RNF-003 |
| U19.09 | `AlterarComPalavraPasseAtualErradaERecusado` | RF-015 CA3: uma palavra-passe atual incorreta é recusada e a anterior mantém-se. | 1 | RF-015 |
| U19.10 | `AlterarSemPalavraPasseAtualIndicaOCampo` | RNF-028: a palavra-passe atual em falta é indicada. | 1 | RNF-028 |

**U20 `ConviteServiceRespostaTestes`** (8 métodos, 11 casos): Testes unitários da aceitação e da recusa de convites no ConviteService (F004; OP-21, OP-22; RF-026, RF-027, RF-045, RF-046). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Convites/ConviteServiceRespostaTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U20.01 | `AceitarAssociaComOPapelOferecido` | RF-045 CA1: aceitar um convite para Familiar associa o convidado com esse papel e o convite deixa de estar pendente. O convite é para o e-mail com maiúsculas diferentes, como a collation da coluna permite. | 2 | RF-045 |
| U20.02 | `RecusarNaoAssocia` | RF-046 CA1: recusar não associa o convidado e o convite deixa de estar pendente. | 1 | RF-046 |
| U20.03 | `ConviteInexistenteOuDeOutroEmailNaoEEncontrado` | UC-14 E2: um convite que não existe, ou que é dirigido a outro e-mail, dá a mesma resposta 404, ao aceitar e ao recusar. | 2 | RF-026, RF-027, RF-045, RF-046 |
| U20.04 | `ConviteJaRespondidoDa409` | UC-14 E1: um convite já respondido não é respondido outra vez (409), seja qual for a segunda resposta. | 2 | RF-026, RF-027, RF-045, RF-046 |
| U20.05 | `ConvidadoJaAssociadoNaoAceita` | D4: se o convidado já estiver associado à ficha (por exemplo, por ter mudado o e-mail da conta), aceitar dá 409 `PESSOA_JA_ASSOCIADA` e o papel não muda. | 1 | RF-026, RF-027, RF-045, RF-046 |
| U20.06 | `FichaArquivadaAceitaNaMesma` | UC-14 A5, RP-13: numa ficha arquivada depois do convite, o convidado aceita na mesma. | 1 | RF-026, RF-027, RF-045, RF-046 |
| U20.07 | `AceitarConviteParaPrincipalTransfereOPapel` | RF-026 CA1: aceitar o convite para Cuidador principal deixa o convidado como Cuidador principal e retira o criador provisório. | 1 | RF-026 |
| U20.08 | `RecusarConviteParaPrincipalMantemOCriador` | RF-027 CA1: recusar o convite para Cuidador principal mantém o criador como Cuidador principal, já não provisório. | 1 | RF-027 |

**U21 `ConviteServiceTestes`** (13 métodos, 18 casos): Testes unitários da criação de convites no ConviteService (F004; OP-19; RF-025, RF-040, RF-043). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Convites/ConviteServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U21.01 | `ConviteFicaPendenteSemAssociar` | RF-040 CA1: o Cuidador convida um e-mail como Familiar; o convite fica pendente, com o papel e quem convidou, e não associa ninguém. | 1 | RF-040 |
| U21.02 | `DadosInvalidosNaoCriamConvite` | RF-040 CA2, RNF-028: sem papel, com e-mail inválido ou com `paraPrincipal=true` e papel Familiar, o convite não é criado. | 5 | RF-040, RNF-028 |
| U21.03 | `ConviteParaEmailComContaCriaNotificacaoEEmail` | RF-041 CA1, D1: o convite para um e-mail com conta cria a notificação `ConviteRecebido` para essa conta e o e-mail de convite pendente, ligado ao convite e à notificação. | 1 | RF-041 |
| U21.04 | `ConviteParaEmailSemContaSoCriaOEmail` | RF-041 CA2: o convite para um e-mail sem conta só cria o e-mail de convite, ligado ao convite e sem notificação. O convite para Cuidador principal da criação da ficha (OP-09) segue a mesma regra. | 1 | RF-041 |
| U21.05 | `ConvitePendenteDuplicadoERecusado` | RF-043 CA1: com um convite pendente para o mesmo e-mail e a mesma ficha, sem distinguir maiúsculas, o novo convite é recusado. | 1 | RF-043 |
| U21.06 | `ConvitePendenteNoutraFichaNaoImpede` | RF-043 CA3: um convite pendente noutra ficha não impede o convite para esta. | 1 | RF-043 |
| U21.07 | `PessoaJaAssociadaNaoEConvidada` | OP-19: o e-mail de uma pessoa já associada à ficha, incluindo o de quem convida, é recusado com 409 `PESSOA_JA_ASSOCIADA`. | 2 | RF-025, RF-040, RF-043 |
| U21.08 | `ConviteParaPrincipalDeixaOPrincipalProvisorioEImpedeOutro` | RF-025, D2a: o convite para Cuidador principal deixa o Cuidador principal atual provisório; D2b: um segundo convite para Cuidador principal, para outro e-mail, é recusado com 409 `CONVITE_PENDENTE_EXISTE`. | 1 | RF-025 |
| U21.09 | `ConviteParaPrincipalDaCriacaoDaFichaFicaPendente` | OP-09, RF-025: o convite para Cuidador principal da criação da ficha é registado pendente, com o papel Cuidador e a marca de Cuidador principal, sem alterar as associações (o criador já foi associado como provisório pela ficha). | 1 | RF-025 |
| U21.10 | `NaFichaPropriaSoOConviteParaPrincipalERecusado` | RF-034, UC-13 E6: na ficha própria o convite para Cuidador principal é recusado com 403; os outros convites são aceites (RF-032). | 1 | RF-032, RF-034 |
| U21.11 | `FamiliarEFichaArquivadaNaoConvidam` | RF-048 CA2, RP-13: o Familiar não convida (403) e, numa ficha arquivada, o convite é recusado (409). | 1 | RF-048 |
| U21.12 | `ListarRecebidosDevolveSoOsPendentesDoSeuEmail` | RF-042 CA1, RF-044: o Utilizador vê só os convites pendentes dirigidos ao seu e-mail, sem distinguir maiúsculas, com a ficha e quem convidou, do mais recente para o mais antigo; os convites respondidos e os de outros e-mails não aparecem. | 1 | RF-042, RF-044 |
| U21.13 | `ListarRecebidosSemContaDa401` | Se a conta do token já não existir, a listagem é recusada com 401, como na consulta da conta (OP-04). | 1 | RF-025, RF-040, RF-043 |

**U22 `TextosConviteTestes`** (2 métodos, 4 casos): Testes unitários dos textos do convite (RF-041, RNF-020). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Convites/TextosConviteTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U22.01 | `NotificacaoIndicaQuemAFichaEOPapel` | A notificação indica quem convidou, a ficha e o papel oferecido, incluindo o de Cuidador principal. | 3 | RF-041, RNF-020 |
| U22.02 | `TextosCabemNasColunas` | Com o nome da conta (120) e o nome da ficha (150) no máximo, a mensagem cabe nos 400 caracteres de `Notificacao.Mensagem` e o assunto cabe nos 150 de `EnvioEmail.Assunto`. | 1 | RF-041, RNF-020 |

**U23 `DocumentoServiceTestes`** (13 métodos, 17 casos): Testes unitários de DocumentoService (RF-022, RF-023, RF-124, RNF-007, RNF-008). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Documentos/DocumentoServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U23.01 | `ValidarDetetaOFormatoPeloConteudo` | O formato é detetado pelo conteúdo do ficheiro, não pela extensão do nome. | 3 | RF-022, RF-023, RF-124, RNF-007, RNF-008 |
| U23.02 | `CategoriaInvalidaERecusada` | RF-022 CA2: uma categoria diferente das três previstas, ou em falta, é recusada com 400 e o campo indicado. | 3 | RF-022 |
| U23.03 | `FicheiroEmFaltaOuVazioERecusado` | Um ficheiro em falta ou vazio é recusado com 400. | 1 | RF-022, RF-023, RF-124, RNF-007, RNF-008 |
| U23.04 | `TamanhoEFormatoSaoVerificados` | RNF-008: mais de 10 MB dá 413 e um formato diferente de PDF, JPG ou PNG dá 415. | 1 | RNF-008 |
| U23.05 | `NomeOriginalENormalizado` | O nome original perde o caminho e é limitado a 255 caracteres; sem nome, recebe um nome genérico. | 1 | RF-022, RF-023, RF-124, RNF-007, RNF-008 |
| U23.06 | `CarregarGuardaFicheiroEMetadados` | OP-15: o Cuidador carrega um documento; o ficheiro fica guardado e os metadados registados (RF-022 CA1). | 1 | RF-022 |
| U23.07 | `FalhaAoGuardarNaoDeixaReferencias` | RF-023 CA3: se o ficheiro não puder ser guardado, nada é registado e a resposta é 503. | 1 | RF-023 |
| U23.08 | `FalhaDaTransacaoApagaOFicheiro` | RNF-046: se a transação falhar depois de guardar o ficheiro, o ficheiro é apagado. | 1 | RNF-046 |
| U23.09 | `FamiliarEFichaArquivadaSaoRecusados` | RF-048 e RP-13: o Familiar não carrega documentos e uma ficha arquivada não os aceita. | 1 | RF-048 |
| U23.10 | `FamiliarListaEDescarrega` | OP-16 e OP-17: o Familiar lista e descarrega os documentos da ficha (RF-028). | 1 | RF-028 |
| U23.11 | `DocumentoDeOutraFichaDa404` | RNF-007: um documento inexistente ou de uma ficha a que o Utilizador não está associado dá a mesma resposta 404. | 1 | RNF-007 |
| U23.12 | `FicheiroIlegivelDa503` | RF-124: se o ficheiro guardado não puder ser obtido, a resposta é 503 e os metadados mantêm-se. | 1 | RF-124 |
| U23.13 | `EliminarFicheirosRemoveTodos` | RF-123: os ficheiros de uma ficha eliminada são removidos do armazenamento. | 1 | RF-123 |

**U24 `EnvioEmailNotificacaoTestes`** (5 métodos, 5 casos): Testes unitários do envio e dos reenvios dos e-mails de notificação no EnvioEmailService (RF-118, RF-119, RF-141, RNF-018), incluindo as notificações criadas sem e-mail por outras funcionalidades. Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Emails/EnvioEmailNotificacaoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U24.01 | `EmailDaNotificacaoEEnviado` | RF-141, RNF-018: o e-mail de uma notificação de atraso é submetido logo no primeiro ciclo, com o assunto do tipo e um corpo com a mensagem e a data em hora de Lisboa, e fica enviado. | 1 | RF-141, RNF-018 |
| U24.02 | `FalhaReenviaTresVezesEFicaNaoEntregue` | RF-118 CA1 e RF-119: com o serviço sempre em falha, há a tentativa inicial e 3 reenvios de 5 em 5 minutos, e o e-mail fica não entregue; a notificação mantém-se (RF-141). | 1 | RF-118, RF-119, RF-141 |
| U24.03 | `ServicoQueVoltaEntregaNoReenvioSeguinte` | RF-118 CA2: o serviço volta antes do segundo reenvio; o e-mail é entregue e não há mais tentativas. | 1 | RF-118 |
| U24.04 | `NotificacaoSemEmailGanhaEmailEEEnviada` | RF-141: uma notificação criada só na aplicação, por exemplo uma `NovaOcorrencia` de outra funcionalidade, ganha o e-mail no ciclo seguinte, para o endereço atual do destinatário, e é enviada; não volta a ser registada. | 1 | RF-141 |
| U24.05 | `EmailSemNotificacaoContaComoFalha` | Um e-mail de notificação cuja notificação desapareceu conta como falha, sem parar o processamento. | 1 | RF-118, RF-119, RF-141, RNF-018 |

**U25 `EnvioEmailServiceTestes`** (4 métodos, 4 casos): Testes unitários do envio e dos reenvios dos e-mails de convite no EnvioEmailService (RF-118, RF-119, RF-135, RF-136). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Emails/EnvioEmailServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U25.01 | `EmailDevidoEEnviado` | RF-041, RNF-018, RNF-029: o e-mail devido é submetido ao serviço de envio, com o assunto e um corpo que indica quem convidou, a ficha, o papel e a data em hora de Lisboa, e fica enviado. | 1 | RF-041, RNF-018, RNF-029 |
| U25.02 | `NoMaximoTresReenviosDepoisNaoEntregue` | RF-136 CA1 e CA2, RF-135 CA1: com o serviço de envio sempre em falha, há uma tentativa e três reenvios, de 5 em 5 minutos; depois o e-mail fica não entregue, não há mais tentativas e o convite continua pendente. | 1 | RF-135, RF-136 |
| U25.03 | `AntesDosCincoMinutosNaoReenvia` | RF-136: depois de uma falha, não há nova tentativa antes de passarem 5 minutos. | 1 | RF-136 |
| U25.04 | `ExcecaoDoEnvioContaComoFalha` | Uma exceção do serviço de envio conta como falha (reenvio agendado), sem impedir o envio dos outros e-mails devidos. | 1 | RF-118, RF-119, RF-135, RF-136 |

**U26 `AutorizacaoFichaServiceTestes`** (9 métodos, 9 casos): Testes unitários da verificação do papel na ficha (Matriz de Permissões, RP-01 a RP-03 e RP-13). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Fichas/AutorizacaoFichaServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U26.01 | `CuidadorEFamiliarConsultam` | RF-028: Cuidadores e Familiares associados consultam a ficha. | 1 | RF-028 |
| U26.02 | `NaoAssociadoEFichaInexistenteDao404` | RNF-005: quem não está associado, ou pede uma ficha inexistente, recebe a mesma resposta 404. | 1 | RNF-005 |
| U26.03 | `FamiliarNaoEscreve` | RF-048: o Familiar não faz operações de Cuidador (403). | 1 | RF-048 |
| U26.04 | `FichaArquivadaRecusaEscritasQueExigemFichaAtiva` | RP-13: numa ficha arquivada, as escritas que exigem ficha ativa são recusadas, mas o Cuidador continua a ser reconhecido. | 1 | - |
| U26.05 | `OrdemDasVerificacoes` | A ordem das verificações é 404, 403 e 409: o Familiar de uma ficha arquivada recebe 403. | 1 | - |
| U26.06 | `FichasDeCuidadorIncluemPrincipalProvisorioECuidador` | RF-100: entram as fichas ativas em que o Utilizador é Cuidador, seja Cuidador principal, Cuidador principal provisório ou Cuidador sem ser principal. | 1 | RF-100 |
| U26.07 | `FichaDeFamiliarSoEntraSemPapelExigido` | RF-100, CA2 e RP-17: as fichas em que o Utilizador é só Familiar não entram na lista diária, mas entram quando qualquer papel serve (agenda). | 1 | RF-100 |
| U26.08 | `FichaArquivadaNaoEntra` | RF-100, CA3: uma ficha arquivada não entra, mesmo para o seu Cuidador principal. | 1 | RF-100 |
| U26.09 | `UtilizadorSemFichasNaoRecebeNenhuma` | RNF-005: um Utilizador sem associações não recebe nenhuma ficha. | 1 | RNF-005 |

**U27 `FichaServiceTestes`** (24 métodos, 25 casos): Testes unitários das regras de negócio de FichaService (F002, F003). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Fichas/FichaServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U27.01 | `CriarFichaDeTerceiroComOCriadorComoPrincipal` | RF-020 CA1 e RF-024 CA1: a ficha de terceiro fica ativa, com as necessidades e o criador como Cuidador principal. | 1 | RF-020, RF-024 |
| U27.02 | `CriarComConviteDeixaOCriadorProvisorio` | RF-025 CA1: com outra pessoa para Cuidador principal, o criador fica provisório e o convite fica pendente. | 1 | RF-025 |
| U27.03 | `CriarFichaPropria` | RF-030: a ficha própria tem o criador como titular e Cuidador principal, sem precisar de cuidadorPrincipal. | 1 | RF-030 |
| U27.04 | `SoUmaFichaPropriaPorUtilizador` | RF-031: a segunda ficha própria é recusada; uma ficha de terceiro continua a ser permitida. | 1 | RF-031 |
| U27.05 | `DadosObrigatoriosEmFaltaSaoIndicados` | RF-020 CA2 e RNF-028: os campos obrigatórios em falta e a data de nascimento futura são indicados, e a ficha não é criada. | 1 | RF-020, RNF-028 |
| U27.06 | `LimitesDaDataDeNascimento` | A data de nascimento anterior a 1900 é recusada e a de hoje é aceite. | 1 | - |
| U27.07 | `ValoresForaDasListasFechadasSaoRecusados` | RF-021 CA2: valores fora das listas fechadas são recusados. | 1 | RF-021 |
| U27.08 | `FichaDeTerceiroExigeCuidadorPrincipal` | RF-024 CA2: a ficha de terceiro sem Cuidador principal, ou com convite sem e-mail válido, não é criada. | 1 | RF-024 |
| U27.09 | `CriarComDocumentosGuardaOsFicheiros` | RF-022: os documentos enviados na criação ficam associados à ficha com a categoria indicada. | 1 | RF-022 |
| U27.10 | `FalhaNumDocumentoNaoCriaAFicha` | RF-023 CA1: se um documento não puder ser guardado, a ficha não é criada e nenhum ficheiro fica no armazenamento. | 1 | RF-023 |
| U27.11 | `DocumentoInvalidoNaCriacaoIndicaOIndice` | Um documento inválido é recusado antes de qualquer ficheiro ser guardado, com o índice do documento no campo. | 1 | - |
| U27.12 | `ListarDevolveSoAsFichasDoUtilizador` | OP-07: a lista só tem as fichas do Utilizador, filtradas pelo estado quando pedido (RNF-005). | 1 | RNF-005 |
| U27.13 | `ObterRespeitaAAssociacao` | OP-10: o Familiar consulta a ficha e quem não está associado recebe 404 (RF-028). | 1 | RF-028 |
| U27.14 | `EditarGuardaAsAlteracoes` | RF-029 CA1: o Cuidador altera os dados e as necessidades e acrescenta documentos. | 1 | RF-029 |
| U27.15 | `EditarRecusaFamiliarEFichaArquivada` | RF-029 CA2 e RF-120: o Familiar não edita (403) e uma ficha arquivada não é editada (409). | 1 | RF-029, RF-120 |
| U27.16 | `EditarComFalhaNoDocumentoNaoAlteraNada` | RF-023 CA3 e RNF-046: se o documento acrescentado não for guardado, a ficha fica como estava. | 1 | RF-023, RNF-046 |
| U27.17 | `ArquivarApagaOsEventosFuturos` | RF-120 e RF-122: o Cuidador arquiva a ficha e as tomas futuras por confirmar são apagadas. | 1 | RF-120, RF-122 |
| U27.18 | `FamiliarNaoArquivaNemReativa` | RF-120 CA2: o Familiar não arquiva nem reativa a ficha. | 1 | RF-120 |
| U27.19 | `ReativarVoltaAoEstadoAtivo` | RF-121: o Cuidador reativa a ficha arquivada; a geração retoma a partir desse instante. | 1 | RF-121 |
| U27.20 | `ReativarRetomaAGeracaoDasTomasAPartirDaReativacao` | RF-122 e TR-10: ao reativar, as tomas dos tratamentos em curso voltam a ser geradas só a partir da reativação, sem tomas para o período em que a ficha esteve arquivada. | 1 | RF-122 |
| U27.21 | `EliminarFichaSemRegistos` | RF-123 CA1: a ficha sem registos é eliminada, com as notificações e os ficheiros dos documentos. | 1 | RF-123 |
| U27.22 | `EliminarFichaComRegistosERecusado` | RF-123 CA2: com medicação ou histórico, a eliminação é recusada e nada é removido. | 2 | RF-123 |
| U27.23 | `FamiliarNaoElimina` | O Familiar não elimina a ficha. | 1 | - |
| U27.24 | `ObterDominiosDevolveAsTresListas` | OP-08: devolve as três listas fechadas, com os graus pela ordem de apresentação. | 1 | - |

**U28 `ListaDiariaServiceTestes`** (10 métodos, 10 casos): Testes unitários da lista de tarefas diária consolidada (F010: RF-100, RF-101, RF-104, RF-087, RNF-047). Em novembro a hora de Lisboa coincide com UTC. Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/ListaDiaria/ListaDiariaServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U28.01 | `ReuneTomasECompromissosDasFichasEmQueECuidador` | RF-100, CA1, CA2 e CA5, e RF-101: reúne as tomas e os compromissos do dia das fichas em que o Utilizador é Cuidador, com o nome do utente; as fichas em que é Familiar não entram e os compromissos não são confirmáveis. | 1 | RF-100, RF-101 |
| U28.02 | `FichaArquivadaNaoAparece` | RF-100, CA3: os itens de uma ficha arquivada não aparecem. | 1 | RF-100 |
| U28.03 | `HojeIncluiTomasPendentesDoDiaAnterior` | RF-100, CA4: a lista de hoje inclui a toma do dia anterior que continua pendente, mas não as já confirmadas nem os compromissos do dia anterior; sem data, o dia é o de hoje em Lisboa. | 1 | RF-100 |
| U28.04 | `OutroDiaNaoIncluiTomasDoDiaAnterior` | RF-100: num dia que não é hoje, a lista só tem os itens desse dia. | 1 | RF-100 |
| U28.05 | `TomaConfirmadaIndicaQuemConfirmouEQuando` | RF-104: uma toma confirmada, a tempo ou com atraso, indica quem a confirmou e quando; uma toma não confirmada não indica nenhum dos dois. Nenhuma delas é confirmável. | 1 | RF-104 |
| U28.06 | `SoEConfirmavelATomaPendenteDoDiaComMenosDe24Horas` | RF-064 e RF-068: só é confirmável a toma pendente cujo dia já começou e que ainda não tem 24 horas; a pendente do dia anterior com mais de 24 horas aparece, mas não é confirmável, e a de amanhã ainda não é confirmável. | 1 | RF-064, RF-068 |
| U28.07 | `OrdenaPorHoraEDesempataPorTipoENomeDoUtente` | Os itens estão ordenados por hora; à mesma hora, a toma vem antes do compromisso e depois ordena-se pelo nome do utente. | 1 | RF-087, RF-100, RF-101, RF-104, RNF-047 |
| U28.08 | `DiaDaMudancaParaHoraDeInvernoTem25Horas` | RNF-047: no dia em que a hora muda para a de inverno (25 horas), o dia vai das 00:00 às 24:00 de Lisboa. | 1 | RNF-047 |
| U28.09 | `DiaDaMudancaParaHoraDeVeraoTem23Horas` | RNF-047: no dia em que a hora muda para a de verão (23 horas), o dia vai das 00:00 às 24:00 de Lisboa. | 1 | RNF-047 |
| U28.10 | `SemFichasDeCuidadorDevolveListaVazia` | RP-15: quem não é Cuidador de nenhuma ficha ativa recebe uma lista vazia, mesmo sendo Familiar de uma ficha com itens. | 1 | RF-087, RF-100, RF-101, RF-104, RNF-047 |

**U29 `NotificacaoServiceTestes`** (4 métodos, 4 casos): Testes unitários do NotificacaoService: destinatários por papel, ficha arquivada e histórico de notificações de OP-51 (RF-113 CA3, RF-119, RF-140, RF-142). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Notificacoes/NotificacaoServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U29.01 | `DestinatariosSaoOsDoPapelPedido` | Os Cuidadores recebem as notificações dirigidas aos Cuidadores e os Familiares as dirigidas aos Familiares; uma associação a um Utilizador que já não existe é ignorada. | 1 | RF-113, RF-119, RF-140, RF-142 |
| U29.02 | `FichaArquivadaNaoGeraNotificacoes` | RF-142: numa ficha arquivada não é criada nenhuma notificação nem e-mail. | 1 | RF-142 |
| U29.03 | `HistoricoTemSoAsDoUtilizadorDaMaisRecenteParaAMaisAntiga` | RF-140 CA2 e CA3, RF-119: o histórico tem só as notificações do Utilizador, da mais recente para a mais antiga, com a ficha, o medicamento e a hora prevista da toma e o estado do e-mail; sem e-mail registado aparece `Pendente`. | 1 | RF-119, RF-140 |
| U29.04 | `HistoricoFiltraPeloIntervaloDeDias` | OP-51: `de` e `ate` filtram pelo dia de Lisboa da criação, com `ate` inclusivo; `ate` anterior a `de` é recusado com o campo em erro (RNF-028). | 1 | RNF-028 |

**U30 `TextosNotificacaoTestes`** (3 métodos, 7 casos): Testes unitários de TextosNotificacao (RNF-020, RNF-029, OP-51). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Notificacoes/TextosNotificacaoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U30.01 | `MensagemDaTomaComoNoExemploDaApi` | A mensagem da toma por confirmar segue o exemplo de OP-51 e cabe nos 400 caracteres de `Notificacao.Mensagem`, mesmo com o medicamento (120) e o nome (150) mais longos. | 1 | RNF-020, RNF-029 |
| U30.02 | `AssuntoDoEmailPorTipo` | O assunto do e-mail depende do tipo, não tem dados de saúde e cabe nos 150 caracteres de `EnvioEmail.Assunto`; os tipos sem assunto próprio usam o genérico. | 5 | RNF-020, RNF-029 |
| U30.03 | `CorpoDoEmailTemAMensagemEAData` | O corpo do e-mail repete a mensagem da notificação, indica onde a consultar e quando foi criada. | 1 | RNF-020, RNF-029 |

**U31 `OcorrenciaServiceTestes`** (11 métodos, 11 casos): Testes unitários de OcorrenciaService (F009, RF-090 a RF-095; Matriz de Permissões, OP-46 a OP-49, RP-13, RP-15 e RP-18). Em novembro, a hora de Lisboa coincide com UTC. Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Ocorrencias/OcorrenciaServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U31.01 | `RegistarOcorrenciaGuardaOAutorEOsDoisInstantes` | RF-090 CA1 e RF-092 CA2: a ocorrência fica na ficha com o autor, o instante em que aconteceu (10:15) e o do registo (10:40). | 1 | RF-090, RF-092 |
| U31.02 | `CuidadorRegistaNotaComAutorEInstante` | RF-091 CA1 e RF-092 CA1: um Cuidador que não é o principal regista uma nota, com ele como autor e o instante do registo. | 1 | RF-091, RF-092 |
| U31.03 | `RegistarOcorrenciaNotificaTodosExcetoOAutor` | RF-115 CA1: a ocorrência do Cuidador principal notifica na aplicação o outro Cuidador e o Familiar, e não o autor; a notificação refere a ocorrência e tem como instante-limite o instante do registo. | 1 | RF-115 |
| U31.04 | `OAutorNuncaENotificadoEANotaNaoNotifica` | RF-115 CA1 e CA2: quando o autor é o outro Cuidador, quem recebe é o Cuidador principal e o Familiar; uma nota não notifica ninguém. | 1 | RF-115 |
| U31.05 | `FalhaNaGravacaoNaoDeixaOcorrenciaNemNotificacoes` | RNF-046: se a gravação da ocorrência falhar, não fica a ocorrência nem nenhuma notificação. | 1 | RNF-046 |
| U31.06 | `DadosEmFaltaOuInvalidosNaoRegistamNada` | RF-090 CA2 e RF-091 CA2: sem descrição, sem instante ou com texto demasiado longo, nada é registado e os campos são indicados. | 1 | RF-090, RF-091 |
| U31.07 | `SoUmCuidadorDaFichaRegista` | RF-090 CA3, RF-091 CA3 e RNF-005: o Familiar recebe ACESSO_PROIBIDO e quem não está associado recebe FICHA_NAO_ENCONTRADA. | 1 | RF-090, RF-091, RNF-005 |
| U31.08 | `FichaArquivadaSoPermiteConsultar` | RF-120 e RP-13: numa ficha arquivada não se registam ocorrências nem notas, mas as já registadas continuam a ser consultadas. | 1 | RF-120 |
| U31.09 | `FamiliarConsultaAsOcorrenciasDaMaisRecenteParaAMaisAntiga` | RF-094 CA1 e CA2: o Familiar vê as ocorrências da mais recente para a mais antiga, com o autor; quem não está associado recebe FICHA_NAO_ENCONTRADA. | 1 | RF-094 |
| U31.10 | `NotasSoSaoVisiveisAosCuidadoresDaFicha` | RF-093 CA1 e CA2, RF-095 e RP-15: o outro Cuidador vê a nota na consulta seguinte; o Familiar recebe ACESSO_PROIBIDO e o Cuidador de outra ficha recebe FICHA_NAO_ENCONTRADA. | 1 | RF-093, RF-095 |
| U31.11 | `FiltraPeloDiaDoRegistoEmHoraDeLisboa` | OP-48 e OP-49: `de` e `ate` são dias de Lisboa aplicados ao instante do registo, com `ate` inclusivo; o intervalo invertido é recusado no campo `ate`. | 1 | RF-090, RF-091, RF-092, RF-093, RF-094, RF-095 |

**U32 `PessoasServiceTestes`** (7 métodos, 9 casos): Testes unitários do PessoasService (F004; OP-18). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Pessoas/PessoasServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U32.01 | `CuidadorEFamiliarVeemAsPessoasComOPrincipalPrimeiro` | RF-025, DP-03: o Cuidador e o Familiar veem as pessoas da ficha, com o nome, o e-mail e o papel, e o Cuidador principal primeiro. | 2 | RF-025 |
| U32.02 | `NaoAssociadoEFichaInexistenteDao404` | RNF-005, RP-02: quem não está associado, ou pede uma ficha inexistente, recebe 404 sem ver as pessoas. | 1 | RNF-005 |
| U32.03 | `PromoverEDespromoverDevolvemAPessoaComONovoPapel` | RF-049 e RF-130: o Cuidador promove o Familiar e depois despromove-o; cada pedido devolve a pessoa com o nome, o e-mail e o novo papel, numa transação confirmada. | 1 | RF-049, RF-130 |
| U32.04 | `PapelEmFaltaOuInvalidoDa400` | RNF-028: o papel é obrigatório e só pode ser Cuidador ou Familiar, incluindo quando chega como número; nada é gravado. | 2 | RNF-028 |
| U32.05 | `FamiliarEFichaArquivadaSaoRecusados` | RNF-004, RF-048: o Familiar não altera papéis (403), e numa ficha arquivada a operação é recusada com 409 (RP-13). | 1 | RF-048, RNF-004 |
| U32.06 | `RemoverAcessoRetiraAPessoaDaFicha` | RF-131 CA1: o Cuidador remove o Familiar, que deixa de estar associado, numa transação confirmada. | 1 | RF-131 |
| U32.07 | `FamiliarARemoverSeEFichaArquivadaSaoRecusados` | DP-01, RP-13: o Familiar não se remove a si próprio (403) e numa ficha arquivada a remoção é recusada (409); nada muda. | 1 | - |

**U33 `AtrasoTomaServiceTestes`** (8 métodos, 9 casos): Testes unitários dos passos 2 e 3 do processo periódico em AtrasoTomaService: tolerância de cada grau e destinatários das notificações de atraso (RF-110 a RF-113, RF-141, RF-142, RNF-043; TR-05, TR-07 e TR-10). A ficha foi criada a 21 de outubro de 2026, 10:00 de Lisboa, e a toma está prevista para as 11:00 de Lisboa (10:00 UTC) do mesmo dia. Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Tomas/AtrasoTomaServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U33.01 | `LeveNotificaSoOsCuidadoresDepoisDasDuasHoras` | RF-110 CA1 e CA3: numa toma Leve, às T + 2 h ainda não há notificação; um segundo depois, cada Cuidador (incluindo o principal) recebe uma notificação `TomaEmAtraso` com a ficha, o medicamento e a hora prevista, e o respetivo e-mail; o Familiar nunca é notificado e a toma continua `Pendente` (RF-066). | 1 | RF-066, RF-110 |
| U33.02 | `ToleranciaDosCuidadoresEADoGrau` | RF-111 e RF-112: a tolerância dos Cuidadores é a do grau da toma (Moderado 1 h, Grave 30 min). | 2 | RF-111, RF-112 |
| U33.03 | `GraveNotificaOsFamiliaresTrintaMinutosDepoisDosCuidadores` | RF-112 e RF-113 CA1: numa toma Grave, os Cuidadores são notificados depois dos 30 minutos e o Familiar 30 minutos depois, com uma notificação `TomaGraveFamiliares` e o respetivo e-mail; aos 60 minutos exatos ainda não. | 1 | RF-112, RF-113 |
| U33.04 | `TomaConfirmadaAntesDaDetecaoNaoGeraNotificacao` | RF-112 CA2 e RF-113 CA2: uma toma confirmada antes da deteção não gera notificação; confirmada com atraso depois do aviso aos Cuidadores, não gera a dos Familiares. | 1 | RF-112, RF-113 |
| U33.05 | `RepetirOsPassosNaoDuplicaNotificacoes` | RNF-043: repetir os passos para o mesmo instante, ou mais tarde, não cria notificações nem e-mails duplicados. | 1 | RNF-043 |
| U33.06 | `GraveSemFamiliaresSoNotificaOsCuidadores` | RF-113 CA3: numa ficha sem Familiares não é criada nenhuma notificação para Familiares e a dos Cuidadores mantém-se; a toma fica marcada para não voltar a ser analisada. | 1 | RF-113 |
| U33.07 | `FichaArquivadaSuspendeAsNotificacoes` | RF-112 CA3 e RF-142 CA2: com a ficha arquivada não há notificações; depois de reativada, o instante-limite que ocorreu durante o arquivo continua sem notificação, mas o que ocorre depois da reativação é notificado (TR-10). | 1 | RF-112, RF-142 |
| U33.08 | `ConflitoDeVersaoNaoFazFalharOPasso` | RNF-044: se a toma for confirmada entre a leitura e a gravação (conflito de versão), a transação é desfeita, o passo não falha e a toma não conta como notificada. | 1 | RNF-044 |

**U34 `TomaServiceTestes`** (16 métodos, 16 casos): Testes unitários das operações da F006 em TomaService: listagem, confirmação, calendário e passos do processo periódico (RF-060 a RF-069, RNF-044; TR-03 a TR-15). O relógio está fixo em 7 de outubro de 2026, 08:30 de Lisboa. Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Tomas/TomaServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U34.01 | `CuidadorConfirmaDentroDaTolerancia` | TR-03 e RF-064: o Cuidador confirma dentro da tolerância; ficam o autor, o nome e o instante. | 1 | RF-064 |
| U34.02 | `FamiliarNaoConfirma` | TR-12: o Familiar não confirma; a toma fica pendente. | 1 | RF-060, RF-061, RF-062, RF-063, RF-064, RF-065, RF-066, RF-067, RF-068, RF-069, RNF-044 |
| U34.03 | `TomaInexistenteOuDeOutraFichaNaoEncontrada` | RNF-005: a toma inexistente e a de uma ficha não associada dão o mesmo `RECURSO_NAO_ENCONTRADO`. | 1 | RNF-005 |
| U34.04 | `SegundaConfirmacaoDevolveAConfirmacaoGravada` | TR-15 e RF-105: a segunda confirmação é recusada com quem confirmou e quando; a primeira mantém-se. | 1 | RF-105 |
| U34.05 | `ConflitoDeVersaoDevolveTomaJaConfirmada` | RNF-044: se outro Cuidador gravar primeiro (conflito de versão), a resposta é `TOMA_JA_CONFIRMADA` com a confirmação gravada. | 1 | RNF-044 |
| U34.06 | `ConflitoDeVersaoSemConfirmacaoVoltaASerLancado` | RNF-044: um conflito de versão sem confirmação gravada não é escondido. | 1 | RNF-044 |
| U34.07 | `FichaArquivadaAindaConfirma` | RP-13: numa ficha arquivada, as tomas ainda se confirmam (não dá `FICHA_ARQUIVADA`). | 1 | RF-060, RF-061, RF-062, RF-063, RF-064, RF-065, RF-066, RF-067, RF-068, RF-069, RNF-044 |
| U34.08 | `TomaDeAmanhaFicaForaDaJanela` | TR-13: a toma de amanhã ainda não pode ser confirmada. | 1 | RF-060, RF-061, RF-062, RF-063, RF-064, RF-065, RF-066, RF-067, RF-068, RF-069, RNF-044 |
| U34.09 | `ListarTomasDoDiaDeLisboa` | RF-063: Cuidadores e Familiares listam as tomas dos dias de Lisboa pedidos, por ordem; o filtro pelo estado aplica-se. | 1 | RF-063 |
| U34.10 | `ListarValidaOIntervalo` | OP-32: o intervalo é obrigatório e tem no máximo 31 dias, contando o primeiro e o último. | 1 | RF-060, RF-061, RF-062, RF-063, RF-064, RF-065, RF-066, RF-067, RF-068, RF-069, RNF-044 |
| U34.11 | `ListarSemAssociacaoNaoEncontraAFicha` | RNF-005: quem não está associado recebe `FICHA_NAO_ENCONTRADA`. | 1 | RNF-005 |
| U34.12 | `CalendarioTemSoAsPendentesDasProximas48Horas` | RF-061 e RF-062: o calendário tem só as tomas pendentes das próximas 48 horas das fichas ativas em que o Utilizador é Cuidador. | 1 | RF-061, RF-062 |
| U34.13 | `ConcluirNaoConfirmadasSoComVinteEQuatroHoras` | Passo 5 do processo periódico (TR-06): só as pendentes com 24 horas passam a não confirmadas. | 1 | RF-060, RF-061, RF-062, RF-063, RF-064, RF-065, RF-066, RF-067, RF-068, RF-069, RNF-044 |
| U34.14 | `ProlongarJanelaMovelGeraAsTomasEmFalta` | Passo 1 do processo periódico (RF-060): a janela móvel avança e geram-se só as tomas em falta, sem duplicados. | 1 | RF-060 |
| U34.15 | `ProlongarJanelaMovelIgnoraFichasArquivadasETratamentosTerminados` | RF-069: o passo 1 não gera tomas para fichas arquivadas nem para tratamentos terminados. | 1 | RF-069 |
| U34.16 | `RetomarGeracaoSoAPartirDaReativacao` | TR-10: ao retomar a geração de uma ficha reativada, só há tomas a partir do instante da reativação. | 1 | RF-060, RF-061, RF-062, RF-063, RF-064, RF-065, RF-066, RF-067, RF-068, RF-069, RNF-044 |

**U35 `TratamentoServiceTestes`** (14 métodos, 14 casos): Testes unitários de TratamentoService e da geração das tomas pelo TomaService (F005, RF-050 a RF-057, RF-060, RNF-046). Ficheiro: `backend/tests/DoseCerta.UnitTests/Aplicacao/Tratamentos/TratamentoServiceTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U35.01 | `RegistarGeraUmaTomaPorHorarioEPorDia` | RF-050 CA1 e RF-060 CA1: o tratamento fica em curso e geram-se 6 tomas pendentes (08:00 e 20:00 de 02 a 04 de novembro). | 1 | RF-050, RF-060 |
| U35.02 | `RegistarSemDataFimGeraAJanelaDeTrintaDias` | RF-060: sem data de fim, geram-se só as tomas da janela móvel de 30 dias. | 1 | RF-060 |
| U35.03 | `RegistarNoPassadoNaoGeraAntesDaFichaAtiva` | DM-19: com a data de início no passado, não se geram tomas anteriores ao instante em que a ficha ficou ativa. | 1 | RF-050, RF-051, RF-052, RF-053, RF-054, RF-055, RF-056, RF-057, RF-060, RNF-046 |
| U35.04 | `RegistarComReceitaAssociaOFicheiroAoTratamento` | RF-052: a receita fica associada ao tratamento, na categoria receita médica. | 1 | RF-052 |
| U35.05 | `RegistarComDadosInvalidosIndicaOsCampos` | RF-050 CA2, CA4 e RF-051: os campos obrigatórios em falta e os valores inválidos são indicados e nada é registado. | 1 | RF-050, RF-051 |
| U35.06 | `RegistarSoPorUmCuidadorNumaFichaAtiva` | RF-050 CA3, RNF-005 e RF-120: o Familiar recebe ACESSO_PROIBIDO, quem não está associado recebe FICHA_NAO_ENCONTRADA e numa ficha arquivada a operação é recusada com FICHA_ARQUIVADA. | 1 | RF-050, RF-120, RNF-005 |
| U35.07 | `ReceitaQueNaoEGuardadaNaoRegistaNada` | RF-053 CA1: se a receita não puder ser guardada, o tratamento não é registado e nada é gravado. | 1 | RF-053 |
| U35.08 | `AtualizarSoAfetaAsTomasFuturas` | RF-055 CA1 e RF-060 CA3: alterar a dose e o horário das 20:00 para as 21:00 a 2026-11-03 às 12:00 só muda as tomas futuras; as anteriores mantêm a hora e a dose. | 1 | RF-055, RF-060 |
| U35.09 | `AtualizarRespeitaPapelEstadoEDataDeInicio` | RF-055: só se altera um tratamento em curso, por um Cuidador associado; a data de início não muda. | 1 | RF-055 |
| U35.10 | `ReceitaNovaQueNaoEGuardadaDeixaOTratamentoComoEstava` | RF-053 CA2: se a receita nova não for guardada, o tratamento e as tomas ficam exatamente como estavam. | 1 | RF-053 |
| U35.11 | `TerminarRemoveAsTomasFuturas` | RF-056 CA1 e RF-060 CA2: terminar a 2026-11-03 às 12:00 apaga as tomas posteriores e mantém as anteriores; não se termina duas vezes. | 1 | RF-056, RF-060 |
| U35.12 | `TerminarConcluidosTerminaSoDepoisDaDataDeFim` | RF-056 (passo 4 do processo periódico): o tratamento com data de fim a 2026-11-04 continua em curso nesse dia e é terminado no dia seguinte, em hora de Lisboa; o tratamento sem data de fim nunca é terminado e um segundo ciclo não faz nada. | 1 | RF-056 |
| U35.13 | `TerminarNumaFichaArquivadaEPermitido` | RF-120: numa ficha arquivada o tratamento pode ser terminado, mas não pode ser alterado; o Familiar não termina. | 1 | RF-120 |
| U35.14 | `ListarEConsultarIncluemOsTerminados` | RF-054 e RF-057: Cuidadores e Familiares consultam os tratamentos em curso e os terminados, com a receita; o filtro pelo estado é aplicado e quem não está associado não vê nada. | 1 | RF-054, RF-057 |

#### 7.1.3 API

**U36 `FiltroDeErrosAssociacaoTestes`** (1 método, 8 casos): Testes unitários da tradução dos erros da associação de Cuidadores e Familiares (F004; Documentação da API, OP-19 a OP-26). Ficheiro: `backend/tests/DoseCerta.UnitTests/Api/FiltroDeErrosAssociacaoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U36.01 | `CodigoDeF004DaOEstadoDocumentado` | Cada código de F004 tem o texto da documentação da API e dá o código HTTP documentado, com o código e a mensagem no corpo. | 8 | - |

**U37 `FiltroDeErrosFichaPropriaTestes`** (1 método, 1 caso): Testes unitários da tradução do erro de proteção do titular da ficha própria (F003; Documentação da API, OP-24 a OP-26). Ficheiro: `backend/tests/DoseCerta.UnitTests/Api/FiltroDeErrosFichaPropriaTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U37.01 | `CuidadorPrincipalProtegidoDa403` | `CUIDADOR_PRINCIPAL_PROTEGIDO` dá 403, com o código e a mensagem da exceção no corpo (RF-033, RF-034). | 1 | RF-033, RF-034 |

**U38 `FiltroDeErrosRegistosTestes`** (1 método, 1 caso): Testes unitários da tradução do erro de imutabilidade das ocorrências e das notas (F009; Documentação da API, secção 4). Ficheiro: `backend/tests/DoseCerta.UnitTests/Api/FiltroDeErrosRegistosTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U38.01 | `RegistoImutavelDa405` | `REGISTO_IMUTAVEL` dá 405, com o código e a mensagem da exceção no corpo (RF-090, RF-091, RNF-049). | 1 | RF-090, RF-091, RNF-049 |

**U39 `FiltroDeErrosTestes`** (11 métodos, 34 casos): Testes unitários da tradução de erros em respostas da API (Documentação da API, secção 3.3). Ficheiro: `backend/tests/DoseCerta.UnitTests/Api/FiltroDeErrosTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U39.01 | `EstadoHttpCorrespondeAoContrato` | Cada código de erro tem o código HTTP definido na Documentação da API. | 21 | - |
| U39.02 | `EstadoHttpDeCodigoDesconhecidoE500` | Um código desconhecido dá 500. | 1 | - |
| U39.03 | `ConverterValidacaoIncluiOsCampos` | Uma exceção de validação dá 400 com os campos em erro. | 1 | - |
| U39.04 | `ConverterTomaJaConfirmadaIncluiAConfirmacao` | OP-33: o erro `TOMA_JA_CONFIRMADA` leva a confirmação gravada, com a data no desvio de Lisboa; os outros erros não têm o campo `confirmacao` no JSON. | 1 | - |
| U39.05 | `ConverterDominioUsaOCodigoDaExcecao` | Uma exceção de domínio dá o código HTTP do código de erro, sem lista de campos. | 1 | - |
| U39.06 | `ConstrutoresDasExcecoesDefinemOCodigo` | Os construtores sem código da exceção de domínio usam o código de erro interno; os da exceção de validação usam o código de validação, sem campos. | 1 | - |
| U39.07 | `ConverterPedidoMalFormadoDa400` | Um pedido mal formado dá 400 com o código de validação. | 1 | - |
| U39.08 | `ConverterErroInesperadoNaoExpoeDetalhes` | Um erro inesperado dá 500 sem expor a mensagem interna. | 1 | - |
| U39.09 | `TryHandleEscreveARespostaDeErro` | O filtro escreve a resposta com o código HTTP e o corpo comum dos erros, sem detalhes internos. | 1 | - |
| U39.10 | `RespostaValidacaoModeloListaOsCampos` | Os erros de formato detetados pelo ASP.NET Core dão 400 com os campos em erro. | 1 | - |
| U39.11 | `NomeDoCampoNormalizaAChave` | As chaves do estado do modelo são convertidas no nome do campo em camelCase. | 4 | - |

**U40 `ProcessoPeriodicoNotificacoesTestes`** (1 método, 1 caso): Testes unitários dos passos 2 e 3 do ProcessoPeriodico: notificações de atraso das tomas aos Cuidadores e, no grau Grave, aos Familiares (RF-110 a RF-113, RNF-042). Ficheiro: `backend/tests/DoseCerta.UnitTests/Api/ProcessoPeriodicoNotificacoesTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U40.01 | `CicloNotificaCuidadoresEFamiliaresDeTomaGrave` | RNF-042: um ciclo depois de uma toma Grave ter passado os 60 minutos sem confirmação cria, pela ordem dos instantes-limite, as notificações aos Cuidadores (passo 2) e aos Familiares (passo 3) através do `AtrasoTomaService` do âmbito. | 1 | RNF-042 |

**U41 `ProcessoPeriodicoTestes`** (5 métodos, 5 casos): Testes unitários do ProcessoPeriodico (Modelo de Classes do Backend, secção 8.1; passos 1 e 5 das tomas, RF-060 e RF-068, passo 4 dos tratamentos, RF-056, passo dos e-mails, RF-136, e passo 7 das credenciais, RF-016). Ficheiro: `backend/tests/DoseCerta.UnitTests/Api/ProcessoPeriodicoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U41.01 | `CicloEnviaOsEmailsDevidos` | Um ciclo processa os e-mails devidos através do `EnvioEmailService` do âmbito. | 1 | RF-016, RF-056, RF-060, RF-068, RF-136 |
| U41.02 | `CicloTerminaTratamentosConcluidosEEliminaCredenciaisExpiradas` | Passos 4 e 7 (RF-056, RF-016): o ciclo termina o tratamento cuja data de fim já passou e elimina as credenciais terminadas que já expiraram, mantendo as que ainda estão dentro da validade. | 1 | RF-016, RF-056 |
| U41.03 | `CicloProlongaAJanelaEConcluiAsTomasNaoConfirmadas` | Um ciclo prolonga a janela móvel das tomas (passo 1) e passa a não confirmada a toma pendente com 24 horas (passo 5, TR-06). | 1 | RF-016, RF-056, RF-060, RF-068, RF-136 |
| U41.04 | `FalhaDeUmPassoNaoParaOProcesso` | A falha de um passo fica registada e não sai do ciclo, para o processo continuar nos ciclos seguintes. | 1 | RF-016, RF-056, RF-060, RF-068, RF-136 |
| U41.05 | `DesligadoTerminaNoArranque` | Desligado pela configuração (testes de integração), o processo termina logo no arranque, sem ciclos. | 1 | RF-016, RF-056, RF-060, RF-068, RF-136 |

#### 7.1.4 Infraestrutura

**U42 `ArmazenamentoSistemaFicheirosTestes`** (4 métodos, 4 casos): Testes unitários do armazenamento de documentos no sistema de ficheiros, numa pasta temporária (RNF-006, RNF-007). Ficheiro: `backend/tests/DoseCerta.UnitTests/Infraestrutura/ArmazenamentoSistemaFicheirosTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U42.01 | `GuardarEAbrirDevolvemOMesmoConteudo` | O ficheiro é guardado com um nome gerado pelo servidor e lido de volta com o mesmo conteúdo. | 1 | RNF-006, RNF-007 |
| U42.02 | `EliminarApagaOFicheiro` | A remoção apaga o ficheiro; remover um ficheiro inexistente não falha. | 1 | RNF-006, RNF-007 |
| U42.03 | `CaminhoForaDaPastaERecusado` | RNF-007: um caminho fora da pasta dos documentos nunca é aberto. | 1 | RNF-007 |
| U42.04 | `PastaPorOmissaoFicaNosDadosLocais` | Sem pasta configurada, usa a pasta de dados locais do utilizador. | 1 | RNF-006, RNF-007 |

**U43 `ConfiguracaoTestes`** (13 métodos, 14 casos): Testes unitários da configuração da solução (JWT, e-mail, relógio, contexto EF Core e comando de teste do e-mail). Ficheiro: `backend/tests/DoseCerta.UnitTests/Infraestrutura/ConfiguracaoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U43.01 | `OpcoesJwtRecusaChaveInvalida` | Uma chave JWT em falta ou curta é recusada com uma mensagem clara. | 2 | - |
| U43.02 | `OpcoesJwtAceitaChaveComTamanhoMinimo` | Uma chave JWT com 32 carateres é aceite. | 1 | - |
| U43.03 | `ConfiguracaoJwtBearerDefineAValidacaoDoToken` | A validação dos tokens exige assinatura HMAC-SHA256 com a chave configurada, emissor, público e prazo de validade, sem tolerância de relógio. | 1 | - |
| U43.04 | `ConfiguracaoJwtBearerIgnoraOutrosEsquemas` | Outros esquemas de autenticação não são alterados. | 1 | - |
| U43.05 | `ConfiguracaoJwtBearerSemChaveFalha` | Sem chave JWT configurada, a autenticação não arranca e indica como definir a chave. | 1 | - |
| U43.06 | `OpcoesEmailExigemServidorERemetente` | O e-mail só é considerado configurado com servidor e remetente. | 1 | - |
| U43.07 | `EmailSenderSmtpSemConfiguracaoDevolveFalso` | Sem configuração SMTP, o envio não é tentado e devolve falso. | 1 | - |
| U43.08 | `EmailSenderSmtpComServidorIndisponivelDevolveFalso` | Uma falha na ligação ao servidor SMTP não é propagada: o envio devolve falso. | 1 | - |
| U43.09 | `ComandoEmailTesteReconheceOArgumento` | O comando do e-mail de teste só é ativado com o argumento e um destino. | 1 | - |
| U43.10 | `ComandoEmailTesteEnviaAMensagemEDevolveZero` | O comando envia a mensagem de teste ao destino, com a hora de Lisboa, e termina com o código 0. | 1 | - |
| U43.11 | `ComandoEmailTesteComFalhaDevolveUm` | Se o serviço de e-mail recusar a mensagem, o comando termina com o código 1. | 1 | - |
| U43.12 | `RelogioSistemaDevolveUtcAoSegundo` | O relógio do sistema devolve o instante em UTC com precisão de um segundo e o calendário de Lisboa. | 1 | - |
| U43.13 | `FabricaDesignCriaContextoSqlServer` | A fábrica usada pelas ferramentas do EF Core cria o contexto sobre SQL Server sem ligar à base de dados. | 1 | - |

**U44 `ConversorViolacaoUnicidadeAssociacaoTestes`** (3 métodos, 4 casos): Testes unitários da conversão das violações dos índices únicos da associação a uma ficha (F004, RF-043; D8). Ficheiro: `backend/tests/DoseCerta.UnitTests/Infraestrutura/ConversorViolacaoUnicidadeAssociacaoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U44.01 | `ConvitePendenteRepetidoDaConvitePendenteExiste` | RF-043: a violação de `UQ_Convite_Pendente` dá `CONVITE_PENDENTE_EXISTE`, com a mesma mensagem da verificação prévia. | 2 | RF-043 |
| U44.02 | `AssociacaoRepetidaDaPessoaJaAssociada` | D8: a violação de `UQ_Associacao_FichaUtilizador` dá `PESSOA_JA_ASSOCIADA`, com a mesma mensagem do domínio. | 1 | RF-043 |
| U44.03 | `IndiceDoCuidadorPrincipalNaoEConvertido` | D8, limitação conhecida: a violação de `UQ_Associacao_Principal` não é convertida, porque a documentação da API não define o erro de duas transferências do Cuidador principal em simultâneo. | 1 | RF-043 |

**U45 `ConversorViolacaoUnicidadeTestes`** (4 métodos, 7 casos): Testes unitários da conversão das violações de índices únicos em erros de negócio (RF-031). O caminho completo, com a exceção real do SQL Server, é testado na integração (`UnidadeDeTrabalhoUnicidadeTestes`). Ficheiro: `backend/tests/DoseCerta.UnitTests/Infraestrutura/ConversorViolacaoUnicidadeTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U45.01 | `IndiceDaFichaPropriaDaFichaPropriaJaExiste` | A mensagem do SQL Server com o índice da ficha própria dá `FICHA_PROPRIA_JA_EXISTE`, com a mesma mensagem da verificação prévia. | 2 | RF-031 |
| U45.02 | `OutrosIndicesNaoSaoConvertidos` | Outros índices únicos, ou nomes que só começam como o da ficha própria, não são convertidos. | 3 | RF-031 |
| U45.03 | `FalhaSemErroDoSqlServerNaoEConvertida` | Uma falha de gravação que não vem do SQL Server não é convertida, mesmo que a mensagem refira o índice. | 1 | RF-031 |
| U45.04 | `IndiceConvertidoExisteNoModelo` | O nome do índice convertido é o do modelo EF, que é único e filtrado; assim, mudar o nome no modelo faz falhar este teste. | 1 | RF-031 |

**U46 `ModeloEfTestes`** (6 métodos, 6 casos): Testes unitários do mapeamento EF Core do modelo de dados, sem base de dados (I030; modelo de dados, secção 13.2). Ficheiro: `backend/tests/DoseCerta.UnitTests/Infraestrutura/ModeloEfTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U46.01 | `ModeloMapeiaAsDezanoveTabelas` | O modelo mapeia as 19 tabelas do esquema dbo (modelo de dados v02, sem a F007), incluindo as duas tabelas de junção das necessidades. | 1 | - |
| U46.02 | `TabelasComGatilhosDeclaramOsGatilhos` | As tabelas com gatilhos declaram-nos, para o EF Core não usar OUTPUT nas gravações dessas tabelas. | 1 | - |
| U46.03 | `ListasFechadasTemValoresPredefinidos` | As listas fechadas têm os valores predefinidos da secção 8 do modelo de dados (9 condições, 6 alergias, 4 graus). | 1 | - |
| U46.04 | `EmailEUnicoSemDistinguirMaiusculas` | O e-mail usa a collation sem distinção de maiúsculas nem de acentos e é único (UQ_Utilizador_Email, RF-011). | 1 | RF-011 |
| U46.05 | `RegrasDeEliminacaoSeguemOModelo` | Eliminar uma ficha elimina as associações por cascata; um Utilizador referenciado não é eliminado (NO ACTION). | 1 | - |
| U46.06 | `GatilhosSaoCriadosERemovidosPelaMigracao` | A migração inicial cria os quatro gatilhos no fim do Up, incluindo o da F007, e remove-os no início do Down. | 1 | - |

**U47 `SegurancaTestes`** (5 métodos, 9 casos): Testes unitários do hash das palavras-passe (RNF-001) e da emissão dos tokens JWT (RNF-009, RNF-054). Ficheiro: `backend/tests/DoseCerta.UnitTests/Infraestrutura/SegurancaTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| U47.01 | `HashTemSaltAleatorioEEVerificavel` | O hash tem salt aleatório: a mesma palavra-passe dá hashes diferentes, ambos verificáveis. | 1 | RNF-001, RNF-009, RNF-054 |
| U47.02 | `HashComFormatoInvalidoNaoEAceite` | Um hash com formato inválido nunca é aceite. | 5 | RNF-001, RNF-009, RNF-054 |
| U47.03 | `HashDoScriptDeDadosIniciaisEAceite` | As contas de demonstração do script de dados iniciais usam o mesmo formato de hash (I030). | 1 | RNF-001, RNF-009, RNF-054 |
| U47.04 | `TokenTemOsDadosEAValidadeDeOitoHoras` | O token tem sub, jti, emissor e público configurados e expira 8 horas depois da emissão. | 1 | RNF-001, RNF-009, RNF-054 |
| U47.05 | `TokenComOutraChaveERecusado` | O token só é válido com a chave do servidor (assinatura alterada é recusada). | 1 | RNF-001, RNF-009, RNF-054 |

### 7.2 Testes de integração

#### 7.2.1 API

**I01 `AgendaApiTestes`** (4 métodos, 4 casos): Testes de integração de OP-45 (F008, I044): a agenda reúne as tomas e os compromissos das fichas ativas do Utilizador. Usa datas de 2027, fora da janela das tomas geradas por outros testes; os dados são criados na base de dados de testes. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/AgendaApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I01.01 | `AgendaDoCuidador` | RF-083 a RF-089: a vista diária devolve as tomas e os compromissos do dia, por ordem do início, com o formato do contrato; as fichas arquivadas e as de outras pessoas não aparecem. | 1 | RF-083, RF-084, RF-085, RF-086, RF-087, RF-088, RF-089 |
| I01.02 | `FamiliarVeAAgendaSemConfirmar` | RF-048 e RF-087: o Familiar vê a agenda da ficha, mas nada é confirmável. | 1 | RF-048, RF-087 |
| I01.03 | `VistaSemanalESemFichas` | RF-085: a vista semanal vai de segunda-feira a domingo; RF-083: sem fichas, a agenda é vazia. | 1 | RF-083, RF-085 |
| I01.04 | `ValidacaoDosParametros` | RNF-028: a vista e a data são obrigatórias e validadas; sem token dá 401. | 1 | RNF-028 |

**I02 `CompromissosApiTestes`** (5 métodos, 5 casos): Testes de integração de OP-41 a OP-44 e da recusa da confirmação de compromissos (F008, I044): a API completa com o SQL Server. As fichas e as associações são criadas diretamente na base de dados de testes. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/CompromissosApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I02.01 | `RegistarListarEditarERemover` | OP-41 a OP-44: o Cuidador regista, o Familiar lista, o Cuidador edita e remove; o compromisso removido deixa de aparecer. | 1 | - |
| I02.02 | `ValidacaoIndicaOCampo` | RF-080, RF-116 e RNF-028: campos em falta, antecedência fora dos valores permitidos e data inválida dão 400 com o campo. | 1 | RF-080, RF-116, RNF-028 |
| I02.03 | `PermissoesPorPapel` | RP-11 e RNF-005: o Familiar recebe 403 nas escritas, quem não está associado 404 e sem token 401. | 1 | RNF-005 |
| I02.04 | `FichaArquivadaSoParaConsulta` | RP-13 e RF-120: numa ficha arquivada as escritas dão 409 `FICHA_ARQUIVADA` e a consulta continua disponível. | 1 | RF-120 |
| I02.05 | `ConfirmarCompromissoDa405` | RF-087: o pedido de confirmação de um compromisso responde sempre 405 `COMPROMISSO_NAO_CONFIRMAVEL`, para qualquer papel. | 1 | RF-087 |

**I03 `ConcorrenciaAssociacaoApiTestes`** (3 métodos, 3 casos): Testes de integração da F004 com pedidos simultâneos, com a API completa e o SQL Server (RF-043, RF-132, RNF-046; D8). A recusa pode vir da verificação prévia ou do índice único; o caminho do índice é provado em `UnidadeDeTrabalhoUnicidadeAssociacaoTestes`. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/ConcorrenciaAssociacaoApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I03.01 | `ConvitesSimultaneosCriamSoUmConvite` | RF-043 com pedidos simultâneos: só um convite é criado; os outros recebem 409 `CONVITE_PENDENTE_EXISTE`, nunca 500, e fica um só e-mail de convite. | 1 | RF-043 |
| I03.02 | `AceitacoesSimultaneasAssociamUmaSoVez` | D8 com pedidos simultâneos: o mesmo convite aceite várias vezes ao mesmo tempo associa o convidado uma só vez; os outros pedidos recebem 409 (`CONVITE_JA_RESPONDIDO` se chegam depois da primeira resposta, `PESSOA_JA_ASSOCIADA` se a corrida chega ao índice), nunca 500. | 1 | RF-043, RF-132, RNF-046 |
| I03.03 | `TransferenciasSimultaneasDeixamSempreUmSoPrincipal` | D8, limitação conhecida: duas transferências do Cuidador principal em simultâneo, para Cuidadores diferentes, podem dar 500 (a violação de `UQ_Associacao_Principal` não é traduzida), mas a ficha fica sempre com exatamente um Cuidador principal. | 1 | RF-043, RF-132, RNF-046 |

**I04 `ContaApiTestes`** (8 métodos, 8 casos): Testes de integração dos endpoints de F001 (OP-01 a OP-06), com a API completa e o SQL Server. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/ContaApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I04.01 | `RegistoDevolve201EGuardaSoOHash` | OP-01: o registo devolve 201 com a conta, sem palavra-passe, e guarda apenas o hash (RNF-001). | 1 | RNF-001 |
| I04.02 | `RegistoRecusaEmailDuplicadoEDadosInvalidos` | OP-01: e-mail duplicado dá 409 e dados inválidos dão 400 com os campos em erro. | 1 | - |
| I04.03 | `LoginDevolveTokenComValidadeDeOitoHoras` | OP-02: o início de sessão devolve um token Bearer que expira 8 horas depois (RNF-009). | 1 | RNF-009 |
| I04.04 | `LoginComCredenciaisErradasDaMesmaResposta` | OP-02 e RF-012: e-mail inexistente e palavra-passe errada dão a mesma resposta 401. | 1 | RF-012 |
| I04.05 | `EndpointProtegidoRecusaSemTokenOuComTokenAlterado` | RF-017: os endpoints protegidos recusam pedidos sem token ou com token alterado. | 1 | RF-017 |
| I04.06 | `ConsultaEAtualizacaoDaConta` | OP-04 e OP-05: consulta e atualização dos dados da conta; e-mail de outra conta dá 409. | 1 | - |
| I04.07 | `AlteracaoDaPalavraPasse` | OP-06: a palavra-passe atual errada dá 403; a alteração correta faz a anterior deixar de ser aceite. | 1 | - |
| I04.08 | `TerminarSessaoInvalidaOToken` | OP-03 e RF-016: depois de terminar a sessão, o mesmo token é recusado com 401. | 1 | RF-016 |

**I05 `ConvitesApiTestes`** (8 métodos, 8 casos): Testes de integração da criação de convites (F004, OP-19), com a API completa e o SQL Server. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/ConvitesApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I05.01 | `CuidadorConvidaFamiliar` | RF-040 CA1, RP-09: o Cuidador convida um e-mail como Familiar; a resposta 201 tem os campos da documentação, com `criadoEm` no desvio de Lisboa, e o convite fica pendente na base de dados sem associar ninguém à ficha. | 1 | RF-040 |
| I05.02 | `DadosInvalidosDao400` | RF-040 CA2, RNF-028: sem papel, com papel desconhecido, com e-mail inválido ou com `paraPrincipal=true` e papel Familiar, o convite não é criado e a resposta 400 indica o campo. | 1 | RF-040, RNF-028 |
| I05.03 | `ConvitePendenteDuplicadoSoNaMesmaFicha` | RF-043 CA1 e CA3: um segundo convite pendente para o mesmo e-mail na mesma ficha é recusado, mesmo com maiúsculas diferentes; noutra ficha, o convite é criado. | 1 | RF-043 |
| I05.04 | `PessoaJaAssociadaDa409` | OP-19: o e-mail de uma pessoa já associada à ficha é recusado com 409 `PESSOA_JA_ASSOCIADA`, também o de quem convida. | 1 | - |
| I05.05 | `ConviteParaPrincipalDeixaOPrincipalProvisorio` | RF-025, D2a, D2b: o convite para Cuidador principal deixa o principal atual provisório; um segundo convite para Cuidador principal, para outro e-mail, dá 409 `CONVITE_PENDENTE_EXISTE`. | 1 | RF-025 |
| I05.06 | `NaFichaPropriaSoOConviteParaPrincipalERecusado` | RF-034, RF-032: na ficha própria, o convite para Cuidador principal dá 403 `CUIDADOR_PRINCIPAL_PROTEGIDO`, mas os convites para Cuidador ou Familiar seguem as regras das restantes fichas. | 1 | RF-032, RF-034 |
| I05.07 | `FamiliarNaoAssociadoEFichaArquivadaNaoConvidam` | RF-048 CA2, RNF-005, RP-13: o Familiar recebe 403, quem não está associado recebe 404 e, numa ficha arquivada, 409. | 1 | RF-048, RNF-005 |
| I05.08 | `SemSessaoDa401` | RF-017: sem token, o pedido é recusado com 401. | 1 | RF-017 |

**I06 `ConvitesRecebidosApiTestes`** (4 métodos, 4 casos): Testes de integração da área de convites (F004, OP-20), com a API completa e o SQL Server. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/ConvitesRecebidosApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I06.01 | `UtilizadorVeOsConvitesParaOSeuEmail` | RF-042 CA1: o Utilizador registado vê os convites pendentes para o seu e-mail, com a ficha, o papel oferecido e quem convidou, do mais recente para o mais antigo; os convites para outros e-mails não aparecem. | 1 | RF-042 |
| I06.02 | `ConviteAntesDoRegistoApareceDepoisDeCriarAConta` | RF-044 CA1: o convite para um e-mail sem conta aparece na área de convites da conta criada depois com esse e-mail, e o registo (OP-01) já indica um convite pendente. | 1 | RF-044 |
| I06.03 | `RespondidoNaoApareceEFichaArquivadaContinuaAAparecer` | RF-046, UC-14 A5: o convite já respondido deixa de aparecer; o convite de uma ficha arquivada depois de convidar continua a aparecer, porque o convidado pode responder na mesma. | 1 | RF-046 |
| I06.04 | `SemSessaoDa401` | RF-017: sem token, o pedido é recusado com 401. | 1 | RF-017 |

**I07 `DocumentosApiTestes`** (8 métodos, 8 casos): Testes de integração dos documentos de F002 (OP-09 e OP-11 com documentos, OP-15 a OP-17), com a API completa, o SQL Server e a pasta de documentos da execução. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/DocumentosApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I07.01 | `CriarFichaComDocumentos` | OP-09 em multipart: a ficha é criada com os documentos, guardados na pasta do servidor (RF-022, RNF-006). | 1 | RF-022, RNF-006 |
| I07.02 | `MultipartSemDadosValidosERecusado` | OP-09 em multipart: uma parte dados em falta ou inválida dá 400 com o campo dados. | 1 | - |
| I07.03 | `CarregarListarEDescarregar` | OP-15, OP-16 e OP-17: o Cuidador carrega; o Familiar lista e descarrega; quem não está associado recebe 404 (RF-028, RNF-007). | 1 | RF-028, RNF-007 |
| I07.04 | `CarregamentoInvalidoERecusado` | OP-15: categoria inválida (400), formato não suportado (415), mais de 10 MB (413), Familiar (403) e ficha arquivada (409). | 1 | - |
| I07.05 | `FalhaAoGuardarNaoCriaAFicha` | RF-023: se o ficheiro não puder ser guardado, a ficha não é criada, nada é gravado e a resposta é 503. | 1 | RF-023 |
| I07.06 | `FicheiroEmFaltaDa503` | RF-124: se o ficheiro guardado não puder ser obtido, a resposta é 503 e a ficha continua consultável. | 1 | RF-124 |
| I07.07 | `EditarComDocumentos` | OP-11 em multipart: a edição acrescenta documentos à ficha. | 1 | - |
| I07.08 | `EliminarFichaRemoveOsFicheiros` | OP-14: ao eliminar a ficha, os ficheiros dos documentos são removidos da pasta do servidor (RF-123, RNF-046). | 1 | RF-123, RNF-046 |

**I08 `EmailConviteApiTestes`** (4 métodos, 4 casos): Testes de integração do registo da notificação e do e-mail de convite (F004, RF-041; OP-09 e OP-19), com o SQL Server. O envio e os reenvios são testados à parte. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/EmailConviteApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I08.01 | `ConviteParaContaGravaNotificacaoEEmail` | RF-041 CA1, D1: o convite para um e-mail com conta grava a notificação `ConviteRecebido` dessa conta e um e-mail pendente, ligado ao convite e à notificação, com a primeira tentativa devida de imediato. | 1 | RF-041 |
| I08.02 | `ConviteSemContaGravaSoOEmail` | RF-041 CA2: o convite para um e-mail sem conta (OP-19) e o convite para Cuidador principal da criação da ficha (OP-09) gravam só o e-mail pendente, sem notificação. | 1 | RF-041 |
| I08.03 | `ConviteRecusadoNaoGravaEmail` | RF-043, RNF-046: um convite recusado por ser repetido não grava notificação nem e-mail. | 1 | RF-043, RNF-046 |
| I08.04 | `EliminarFichaApagaConvitesNotificacoesEEmails` | RF-123 (I033) com convites: eliminar uma ficha sem registos apaga também os convites, as notificações de convite e os e-mails. | 1 | RF-123 |

**I09 `EnvioEmailConviteApiTestes`** (3 métodos, 3 casos): Testes de integração do envio e dos reenvios do e-mail de convite (F004, RF-135, RF-136; passo 7 do processo periódico), com o SQL Server e um serviço de e-mail controlado. O processo periódico está desligado nos testes; os ciclos são chamados diretamente, com o instante que cada teste precisa. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/EnvioEmailConviteApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I09.01 | `FalhaDoEmailNaoImpedeOConviteEReenviaTresVezes` | RF-136 CA1 e CA2, RF-135 CA1: com o serviço de e-mail sempre em falha, o convite é criado (201), o e-mail é tentado uma vez e reenviado três vezes, de 5 em 5 minutos, e fica não entregue; o convite continua na área de convites do convidado. | 1 | RF-135, RF-136 |
| I09.02 | `EmailDeConviteEEnviado` | RF-041, RNF-018: o e-mail de convite é submetido ao serviço de e-mail com o assunto e o corpo do convite, e fica enviado. | 1 | RF-041, RNF-018 |
| I09.03 | `ProcessoPeriodicoEnviaOEmail` | O processo periódico registado no arranque executa o passo 7: um ciclo envia o e-mail de convite devido. | 1 | RF-135, RF-136 |

**I10 `FichaPropriaApiTestes`** (6 métodos, 6 casos): Testes de integração da ficha própria (F003, OP-09 com `propria=true`), com a API completa e o SQL Server (RF-030 a RF-032). Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/FichaPropriaApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I10.01 | `CriarFichaPropriaDeixaOTitularComoPrincipal` | RF-030 CA1: a ficha própria é criada com o titular como Cuidador principal, nunca provisório, e sem convite. | 1 | RF-030 |
| I10.02 | `PedidosSimultaneosCriamSoUmaFichaPropria` | RF-031 CA1 com pedidos simultâneos: só um cria a ficha própria; os outros recebem 409 `FICHA_PROPRIA_JA_EXISTE`, nunca 500. A recusa pode vir da verificação prévia ou do índice `UQ_Ficha_UtilizadorProprio`; o caminho do índice é provado em `UnidadeDeTrabalhoUnicidadeTestes`. | 1 | RF-031 |
| I10.03 | `CuidadorPrincipalEIgnoradoNaFichaPropria` | OP-09: com `propria=true` o campo `cuidadorPrincipal` é ignorado; o titular fica Cuidador principal e não é criado convite (decisão do Executor: a documentação só exige o campo numa ficha de terceiro). | 1 | RF-030, RF-031, RF-032 |
| I10.04 | `ComFichaPropriaPodeCriarFichaDeTerceiro` | RF-031 CA2: quem já tem ficha própria pode criar fichas de terceiros. | 1 | RF-031 |
| I10.05 | `FichaPropriaTemAsMesmasOperacoesDasOutras` | RF-032 CA1: a ficha própria aceita documentos na criação e é listada, editada, arquivada e reativada como uma ficha de terceiro. | 1 | RF-032 |
| I10.06 | `FamiliarNaFichaPropriaSoConsulta` | RF-032 e RNF-005: na ficha própria, o Familiar só consulta (403 ao editar) e quem não está associado recebe 404. | 1 | RF-032, RNF-005 |

**I11 `FichasApiTestes`** (11 métodos, 11 casos): Testes de integração dos endpoints de fichas de F002 e F003 (OP-07 a OP-14), com a API completa e o SQL Server. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/FichasApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I11.01 | `DominiosDevolveAsListasFechadas` | OP-08: devolve as listas fechadas dos dados predefinidos (RF-021). | 1 | RF-021 |
| I11.02 | `CriarEConsultarFichaDeTerceiro` | OP-09 e OP-10: a ficha de terceiro é criada ativa, com o criador como Cuidador principal, e consultada com o contrato da API. | 1 | - |
| I11.03 | `CriarComConviteParaPrincipalDeixaOCriadorProvisorio` | RF-025: com outra pessoa para Cuidador principal, o criador fica provisório e o convite fica pendente. | 1 | RF-025 |
| I11.04 | `FichaPropriaSoUmaVez` | RF-030 e RF-031: a ficha própria é criada uma só vez por Utilizador. | 1 | RF-030, RF-031 |
| I11.05 | `CriarComDadosInvalidosOuSemSessaoERecusado` | OP-09: dados inválidos dão 400 com os campos em erro e um pedido sem sessão dá 401. | 1 | - |
| I11.06 | `ListarFichasDoUtilizador` | OP-07: a lista só tem as fichas do Utilizador, com o filtro de estado (RNF-005). | 1 | RNF-005 |
| I11.07 | `ConsultaRespeitaAAssociacao` | RF-028 e RNF-005: o Familiar consulta a ficha; quem não está associado, ou só tem convite pendente, recebe 404. | 1 | RF-028, RNF-005 |
| I11.08 | `EditarFicha` | OP-11: o Cuidador edita; o Familiar recebe 403 e uma ficha arquivada 409 (RF-029, RF-120). | 1 | RF-029, RF-120 |
| I11.09 | `ArquivarEReativar` | OP-12 e OP-13: arquivar apaga só as tomas futuras por confirmar; reativar volta a ativa; repetir dá 409 (RF-120 a RF-122). | 1 | RF-120, RF-121, RF-122 |
| I11.10 | `EliminarFichaSemRegistos` | OP-14: a ficha sem registos é eliminada com as associações, os convites e as notificações (RF-123 CA1). | 1 | RF-123 |
| I11.11 | `EliminarFichaComMedicacaoERecusado` | OP-14: com medicação, a eliminação é recusada com 409 e nada é removido; o Familiar recebe 403 (RF-123 CA2). | 1 | RF-123 |

**I12 `ListaDiariaApiTestes`** (6 métodos, 8 casos): Testes de integração de OP-50 (F010, I046): a API completa com o SQL Server. As tomas e os compromissos são criados diretamente na base de dados, e as tomas confirmadas com um `UPDATE`, porque a confirmação (F006) e a agenda (F008) são de outras tarefas. O dia usado é de verão, para verificar o desvio `+01:00` de Lisboa. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/ListaDiariaApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I12.01 | `CadaCuidadorVeOsItensDoDiaDasSuasFichas` | RF-100 (CA1, CA2 e CA5), RF-101 e RF-104: cada Cuidador vê as tomas e os compromissos do dia das fichas em que é Cuidador, com o nome do utente, ordenados por hora; a toma confirmada por outro Cuidador aparece com quem a confirmou e quando; as fichas em que é Familiar não aparecem e os compromissos não são confirmáveis. | 1 | RF-100, RF-101, RF-104 |
| I12.02 | `FichaArquivadaDeixaDeAparecer` | RF-100, CA3: depois de arquivada, os itens da ficha deixam de aparecer, incluindo os compromissos e as tomas confirmadas. | 1 | RF-100 |
| I12.03 | `SemDataUsaODiaDeHojeEmLisboa` | RNF-047: sem a data, a lista é a do dia de hoje na hora de Lisboa, e a toma pendente de hoje é confirmável (RF-102). | 1 | RF-102, RNF-047 |
| I12.04 | `SemFichasDeCuidadorRecebeListaVazia` | RP-15: quem não é Cuidador de nenhuma ficha ativa recebe 200 com uma lista vazia. | 1 | - |
| I12.05 | `DataInvalidaERecusada` | RNF-028: uma data com outro formato ou inexistente é recusada com 400 e o campo `data`. | 3 | RNF-028 |
| I12.06 | `SemSessaoERecusado` | RF-017: sem sessão, o pedido é recusado com 401. | 1 | RF-017 |

**I13 `NotificacoesApiTestes`** (8 métodos, 8 casos): Testes de integração das notificações de atraso das tomas (passos 2 e 3 do processo periódico), do envio dos e-mails de notificação e de OP-51 (F011, I047), com o SQL Server e um serviço de e-mail controlado. O processo periódico está desligado nos testes: os passos são chamados com o instante que cada teste precisa, e um teste corre o ciclo completo com o relógio real. As tomas ficam previstas para daqui a uma hora, depois do instante desde o qual a ficha está ativa. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/NotificacoesApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I13.01 | `LeveNotificaOsCuidadoresComAToleranciaExata` | RF-110 CA1 e CA3, RF-066: numa toma Leve, o passo 2 às T + 2 h ainda não notifica; um segundo depois, os dois Cuidadores (principal e outro) recebem uma notificação `TomaEmAtraso` com o e-mail registado; o passo 3 nunca notifica o Familiar e a toma continua `Pendente` na base de dados, com a marca do aviso. | 1 | RF-066, RF-110 |
| I13.02 | `GraveNotificaOsFamiliaresEOHistoricoMostraANotificacao` | RF-112, RF-113 CA1, RNF-043: numa toma Grave, os Cuidadores são notificados depois dos 30 minutos e o Familiar 30 minutos depois; repetir os passos não duplica. Os e-mails são enviados e o Familiar vê a notificação em OP-51, com a ficha, o medicamento, a hora prevista, a hora da notificação e o e-mail enviado (RF-140 CA1). | 1 | RF-112, RF-113, RF-140, RNF-043 |
| I13.03 | `TomaConfirmadaAntesDaDetecaoNaoGeraNotificacao` | RF-112 CA2 e RF-113 CA2: a toma confirmada pelo Cuidador (OP-33) antes da deteção não gera notificações; a outra toma, por confirmar, gera. | 1 | RF-112, RF-113 |
| I13.04 | `FalhaDoEmailFicaNaoEntregueNoHistorico` | RF-118 CA1, RF-119 CA1 e RF-141 CA1: com o serviço de e-mail sempre em falha, a notificação existe na mesma, o e-mail é tentado e reenviado três vezes de 5 em 5 minutos e o histórico mostra-o como `NaoEntregue`. | 1 | RF-118, RF-119, RF-141 |
| I13.05 | `HistoricoSoDoProprioComFiltrosEValidacao` | OP-51: cada Utilizador só vê as suas notificações (RF-140 CA3); `de` e `ate` filtram pelo dia de Lisboa da criação; `ate` anterior a `de` ou uma data inválida dão 400 com o campo; sem token dá 401. | 1 | RF-140 |
| I13.06 | `UtilizadorComConvitePendenteVeANotificacaoMesmoSemEmail` | US-I047-03, RF-041 e RF-141: um Utilizador só com um convite pendente, sem associação à ficha, consulta em OP-51 a notificação `ConviteRecebido`, sem medicamento nem hora prevista e com o e-mail ainda por enviar; depois de o e-mail falhar nas 3 tentativas de reenvio, a notificação continua no histórico, identificada como `NaoEntregue` (RF-119). | 1 | RF-041, RF-119, RF-141 |
| I13.07 | `FichaArquivadaNaoGeraNotificacoes` | RF-112 CA3 e RF-142 CA2: com a ficha arquivada (OP-12) os passos não criam notificações da toma. | 1 | RF-112, RF-142 |
| I13.08 | `ProcessoPeriodicoNotificaEEnviaOsEmails` | RNF-040 e RNF-042: o processo periódico registado no arranque, com o relógio real, cria num só ciclo as notificações em falta de uma toma Grave com mais de 60 minutos (Cuidadores e Familiar) e envia os respetivos e-mails. | 1 | RNF-040, RNF-042 |

**I14 `PapeisApiTestes`** (9 métodos, 11 casos): Testes de integração da promoção e da despromoção (F004, OP-23 e OP-24), com a API completa e o SQL Server. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/PapeisApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I14.01 | `FamiliarPromovidoPassaAEscreverNoPedidoSeguinte` | RF-049 CA1, RP-01: o Familiar promovido recebe 200 com o novo papel e pode editar a ficha logo no pedido seguinte. | 1 | RF-049 |
| I14.02 | `CuidadorDespromovidoPerdeAEscritaNoPedidoSeguinte` | RF-130 CA1, RP-01: o Cuidador despromovido fica Familiar na base de dados e perde a escrita no pedido seguinte (403). | 1 | RF-130 |
| I14.03 | `PrincipalNaoEDespromovido` | RF-133 e RF-134: o Cuidador principal não é despromovido; sozinho dá `FICHA_SEM_CUIDADOR` e, com outro Cuidador, `CUIDADOR_PRINCIPAL_SEM_SUBSTITUTO`. Nada muda na base de dados. | 2 | RF-133, RF-134 |
| I14.04 | `TitularDaFichaPropriaNaoEDespromovido` | RF-033 CA1: o titular da ficha própria não é despromovido, nem a pedido de outro Cuidador nem a pedido próprio (403). | 1 | RF-033 |
| I14.05 | `PessoaNaoAssociadaDa404` | D6: a pessoa indicada tem de estar associada à ficha (404 `PESSOA_NAO_ENCONTRADA`), tanto ao promover como ao despromover. | 2 | - |
| I14.06 | `PapelInvalidoDa400` | RNF-028: papel em falta, com um texto que não é um papel ou com um número, dá 400 `VALIDACAO_INVALIDA` com o campo `papel`. | 1 | RNF-028 |
| I14.07 | `FamiliarNaoAssociadoEFichaArquivadaSaoRecusados` | RNF-004, RF-048 e RP-13: o Familiar recebe 403 `ACESSO_PROIBIDO`, quem não está associado recebe 404 e, numa ficha arquivada, o Cuidador recebe 409 `FICHA_ARQUIVADA`. | 1 | RF-048, RNF-004 |
| I14.08 | `PapelAtualDa200SemAlteracoes` | D7: pedir o papel atual dá 200 sem alterações, também sobre o Cuidador principal. | 1 | - |
| I14.09 | `SemSessaoDa401` | RF-017: sem token, o pedido é recusado com 401. | 1 | RF-017 |

**I15 `PessoasApiTestes`** (5 métodos, 5 casos): Testes de integração das pessoas associadas a uma ficha (F004, OP-18), com a API completa e o SQL Server. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/PessoasApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I15.01 | `CuidadorEFamiliarVeemAsPessoasDaFicha` | OP-18, DP-03: o Cuidador e o Familiar recebem a mesma lista, com o nome, o e-mail e o papel de cada pessoa e o Cuidador principal primeiro. | 1 | - |
| I15.02 | `PrincipalProvisorioApareceComoProvisorio` | OP-18, RF-025: com um convite para Cuidador principal pendente, o criador aparece como Cuidador principal provisório e o convidado não aparece, porque ainda não está associado. | 1 | RF-025 |
| I15.03 | `NaoAssociadoEConvidadoPendenteRecebem404` | RNF-005, RP-02: quem não está associado, o Utilizador com convite pendente e quem pede uma ficha inexistente recebem 404. | 1 | RNF-005 |
| I15.04 | `FichaArquivadaContinuaAListarAsPessoas` | RP-13: numa ficha arquivada a lista de pessoas continua disponível. | 1 | - |
| I15.05 | `SemSessaoDa401` | RF-017: sem token, o pedido é recusado com 401. | 1 | RF-017 |

**I16 `RegistosApiTestes`** (9 métodos, 9 casos): Testes de integração de OP-46 a OP-49 (F009, I045): a API completa com o SQL Server. A ficha tem um Cuidador principal, outro Cuidador e um Familiar; há ainda um Utilizador sem associação. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/RegistosApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I16.01 | `RegistarOcorrenciaEConsultar` | OP-46 e OP-48 (RF-090, RF-092 a RF-094): o Cuidador principal regista; o outro Cuidador e o Familiar veem a ocorrência na consulta seguinte, com o autor e os dois instantes; quem não está associado recebe 404. | 1 | RF-090, RF-092, RF-093, RF-094 |
| I16.02 | `NotasSaoSoDosCuidadores` | OP-47 e OP-49 (RF-091, RF-093, RF-095, RP-15): um Cuidador que não é o principal regista a nota e o principal vê-a; o Familiar recebe 403 e quem não está associado recebe 404. | 1 | RF-091, RF-093, RF-095 |
| I16.03 | `SoUmCuidadorDaFichaRegista` | RF-090 CA3, RF-091 CA3 e RNF-005: o Familiar não regista (403) e quem não está associado recebe 404; nada é gravado. | 1 | RF-090, RF-091, RNF-005 |
| I16.04 | `DadosInvalidosSaoIndicadosPorCampo` | RF-090 CA2, RF-091 CA2 e RNF-028: os campos em falta ou inválidos, incluindo os da consulta, são indicados um a um. | 1 | RF-090, RF-091, RNF-028 |
| I16.05 | `FichaArquivadaSoPermiteConsultar` | RF-120 e RP-13: numa ficha arquivada não se registam ocorrências nem notas (409), mas as já registadas continuam visíveis. | 1 | RF-120 |
| I16.06 | `RegistosSaoImutaveis` | R-07, RF-090 CA4 e secção 4 da Documentação da API: alterar ou eliminar um registo responde 405 `REGISTO_IMUTAVEL`, e os gatilhos `TR_Ocorrencia_Imutavel` e `TR_Nota_Imutavel` recusam a eliminação na própria base de dados. | 1 | RF-090 |
| I16.07 | `OrdenaEFiltraPeloDiaDoRegistoEmHoraDeLisboa` | OP-48 (RF-094 CA1): da mais recente para a mais antiga pelo instante do registo; `de` e `ate` são dias de Lisboa. Às 23:30 UTC de 1 de julho já é dia 2 em Lisboa (UTC+1). | 1 | RF-094 |
| I16.08 | `OcorrenciaNotificaOsOutrosEANotaNao` | RF-115 CA1 e CA2 (parte na aplicação): a ocorrência do Cuidador principal cria uma notificação `NovaOcorrencia` para o outro Cuidador e para o Familiar, e nenhuma para o autor; a nota não cria notificações e não há e-mails registados. | 1 | RF-115 |
| I16.09 | `FalhaNaGravacaoNaoDeixaOcorrenciaNemNotificacoes` | RNF-046: se a base de dados recusar a gravação da ocorrência, a resposta é 500 e não fica a ocorrência nem nenhuma notificação. | 1 | RNF-046 |

**I17 `RemocaoAcessoApiTestes`** (7 métodos, 8 casos): Testes de integração da remoção de acesso (F004, OP-25), com a API completa e o SQL Server. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/RemocaoAcessoApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I17.01 | `FamiliarRemovidoDeixaDeVerAFicha` | RF-131 CA1 e CA2: o Familiar removido deixa de estar associado na base de dados, sai da lista de pessoas e, no pedido seguinte, recebe 404 ao abrir a ficha, como se ela não existisse. | 1 | RF-131 |
| I17.02 | `CuidadorSaiDaFichaPorIniciativaPropria` | Matriz de Permissões, secção 6: um Cuidador que não é o principal sai da ficha por iniciativa própria e deixa de a ver. | 1 | - |
| I17.03 | `FamiliarNaoSeRemoveASiProprio` | DP-01: o Familiar não se remove a si próprio (403 `ACESSO_PROIBIDO`) e continua associado. | 1 | - |
| I17.04 | `PrincipalNaoERemovido` | RF-133 CA1 e RF-134: o Cuidador principal não é removido, nem por si próprio; sozinho dá `FICHA_SEM_CUIDADOR` e, com outro Cuidador, `CUIDADOR_PRINCIPAL_SEM_SUBSTITUTO`. | 2 | RF-133, RF-134 |
| I17.05 | `TitularDaFichaPropriaNaoERemovido` | RF-033 CA2: o titular da ficha própria não é removido, nem a pedido de outro Cuidador nem a pedido próprio (403). | 1 | RF-033 |
| I17.06 | `PessoaNaoAssociadaNaoAssociadoEFichaArquivadaSaoRecusados` | D6, RNF-005, RP-13: pessoa não associada dá 404 `PESSOA_NAO_ENCONTRADA`; quem não está associado à ficha recebe 404 `FICHA_NAO_ENCONTRADA`; numa ficha arquivada a remoção é recusada com 409 `FICHA_ARQUIVADA`. | 1 | RNF-005 |
| I17.07 | `SemSessaoDa401` | RF-017: sem token, o pedido é recusado com 401. | 1 | RF-017 |

**I18 `RespostaConvitesApiTestes`** (10 métodos, 10 casos): Testes de integração da aceitação e da recusa de convites (F004, OP-21 e OP-22), com a API completa e o SQL Server. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/RespostaConvitesApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I18.01 | `FamiliarAceitaEFicaSoComConsulta` | RF-045 CA1, RF-048: o convidado aceita o convite para Familiar, recebe 200 com o seu papel, passa a consultar a ficha, não a edita e o convite fica aceite. | 1 | RF-045, RF-048 |
| I18.02 | `CuidadorAceitaEPassaAEscrever` | RF-045, RF-047: quem aceita um convite para Cuidador passa a poder editar a ficha. | 1 | RF-045, RF-047 |
| I18.03 | `RecusarNaoAssociaEPermiteNovoConvite` | RF-046 CA1 e RF-043 CA2: recusar dá 204, não associa o convidado e permite convidar de novo o mesmo e-mail para a mesma ficha. | 1 | RF-043, RF-046 |
| I18.04 | `ConviteDeOutroOuInexistenteDa404` | UC-14 E2, RP-09: um convite dirigido a outro e-mail, ou que não existe, dá 404 `CONVITE_NAO_ENCONTRADO` ao aceitar e ao recusar, e o convite continua pendente. | 1 | - |
| I18.05 | `ConviteJaRespondidoDa409` | UC-14 E1: um convite já aceite não é aceite nem recusado outra vez (409 `CONVITE_JA_RESPONDIDO`). | 1 | - |
| I18.06 | `PessoaJaAssociadaNaoAceita` | D4: um Familiar da ficha muda o e-mail da conta (OP-05) para o e-mail de um convite pendente dessa ficha; ao aceitar recebe 409 `PESSOA_JA_ASSOCIADA`, continua Familiar e o convite fica pendente. | 1 | - |
| I18.07 | `NaFichaArquivadaOConvidadoAceitaNaMesma` | UC-14 A5, RP-13: arquivada a ficha depois do convite, o convidado aceita na mesma (sem `FICHA_ARQUIVADA`). | 1 | - |
| I18.08 | `AceitarConviteParaPrincipalTransfereOPapel` | RF-025 e RF-026 CA1: com o convite para Cuidador principal da OP-09, quem cria a conta com esse e-mail vê o convite, aceita-o e passa a Cuidador principal; o criador provisório deixa de estar associado e recebe 404 na ficha. | 1 | RF-025, RF-026 |
| I18.09 | `RecusarConviteParaPrincipalMantemOCriadorEPermiteOutroConvite` | RF-027 CA1: recusar o convite para Cuidador principal mantém o criador como Cuidador principal, já não provisório, e o criador pode convidar outra pessoa para Cuidador principal (OP-19). | 1 | RF-027 |
| I18.10 | `SemSessaoDa401` | RF-017: sem token, aceitar e recusar são recusados com 401. | 1 | RF-017 |

**I19 `SaudeApiTestes`** (2 métodos, 2 casos): Testes de integração da configuração da solução (I029): arranque da API e ligação ao SQL Server. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/SaudeApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I19.01 | `SaudeRespondeSaudavelComBaseDeDados` | O endpoint de verificação do estado responde 200 sem sessão e confirma o acesso à base de dados. | 1 | - |
| I19.02 | `BaseDeDadosCriadaPelasMigracoesAceitaLigacoes` | A base de dados de testes foi criada pelas migrações e aceita ligações. | 1 | - |

**I20 `TomasApiTestes`** (8 métodos, 8 casos): Testes de integração de OP-32 a OP-34 (F006, I038): a API completa com o SQL Server e o relógio real. As tomas são criadas na base de dados com instantes relativos ao momento do teste; o tratamento é registado para daqui a 60 dias, para as tomas que gera não interferirem. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/TomasApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I20.01 | `ListarTomasDaFicha` | OP-32: o Cuidador e o Familiar listam as tomas do dia, com o JSON do contrato; o filtro pelo estado aplica-se. | 1 | RF-063, RF-065 |
| I20.02 | `ListarValidaOIntervaloEOAcesso` | OP-32: o intervalo é obrigatório e tem no máximo 31 dias; quem não está associado recebe 404 e sem token dá 401. | 1 | - |
| I20.03 | `ConfirmarToma` | OP-33: o Cuidador confirma (200 com quem e quando); a segunda confirmação dá 409 `TOMA_JA_CONFIRMADA` com a confirmação gravada; o Familiar recebe 403 e a toma inexistente 404. | 1 | RF-064, RF-065, RF-105 |
| I20.04 | `TomaDeAmanhaFicaForaDaJanela` | OP-33 e TR-13: a toma de amanhã ainda não pode ser confirmada (409 `TOMA_FORA_DA_JANELA`) e fica pendente. | 1 | RF-064 |
| I20.05 | `TomaComVinteEQuatroHorasNaoEConfirmavel` | OP-33 e TR-14: passadas 24 horas, a toma pendente já não pode ser confirmada (409 `TOMA_NAO_CONFIRMAVEL`). | 1 | RF-068 |
| I20.06 | `FichaArquivadaAindaConfirma` | OP-33 e RP-13: numa ficha arquivada, a toma passada ainda é confirmada (não dá `FICHA_ARQUIVADA`). | 1 | - |
| I20.07 | `ConfirmacoesEmParaleloDaoUma200EUma409` | RNF-044: dois Cuidadores a confirmar ao mesmo tempo dão uma resposta 200 e uma 409 `TOMA_JA_CONFIRMADA`. | 1 | RNF-044 |
| I20.08 | `CalendarioDeTomas` | OP-34: o calendário tem só as tomas pendentes das próximas 48 horas das fichas ativas em que é Cuidador; sem tomas, a lista é vazia. | 1 | RF-061, RF-069 |

**I21 `TransferenciaPrincipalApiTestes`** (9 métodos, 9 casos): Testes de integração da transferência do papel de Cuidador principal (F004, OP-26), com a API completa e o SQL Server. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/TransferenciaPrincipalApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I21.01 | `TransferenciaDeixaUmUnicoPrincipal` | RF-132 CA1, RNF-046: o segundo Cuidador passa a Cuidador principal, o anterior continua Cuidador e a base de dados fica com um único Cuidador principal (índice `UQ_Associacao_Principal`). | 1 | RF-132, RNF-046 |
| I21.02 | `TransferenciaFuncionaNosDoisSentidos` | RF-132, `UQ_Associacao_Principal`: transferir o papel para B e de volta para A funciona nos dois sentidos. O EF Core ordena os `UPDATE` pela chave da associação, por isso pelo menos uma das transferências grava a linha do novo principal antes da do anterior; o índice único filtrado não pode recusar nenhuma delas. | 1 | RF-132 |
| I21.03 | `CuidadorQueNaoEOPrincipalTransfereEDespromoveOAnterior` | DP-02, RP-08, RF-133 CA2: um Cuidador que não é o principal transfere o papel para si próprio e, a seguir, despromove o anterior Cuidador principal. | 1 | RF-133 |
| I21.04 | `NaFichaPropriaOPapelNaoSeTransfere` | RF-034, UC-16 E1: na ficha própria o papel não se transfere (403), nem para outro Cuidador nem a pedido do titular. | 1 | RF-034 |
| I21.05 | `DestinatarioQueNaoECuidadorDa409` | UC-16 E2, D6: o destinatário Familiar ou não associado à ficha dá 409 `DESTINATARIO_NAO_E_CUIDADOR` e nada muda. | 1 | - |
| I21.06 | `TransferirParaOPrincipalAtualDa200SemAlteracoes` | D7: transferir o papel para quem já é Cuidador principal dá 200 sem alterações. | 1 | - |
| I21.07 | `DestinatarioEmFaltaOuInvalidoDa400` | RNF-028: o destinatário em falta ou que não é um identificador dá 400 `VALIDACAO_INVALIDA` com o campo `utilizadorId`. | 1 | RNF-028 |
| I21.08 | `FamiliarNaoAssociadoEFichaArquivadaSaoRecusados` | RNF-004, RNF-005, RP-13: o Familiar recebe 403, quem não está associado recebe 404 e, numa ficha arquivada, 409. | 1 | RNF-004, RNF-005 |
| I21.09 | `SemSessaoDa401` | RF-017: sem token, o pedido é recusado com 401. | 1 | RF-017 |

**I22 `TratamentosApiTestes`** (7 métodos, 7 casos): Testes de integração de OP-27 a OP-31 (F005, I037): a API completa com o SQL Server e a pasta de documentos da execução. As datas são futuras, para o número de tomas geradas não depender do dia em que os testes correm. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Api/TratamentosApiTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I22.01 | `RegistarConsultarEListar` | OP-27, OP-28 e OP-29: o Cuidador regista, as tomas ficam geradas e o Familiar consulta; quem não está associado recebe 404 e o filtro pelo estado é aplicado. | 1 | - |
| I22.02 | `RegistarComReceita` | OP-27 com receita (RF-052): o ficheiro é guardado, fica associado ao tratamento e obtém-se com OP-17. | 1 | RF-052 |
| I22.03 | `SoUmCuidadorRegistaAlteraETermina` | RF-050 CA3, RF-055 CA2 e RNF-005: só um Cuidador regista, altera e termina; o Familiar recebe 403 e quem não está associado recebe 404. | 1 | RF-050, RF-055, RNF-005 |
| I22.04 | `DadosInvalidosSaoIndicadosPorCampo` | RF-050 CA2, CA4 e RF-051: os campos em falta ou inválidos são indicados um a um (RNF-028). | 1 | RF-050, RF-051, RNF-028 |
| I22.05 | `AtualizarETerminar` | OP-30 e OP-31 (RF-055, RF-056, RF-060): alterar regenera as tomas futuras com os dados novos; terminar apaga-as e um tratamento terminado já não muda. | 1 | RF-055, RF-056, RF-060 |
| I22.06 | `FichaArquivadaSoPermiteTerminar` | RF-120 e RF-122: numa ficha arquivada não se regista nem se altera medicação (409), mas o tratamento pode ser terminado; ao arquivar, as tomas futuras são apagadas. | 1 | RF-120, RF-122 |
| I22.07 | `ReceitaQueNaoEGuardadaNaoRegistaOTratamento` | RF-053 CA1: se a receita não puder ser guardada, a resposta é 503 e o tratamento não é registado. | 1 | RF-053 |

#### 7.2.2 Dados

**I23 `ModeloDadosTestes`** (11 métodos, 11 casos): Testes de integração do modelo de dados (I030): criação da base de dados pelas migrações, dados predefinidos, unicidade, integridade referencial, cascatas e gatilhos (modelo de dados, secções 7 e 13). Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Dados/ModeloDadosTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I23.01 | `MigracoesDeixamAsDezanoveTabelas` | As migrações deixam as 19 tabelas do modelo de dados v02: a migração RetirarTarefasRecorrentes remove as quatro tabelas da F007. | 1 | - |
| I23.02 | `MigracoesDeixamOsTresGatilhos` | As migrações deixam os três gatilhos de imutabilidade do histórico; o gatilho da F007 é removido. | 1 | - |
| I23.03 | `ListasFechadasTemOsValoresPredefinidos` | As listas fechadas de necessidades têm os valores predefinidos da secção 8 do modelo de dados. | 1 | - |
| I23.04 | `EmailDuplicadoComMaiusculasDiferentesERecusado` | O e-mail é único sem distinção de maiúsculas (UQ_Utilizador_Email com collation CI_AI). | 1 | - |
| I23.05 | `SegundaFichaPropriaERecusada` | Um Utilizador tem no máximo uma ficha própria (UQ_Ficha_UtilizadorProprio). | 1 | - |
| I23.06 | `SegundoCuidadorPrincipalERecusado` | Uma ficha tem no máximo um Cuidador principal (UQ_Associacao_Principal) e o principal é Cuidador (CK_Associacao_Principal). | 1 | - |
| I23.07 | `ConvitePendenteDuplicadoERecusadoMasPermitidoDepoisDeRecusado` | Só existe um convite pendente por e-mail e ficha; depois de recusado pode ser criado outro (UQ_Convite_Pendente). | 1 | - |
| I23.08 | `AssociacaoAUtilizadorInexistenteERecusada` | Uma associação a um Utilizador inexistente é recusada (FK_Associacao_UtilizadorId). | 1 | - |
| I23.09 | `EliminarUtilizadorReferenciadoERecusado` | Não é possível eliminar um Utilizador que criou uma ficha (FK_Ficha_CriadaPorId, NO ACTION). | 1 | - |
| I23.10 | `EliminarFichaRemoveAssociacoesEConvitesPorCascata` | Eliminar uma ficha remove as associações e os convites por cascata (RF-123). | 1 | RF-123 |
| I23.11 | `OcorrenciaNaoPodeSerAlterada` | Uma ocorrência registada não pode ser alterada (TR_Ocorrencia_Imutavel). | 1 | - |

**I24 `RetiradaF007DadosTestes`** (1 método, 1 caso): Testes de integração da migração `RetirarTarefasRecorrentes` sobre dados existentes (I043): numa base de dados só com a migração inicial e os dados fictícios do script `02`, a retirada da F007 não apaga contas, fichas, necessidades, associações, convites nem notificações. Usa uma base de dados própria, porque a da coleção já tem todas as migrações. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Dados/RetiradaF007DadosTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I24.01 | `RetirarTarefasRecorrentesNaoApagaOsDadosFicticios` | As linhas de cada tabela com dados são as mesmas antes e depois da migração `RetirarTarefasRecorrentes`, incluindo uma notificação `ConviteRecebido`, que tem de continuar válida nas restrições recriadas pela migração. | 1 | - |

**I25 `RetiradaF007Testes`** (3 métodos, 3 casos): Testes de integração da retirada da F007 (I043): depois de aplicadas todas as migrações não resta nenhuma tabela, gatilho nem valor de restrição das tarefas recorrentes (modelo de dados v02, secção 15). Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Dados/RetiradaF007Testes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I25.01 | `TabelasEGatilhoDaF007NaoExistem` | As quatro tabelas e o gatilho `TR_ExecucaoTarefa_Historico` da F007 não existem. | 1 | - |
| I25.02 | `FicamDezanoveTabelasETresGatilhos` | Ficam 19 tabelas de domínio (sem contar `__EFMigrationsHistory`) e 3 gatilhos. | 1 | - |
| I25.03 | `RestricoesDasNotificacoesRecusamOsValoresDaF007` | `CK_Notificacao_Tipo` recusa o tipo `TarefaNaoConfirmada` e `CK_Notificacao_Referencia` recusa a referência `ExecucaoTarefa`; uma notificação válida com os mesmos dados é aceite. | 1 | - |

**I26 `TerminoTratamentosTestes`** (1 método, 1 caso): Testes de integração do passo 4 do processo periódico sobre o SQL Server: término dos tratamentos cuja data de fim foi ultrapassada (RF-056; Modelo de Classes do Backend, secção 8.1). Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Dados/TerminoTratamentosTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I26.01 | `TerminarConcluidosGravaSoOsTratamentosComDataDeFimUltrapassada` | Só o tratamento em curso com data de fim anterior a hoje (Lisboa) é terminado e gravado; o que acaba hoje e o contínuo continuam em curso. | 1 | RF-056 |

**I27 `UnidadeDeTrabalhoTestes`** (2 métodos, 2 casos): Testes de integração da unidade de trabalho sobre o SQL Server (RNF-046, AD-10). Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Dados/UnidadeDeTrabalhoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I27.01 | `OperacaoBemSucedidaFicaGravada` | Uma operação bem-sucedida fica gravada e as compensações não são executadas. | 1 | RNF-046 |
| I27.02 | `OperacaoFalhadaNaoGravaNadaEExecutaCompensacoes` | Uma operação que falha não grava nada, mesmo o que já tinha sido gravado dentro da transação, executa as compensações registadas (mesmo que uma delas falhe) e volta a lançar o erro original. | 1 | RNF-046 |

**I28 `UnidadeDeTrabalhoUnicidadeAssociacaoTestes`** (2 métodos, 2 casos): Testes de integração da conversão das violações dos índices únicos da associação a uma ficha pela unidade de trabalho, sobre o SQL Server (RF-043, RNF-046; D8). Cada teste força a violação, sem depender do momento em que chegam pedidos simultâneos. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Dados/UnidadeDeTrabalhoUnicidadeAssociacaoTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I28.01 | `ConvitePendenteRepetidoDaErroDeNegocio` | RF-043: um segundo convite pendente para o mesmo e-mail e a mesma ficha, recusado pelo índice `UQ_Convite_Pendente`, dá `CONVITE_PENDENTE_EXISTE` e não grava nada. | 1 | RF-043 |
| I28.02 | `AssociacaoRepetidaDaErroDeNegocio` | D8: uma segunda associação do mesmo Utilizador à mesma ficha, recusada pelo índice `UQ_Associacao_FichaUtilizador`, dá `PESSOA_JA_ASSOCIADA` e não grava nada. | 1 | RF-043, RNF-046 |

**I29 `UnidadeDeTrabalhoUnicidadeTestes`** (3 métodos, 4 casos): Testes de integração da conversão das violações de unicidade pela unidade de trabalho, sobre o SQL Server (RF-031, RNF-046). Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Dados/UnidadeDeTrabalhoUnicidadeTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I29.01 | `ViolacaoDaFichaPropriaDaErroDeNegocio` | Uma segunda ficha própria recusada pelo índice `UQ_Ficha_UtilizadorProprio` dá `FICHA_PROPRIA_JA_EXISTE`, não grava nada e executa as compensações, quer a gravação seja feita dentro da operação, quer na confirmação. | 2 | RF-031, RNF-046 |
| I29.02 | `OutroIndiceUnicoNaoEConvertido` | Outro índice único (e-mail repetido) continua a sair como DbUpdateException: só os índices conhecidos são convertidos. | 1 | RF-031, RNF-046 |
| I29.03 | `FalhaDeChaveEstrangeiraNaoEConvertida` | Uma falha de integridade referencial continua a sair como DbUpdateException (sem regressão). | 1 | RF-031, RNF-046 |

#### 7.2.3 Segurança

**I30 `AutenticacaoJwtTestes`** (2 métodos, 4 casos): Testes de integração da autenticação JWT configurada na API (AD-02, RNF-054): os parâmetros de validação são obtidos da API arrancada, com a chave lida da configuração não versionada. Ficheiro: `backend/tests/DoseCerta.IntegrationTests/Seguranca/AutenticacaoJwtTestes.cs`.

| ID | Método | Condição testada e resultado esperado | Casos | Requisitos |
|---|---|---|---|---|
| I30.01 | `TokenValidoEAceite` | Um token assinado com a chave configurada, do emissor e público certos e dentro da validade, é aceite. | 1 | RNF-054 |
| I30.02 | `TokenInvalidoERecusado` | Um token assinado com outra chave, de outro emissor ou já expirado é recusado. | 3 | RNF-054 |

### 7.3 Testes de sistema (collection Postman)

A collection `m3-collection-postman-v02.json` tem uma pasta por funcionalidade, executada por ordem, porque cada pasta usa os dados criados pelas anteriores. Cada pedido verifica pelo menos o código HTTP e, nos erros, o código de erro da documentação da API. O detalhe de cada pedido está na própria collection (descrição de cada pasta e scripts de teste).

| ID | Pasta | Pedidos | Verificações (`pm.test`) | Operações |
|---|---|---|---|---|
| S01 | F001 Autenticação e conta | 16 | 28 | OP-01 a OP-06 |
| S02 | F002 Fichas de utentes | 19 | 36 | OP-07 a OP-17 |
| S03 | F003 Ficha própria | 3 | 6 | OP-09 (`propria=true`) |
| S04 | F004 Associação | 18 | 32 | OP-18 a OP-26 |
| S05 | F005 Registo de medicação | 17 | 36 | OP-27 a OP-31 |
| S06 | F006 Horários e confirmação de toma | 12 | 30 | OP-32 a OP-34 |
| S07 | F008 Compromissos e agenda | 16 | 38 | OP-41 a OP-45 e o `405` da confirmação de compromissos |
| S08 | F009 Ocorrências e notas partilhadas | 15 | 34 | OP-46 a OP-49 e os `405` da alteração e eliminação de registos |
| S09 | F010 Lista diária | 7 | 19 | OP-50 |
| S10 | F011 Notificações | 5 | 18 | OP-51 |
| | **Total** | **128** | **277** | |

Limitações da collection, que são cobertas pelos testes de integração: não testa `TOMA_NAO_CONFIRMAVEL` nem as notificações de atraso, que dependem do processo periódico e de esperar pelas horas-limite (cobertas por I13 e I20), e as tomas e o compromisso do dia usam as 23:57 a 23:59 de Lisboa, pelo que não deve ser executada nos últimos minutos do dia.

## 8. Rastreabilidade

### 8.1 Requisitos funcionais

Os 96 requisitos funcionais ativos da secção 7 da documentação da API. Os requisitos da F007 (RF-070 a RF-078, RF-103 e RF-114) estão descontinuados e não têm casos de teste. A coluna Funcionalidade segue os intervalos de requisitos de cada funcionalidade (plano, secção 4.1).

| Requisito | Título | Funcionalidade | Casos unitários | Casos de integração | Situação |
|---|---|---|---|---|---|
| RF-010 | Criar conta | F001 | U18.01, U18.02 | - | Coberto |
| RF-011 | Impedir conta com e-mail já registado | F001 | U18.03, U46.04 | - | Coberto |
| RF-012 | Iniciar sessão | F001 | U18.05, U18.06 | I04.04 | Coberto |
| RF-013 | Consultar dados da conta | F001 | U19.01 | - | Coberto |
| RF-014 | Atualizar nome e e-mail da conta | F001 | U19.03, U19.05 | - | Coberto |
| RF-015 | Alterar palavra-passe | F001 | U19.07, U19.08, U19.09 | - | Coberto |
| RF-016 | Terminar sessão | F001 | U18.08, U41.01, U41.02, U41.03, U41.04, U41.05 | I04.08 | Coberto |
| RF-017 | Exigir sessão iniciada | F001 | - | I04.05, I05.08, I06.04, I12.06, I14.09, I15.05, I17.07, I18.10, I21.09 | Coberto |
| RF-020 | Criar ficha de terceiro | F002 | U27.01, U27.05 | - | Coberto |
| RF-021 | Registar necessidades específicas | F002 | U10.08, U27.07 | I11.01 | Coberto |
| RF-022 | Carregar documentos na ficha | F002 | U23.01, U23.02, U23.03, U23.05, U23.06, U27.09 | I07.01 | Coberto |
| RF-023 | Recusar a operação da ficha quando o documento não é guardado | F002 | U23.01, U23.03, U23.05, U23.07, U27.10, U27.16 | I07.05 | Coberto |
| RF-024 | Definir o criador como Cuidador principal | F002 | U10.01, U27.01, U27.08 | - | Coberto |
| RF-025 | Convidar outra pessoa para Cuidador principal | F002 | U06.03, U06.04, U06.06, U06.07, U06.08, U06.09, U10.02, U21.07, U21.08, U21.09, U21.13, U27.02, U32.01 | I05.05, I11.03, I15.02, I18.08 | Coberto |
| RF-026 | Transferir o papel de Cuidador principal quando o convite é aceite | F002 | U06.01, U06.04, U06.06, U06.07, U06.08, U06.09, U20.03, U20.04, U20.05, U20.06, U20.07 | I18.08 | Coberto |
| RF-027 | Manter o criador como Cuidador principal quando o convite é recusado | F002 | U06.02, U06.04, U06.06, U06.07, U06.08, U06.09, U20.03, U20.04, U20.05, U20.06, U20.08 | I18.09 | Coberto |
| RF-028 | Consultar ficha | F002 | U23.10, U26.01, U27.13 | I07.03, I11.07 | Coberto |
| RF-029 | Editar ficha | F002 | U10.08, U27.14, U27.15 | I11.08 | Coberto |
| RF-030 | Criar ficha própria | F003 | U27.03 | I10.01, I10.03, I11.04 | Coberto |
| RF-031 | Limitar a uma ficha própria por Utilizador | F003 | U27.04, U45.01, U45.02, U45.03, U45.04 | I10.02, I10.03, I10.04, I11.04, I29.01, I29.02, I29.03 | Coberto |
| RF-032 | Aplicar à ficha própria as regras das restantes fichas | F003 | U07.07, U09.07, U21.10 | I05.06, I10.03, I10.05, I10.06 | Coberto |
| RF-033 | Proteger o titular da ficha própria | F003 | U07.06, U08.01, U08.02, U08.03, U09.06, U37.01 | I14.04, I17.05 | Coberto |
| RF-034 | Manter o titular como Cuidador principal da ficha própria | F003 | U06.04, U06.05, U06.06, U06.07, U06.08, U06.09, U08.01, U08.05, U10.04, U11.02, U11.03, U11.04, U11.05, U11.07, U21.10, U37.01 | I05.06, I21.04 | Coberto |
| RF-040 | Convidar pessoa para uma ficha | F004 | U21.01, U21.02, U21.07, U21.13 | I05.01, I05.02 | Coberto |
| RF-041 | Enviar notificação e e-mail de convite | F004 | U21.03, U21.04, U22.01, U22.02, U25.01 | I08.01, I08.02, I09.02, I13.06 | Coberto |
| RF-042 | Disponibilizar convites na área de convites | F004 | U03.03, U03.04, U21.12 | I06.01 | Coberto |
| RF-043 | Impedir convite pendente duplicado | F004 | U03.03, U21.05, U21.06, U21.07, U21.13, U44.01, U44.02, U44.03 | I03.01, I03.02, I03.03, I05.03, I08.03, I18.03, I28.01, I28.02 | Coberto |
| RF-044 | Apresentar convites pendentes após o registo | F004 | U03.03, U03.04, U18.04, U21.12 | I06.02 | Coberto |
| RF-045 | Aceitar convite | F004 | U03.01, U03.03, U20.01, U20.03, U20.04, U20.05, U20.06 | I18.01, I18.02 | Coberto |
| RF-046 | Recusar convite | F004 | U03.02, U03.03, U20.02, U20.03, U20.04, U20.05, U20.06 | I06.03, I18.03 | Coberto |
| RF-047 | Conceder ao Cuidador acesso completo à ficha | F004 | - | I18.02 | Coberto |
| RF-048 | Restringir o Familiar a consulta | F004 | U10.05, U16.05, U21.11, U23.09, U26.03, U32.05 | I01.02, I05.07, I14.07, I18.01 | Coberto |
| RF-049 | Promover Familiar a Cuidador | F004 | U07.01, U07.03, U07.08, U32.03 | I14.01 | Coberto |
| RF-050 | Registar medicação | F005 | U15.02, U15.06, U15.07, U35.01, U35.03, U35.05, U35.06 | I22.03, I22.04 | Coberto |
| RF-051 | Atribuir grau de importância à medicação | F005 | U35.03, U35.05 | I22.04 | Coberto |
| RF-052 | Carregar receita no tratamento | F005 | U15.08, U35.03, U35.04 | I22.02 | Coberto |
| RF-053 | Recusar a operação do tratamento quando a receita não é guardada | F005 | U35.03, U35.07, U35.10 | I22.07 | Coberto |
| RF-054 | Consultar medicação da ficha | F005 | U35.03, U35.14 | - | Coberto |
| RF-055 | Atualizar tratamento em curso | F005 | U15.02, U15.04, U15.05, U15.07, U35.03, U35.08, U35.09 | I22.03, I22.05 | Coberto |
| RF-056 | Terminar tratamento antecipadamente | F005 | U15.02, U15.04, U15.07, U35.03, U35.11, U35.12, U41.01, U41.02, U41.03, U41.04, U41.05 | I22.05, I26.01 | Coberto |
| RF-057 | Preservar histórico de tratamentos terminados | F005 | U35.03, U35.14 | - | Coberto |
| RF-060 | Gerar as tomas previstas de uma medicação | F006 | U15.01, U15.02, U15.07, U34.02, U34.07, U34.08, U34.10, U34.13, U34.14, U34.16, U35.01, U35.02, U35.03, U35.08, U35.11, U41.01, U41.03, U41.04, U41.05 | I22.05 | Coberto |
| RF-061 | Sincronizar o calendário de tomas com a aplicação móvel | F006 | U34.02, U34.07, U34.08, U34.10, U34.12, U34.13, U34.16 | I20.08 | Coberto |
| RF-062 | Emitir o aviso local da toma prevista | F006 | U34.02, U34.07, U34.08, U34.10, U34.12, U34.13, U34.16 | - | Coberto no backend (dados do aviso em OP-34); a emissão do aviso local é da aplicação mobile e é testada em m4 |
| RF-063 | Apresentar o calendário de tomas de uma ficha | F006 | U34.02, U34.07, U34.08, U34.09, U34.10, U34.13, U34.16 | I20.01 | Coberto |
| RF-064 | Confirmar uma toma | F006 | U28.06, U34.01, U34.02, U34.07, U34.08, U34.10, U34.13, U34.16 | I20.03, I20.04 | Coberto |
| RF-065 | Registar o autor e o momento da confirmação de uma toma | F006 | U12.02, U14.01, U34.02, U34.07, U34.08, U34.10, U34.13, U34.16 | I20.01, I20.03 | Coberto |
| RF-066 | Atribuir o estado "pendente" às tomas por confirmar | F006 | U12.01, U12.02, U13.04, U14.03, U33.01, U34.02, U34.07, U34.08, U34.10, U34.13, U34.16 | I13.01 | Coberto |
| RF-067 | Marcar como atrasada a toma confirmada fora da tolerância | F006 | U12.02, U14.03, U14.04, U34.02, U34.07, U34.08, U34.10, U34.13, U34.16 | - | Coberto |
| RF-068 | Marcar como não confirmada a toma não confirmada em 24 horas | F006 | U12.02, U12.03, U28.06, U34.02, U34.07, U34.08, U34.10, U34.13, U34.16, U41.01, U41.03, U41.04, U41.05 | I20.05 | Coberto |
| RF-069 | Suspender a geração de tomas numa ficha arquivada | F006 | U34.02, U34.07, U34.08, U34.10, U34.13, U34.15, U34.16 | I20.08 | Coberto |
| RF-080 | Registar um compromisso de saúde | F008 | U02.05, U02.06, U17.01, U17.02, U17.04, U17.06, U17.07, U17.09 | I02.02 | Coberto |
| RF-081 | Editar um compromisso de saúde | F008 | U02.04, U02.05, U02.06, U17.04, U17.06, U17.07, U17.09 | - | Coberto |
| RF-082 | Remover um compromisso de saúde | F008 | U17.04, U17.06, U17.07, U17.09, U17.10 | - | Coberto |
| RF-083 | Reunir na agenda os eventos de todas as fichas do Utilizador | F008 | U16.01, U16.02, U16.03, U16.04, U16.06, U16.08 | I01.01, I01.03 | Coberto |
| RF-084 | Apresentar a agenda em vista diária | F008 | U16.01, U16.02, U16.04 | I01.01 | Coberto |
| RF-085 | Apresentar a agenda em vista semanal | F008 | U01.10, U01.11, U16.01, U16.02, U16.07 | I01.01, I01.03 | Coberto |
| RF-086 | Apresentar a agenda em vista mensal | F008 | U01.12, U16.01, U16.02, U16.08 | I01.01 | Coberto |
| RF-087 | Impedir a confirmação de compromissos | F008 | U16.01, U16.02, U16.05, U28.07, U28.10 | I01.01, I01.02, I02.05 | Coberto |
| RF-088 | Impedir a alteração de tomas a partir da agenda | F008 | U16.01, U16.02 | I01.01 | Coberto no backend (OP-45 só de leitura; as tomas só se alteram em OP-30); a interface é testada em m4 |
| RF-089 | Identificar a ficha de cada evento da agenda | F008 | U16.01, U16.02, U16.04 | I01.01 | Coberto |
| RF-090 | Registar uma ocorrência | F009 | U31.01, U31.06, U31.07, U31.11, U38.01 | I16.01, I16.03, I16.04, I16.06 | Coberto |
| RF-091 | Registar uma nota dirigida aos Cuidadores | F009 | U31.02, U31.06, U31.07, U31.11, U38.01 | I16.02, I16.03, I16.04 | Coberto |
| RF-092 | Registar o autor e o momento de ocorrências e notas | F009 | U31.01, U31.02, U31.11 | I16.01 | Coberto |
| RF-093 | Disponibilizar ocorrências e notas aos Cuidadores da ficha | F009 | U31.10, U31.11 | I16.01, I16.02 | Coberto |
| RF-094 | Consultar o histórico de ocorrências de uma ficha | F009 | U31.09, U31.11 | I16.01, I16.07 | Coberto |
| RF-095 | Consultar as notas de uma ficha | F009 | U31.10, U31.11 | I16.02 | Coberto |
| RF-100 | Apresentar a lista diária consolidada | F010 | U26.06, U26.07, U26.08, U28.01, U28.02, U28.03, U28.04, U28.07, U28.10 | I12.01, I12.02 | Coberto |
| RF-101 | Identificar a ficha de cada item da lista diária | F010 | U28.01, U28.07, U28.10 | I12.01 | Coberto |
| RF-102 | Confirmar uma toma a partir da lista diária | F010 | - | I12.03 | Coberto |
| RF-104 | Refletir as confirmações nas listas dos restantes Cuidadores | F010 | U28.05, U28.07, U28.10 | I12.01 | Coberto |
| RF-105 | Impedir a confirmação repetida de um item | F010 | U34.04 | I20.03 | Coberto |
| RF-110 | Notificar Cuidadores de toma Leve não confirmada | F011 | U13.01, U13.03, U13.05, U13.06, U13.07, U33.01 | I13.01 | Coberto |
| RF-111 | Notificar Cuidadores de toma Moderado não confirmada | F011 | U13.01, U13.03, U13.05, U13.07, U33.02 | - | Coberto |
| RF-112 | Notificar Cuidadores de toma Grave não confirmada | F011 | U13.01, U13.05, U13.07, U33.02, U33.03, U33.04, U33.07 | I13.02, I13.03, I13.07 | Coberto |
| RF-113 | Notificar Familiares de toma Grave com atraso prolongado | F011 | U13.02, U13.05, U13.06, U13.07, U29.01, U33.03, U33.04, U33.06 | I13.02, I13.03 | Coberto |
| RF-115 | Notificar o registo de uma nova ocorrência | F009 | U31.03, U31.04 | I16.08 | Coberto |
| RF-116 | Definir a antecedência de aviso de um compromisso | F008 | U02.01, U02.05, U02.06, U17.03, U17.04, U17.06, U17.07, U17.09 | I02.02 | Coberto |
| RF-117 | Notificar antes de um compromisso | F008 | U02.02, U02.03, U02.04, U02.05, U02.06, U17.04, U17.06, U17.07, U17.08, U17.09 | - | **Parcial**: a antecedência é validada e guardada, mas a notificação `ProximoCompromisso` não é criada (secção 9, L-01) |
| RF-118 | Reenviar o e-mail de notificação após falha | F011 | U05.05, U05.06, U24.02, U24.03, U24.05, U25.04 | I13.04 | Coberto |
| RF-119 | Identificar no histórico o e-mail não entregue | F011 | U05.06, U24.02, U24.05, U25.04, U29.01, U29.03 | I13.04, I13.06 | Coberto |
| RF-120 | Arquivar ficha | F002 | U10.06, U27.15, U27.17, U27.18, U31.08, U35.06, U35.13 | I02.04, I11.08, I11.09, I16.05, I22.06 | Coberto |
| RF-121 | Reativar ficha | F002 | U10.06, U27.19 | I11.09 | Coberto |
| RF-122 | Suspender tarefas, avisos e notificações de fichas arquivadas | F002 | U27.17, U27.20 | I11.09, I22.06 | Coberto |
| RF-123 | Eliminar ficha sem registos | F002 | U10.09, U23.13, U27.21, U27.22 | I07.08, I08.04, I11.10, I11.11, I23.10 | Coberto |
| RF-124 | Informar falha ao consultar documento | F002 | U23.01, U23.03, U23.05, U23.12 | I07.06 | Coberto |
| RF-130 | Despromover Cuidador a Familiar | F004 | U07.02, U07.03, U07.07, U07.08, U32.03 | I14.02 | Coberto |
| RF-131 | Remover acesso à ficha | F004 | U09.01, U09.02, U09.03, U09.07, U32.06 | I17.01 | Coberto |
| RF-132 | Transferir o papel de Cuidador principal | F004 | U10.03, U11.01, U11.02, U11.04, U11.05, U11.07 | I03.02, I03.03, I21.01, I21.02 | Coberto |
| RF-133 | Exigir substituto antes de despromover ou remover o Cuidador principal | F004 | U07.03, U07.05, U07.08, U08.04, U09.03, U09.04, U09.05, U11.02, U11.04, U11.05, U11.06, U11.07 | I14.03, I17.04, I21.03 | Coberto |
| RF-134 | Garantir que a ficha tem sempre um Cuidador | F004 | U07.03, U07.04, U07.08, U09.03, U09.04 | I14.03, I17.04 | Coberto |
| RF-135 | Manter o convite quando o e-mail não é entregue | F004 | U25.02, U25.04 | I09.01, I09.03 | Coberto |
| RF-136 | Reenviar e-mail de convite em caso de falha | F004 | U05.03, U05.04, U05.06, U25.02, U25.03, U25.04, U41.01, U41.03, U41.04, U41.05 | I09.01, I09.03 | Coberto |
| RF-140 | Consultar o histórico de notificações recebidas (F011) | F011 | U29.01, U29.03 | I13.02, I13.05 | Coberto |
| RF-141 | Criar a notificação na aplicação independentemente do e-mail (F011) | F011 | U24.01, U24.02, U24.04, U24.05 | I13.04, I13.06 | Coberto |
| RF-142 | Suspender notificações de ficha arquivada (F011) | F011 | U29.01, U29.02, U33.07 | I13.07 | Coberto |

### 8.2 Requisitos não funcionais com efeito no contrato da API

Os 33 requisitos não funcionais que a documentação da API reflete no contrato (secção 7). Os restantes requisitos não funcionais dizem respeito aos frontends ou à infraestrutura e não são objeto dos testes do backend (plano, secção 4.2).

| Requisito | Título | Casos de teste | Verificação |
|---|---|---|---|
| RNF-001 | Proteção das palavras-passe | U18.01, U47.01, U47.02, U47.03, U47.04, U47.05, I04.01 | Testes automatizados |
| RNF-002 | Comunicação cifrada | - | Sem caso automatizado no backend: o HTTPS é imposto pelo proxy do ambiente de demonstração (redirecionamento 308). Verificado no relatório de evidências de CI/CD (I050) e na execução da collection contra o URL público |
| RNF-003 | Requisitos mínimos da palavra-passe | U19.08 | Testes automatizados |
| RNF-004 | Autorização verificada no servidor | U32.05, I14.07, I21.08 | Testes automatizados |
| RNF-005 | Isolamento dos dados das fichas | U16.06, U17.05, U26.02, U26.09, U27.12, U31.07, U32.02, U34.03, U34.11, U35.06, I02.03, I05.07, I10.06, I11.06, I11.07, I15.03, I16.03, I17.06, I21.08, I22.03 | Testes automatizados |
| RNF-006 | Armazenamento local dos documentos | U42.01, U42.02, U42.04, I07.01 | Testes automatizados |
| RNF-007 | Acesso restrito aos documentos | U23.01, U23.03, U23.05, U23.11, U42.01, U42.02, U42.03, U42.04, I07.03 | Testes automatizados |
| RNF-008 | Formatos e tamanho dos documentos | U23.01, U23.03, U23.04, U23.05 | Testes automatizados |
| RNF-009 | Validade da sessão | U47.01, U47.02, U47.03, U47.04, U47.05, I04.03 | Testes automatizados |
| RNF-010 | Tempo de carregamento da lista diária | - | Medição de desempenho (plano, secção 9): tempo de OP-50 na execução da collection contra o URL público |
| RNF-011 | Tempo de resposta da confirmação de tomas | - | Medição de desempenho: tempo de OP-33 na execução da collection contra o URL público |
| RNF-012 | Tempo de carregamento da agenda | - | Medição de desempenho: tempo de OP-45 na execução da collection contra o URL público |
| RNF-013 | Tempo de autenticação | - | Medição de desempenho: tempo de OP-02 na execução da collection contra o URL público |
| RNF-014 | Tempo de carregamento de documentos | - | Medição de desempenho: carregamento de um documento de 5 MB em OP-15 contra o URL público |
| RNF-015 | Capacidade de utilizadores simultâneos | - | Não verificado em m3 (sem teste de carga); risco R-05 do plano |
| RNF-017 | Tempo de sincronização do calendário de tomas | - | Medição de desempenho: tempo de OP-34 na execução da collection contra o URL público |
| RNF-018 | Tempo até ao envio do e-mail de notificação | U05.01, U05.02, U05.06, U24.01, U24.05, U25.01, I09.02 | Testes automatizados |
| RNF-020 | Língua da interface | U22.01, U22.02, U30.01, U30.02, U30.03 | Testes automatizados |
| RNF-028 | Mensagens de erro de validação | U17.02, U18.02, U18.07, U19.06, U19.10, U21.02, U27.05, U29.04, U32.04, I01.04, I02.02, I05.02, I12.05, I14.06, I16.04, I21.07, I22.04 | Testes automatizados |
| RNF-029 | Formatos de data e hora | U01.01, U01.02, U01.03, U01.04, U01.05, U01.06, U01.07, U01.08, U25.01, U30.01, U30.02, U30.03 | Testes automatizados |
| RNF-031 | Tempo de resposta após inatividade | - | Não verificado em m3 (exige 60 minutos sem pedidos); risco R-05 do plano |
| RNF-040 | Precisão da deteção de atrasos e antecedências | I13.08 | Testes automatizados |
| RNF-041 | Precisão do aviso local da toma | - | Aplicação mobile (m4); o backend fornece os instantes em OP-34, cobertos pelos casos de RF-061 |
| RNF-042 | Recuperação de eventos após reinício do servidor | U40.01, I13.08 | Testes automatizados |
| RNF-043 | Ausência de notificações duplicadas | U13.04, U33.05, I13.02 | Testes automatizados |
| RNF-044 | Ausência de confirmações duplicadas em concorrência | U33.08, U34.02, U34.05, U34.06, U34.07, U34.08, U34.10, U34.13, U34.16, I20.07 | Testes automatizados |
| RNF-045 | Cópias de segurança | - | Infraestrutura do ambiente de demonstração; fora do âmbito dos testes do backend (manual de instalação, I051) |
| RNF-046 | Atomicidade das operações de escrita | U23.08, U27.16, U31.05, U35.03, I03.02, I03.03, I07.08, I08.03, I16.09, I21.01, I27.01, I27.02, I28.02, I29.01, I29.02, I29.03 | Testes automatizados |
| RNF-047 | Correção dos cálculos temporais em casos-limite | U01.01, U01.02, U01.03, U01.04, U01.05, U01.06, U01.07, U01.08, U01.09, U15.03, U16.01, U16.02, U16.07, U28.07, U28.08, U28.09, U28.10, I12.03 | Testes automatizados |
| RNF-049 | Integridade do histórico | U38.01 | Testes automatizados |
| RNF-052 | Envio de e-mail por endereço dedicado | - | Configuração do servidor de e-mail; verificado por inspeção da configuração e pelo comando `--enviar-email-teste` (README do backend, secção 6) |
| RNF-053 | Tecnologia do backend | - | Verificado por inspeção e pelo QG-01 (a solução compila para .NET 8 / ASP.NET Core 8) |
| RNF-054 | Autenticação por token | U47.01, U47.02, U47.03, U47.04, U47.05, I30.01, I30.02 | Testes automatizados |

### 8.3 Operações da API

As 45 operações da secção 6 da documentação da API, os pedidos recusados com `405` e a verificação do estado. A coluna dos casos de integração lista os casos que chamam a operação, direta ou indiretamente, por um auxiliar do teste. Ficam de fora as chamadas de preparação (criar conta, iniciar sessão, criar a ficha de partida), que não são o objeto do caso. Os casos unitários listados são os que citam a operação no comentário.

| Operação | Método e caminho | Casos de integração que chamam a operação | Casos unitários que a citam | Pasta da collection Postman |
|---|---|---|---|---|
| OP-01 Registar conta | `POST /auth/registo` | I04.01, I04.02, I06.02 | - | F001 Autenticação e conta |
| OP-02 Iniciar sessão | `POST /auth/login` | I04.03, I04.04, I04.07 | - | F001 Autenticação e conta |
| OP-03 Terminar sessão | `POST /auth/logout` | I04.08 | - | F001 Autenticação e conta |
| OP-04 Consultar dados da conta | `GET /conta` | I04.05, I04.06, I04.08 | U21.13 | F001 Autenticação e conta |
| OP-05 Atualizar dados da conta | `PUT /conta` | I04.06, I18.06 | - | F001 Autenticação e conta |
| OP-06 Alterar palavra-passe | `PUT /conta/palavra-passe` | I04.07 | - | F001 Autenticação e conta |
| OP-07 Listar fichas do Utilizador | `GET /fichas` | I10.05, I11.06 | U27.12 | F002 Fichas de utentes |
| OP-08 Obter as listas de necessidades | `GET /dominios/necessidades` | I11.01 | U27.24 | F002 Fichas de utentes |
| OP-09 Criar ficha (de terceiro ou própria) | `POST /fichas` | I07.01, I07.02, I07.05, I10.01, I10.02, I10.03, I10.04, I10.05, I10.06, I11.02, I11.03, I11.04, I11.05, I11.10, I14.04, I17.05, I21.04 | U21.04, U21.09 | F002 Fichas de utentes; F003 Ficha própria |
| OP-10 Consultar ficha | `GET /fichas/{fichaId}` | I07.06, I10.06, I11.02, I11.07, I11.09, I11.10, I17.01, I17.02, I18.01, I18.03, I18.08 | U27.13 | F002 Fichas de utentes |
| OP-11 Editar ficha | `PUT /fichas/{fichaId}` | I07.07, I10.05, I10.06, I11.08, I14.01, I14.02, I18.01, I18.02 | - | F002 Fichas de utentes |
| OP-12 Arquivar ficha | `POST /fichas/{fichaId}/arquivo` | I05.07, I06.03, I07.04, I10.05, I11.06, I11.08, I11.09, I12.02, I13.07, I14.07, I15.04, I16.05, I17.06, I18.07, I20.06, I20.08, I21.08, I22.06 | U10.07 | F002 Fichas de utentes |
| OP-13 Reativar ficha | `DELETE /fichas/{fichaId}/arquivo` | I10.05, I11.09 | U10.07 | F002 Fichas de utentes |
| OP-14 Eliminar ficha | `DELETE /fichas/{fichaId}` | I07.08, I08.04, I11.10, I11.11 | - | F002 Fichas de utentes |
| OP-15 Carregar documento na ficha | `POST /fichas/{fichaId}/documentos` | I07.03, I07.04, I07.06, I07.08 | U23.06 | F002 Fichas de utentes |
| OP-16 Listar documentos da ficha | `GET /fichas/{fichaId}/documentos` | I07.01, I07.03, I10.05 | U23.10 | F002 Fichas de utentes |
| OP-17 Descarregar documento | `GET /documentos/{documentoId}` | I07.03, I07.06, I22.02 | U23.10 | F002 Fichas de utentes |
| OP-18 Listar pessoas associadas à ficha | `GET /fichas/{fichaId}/pessoas` | I05.01, I05.05, I15.01, I15.02, I15.03, I15.04, I15.05, I17.01, I18.08, I18.09, I21.03 | - | F004 Associação |
| OP-19 Convidar pessoa para a ficha | `POST /fichas/{fichaId}/convites` | I03.01, I03.02, I05.01, I05.02, I05.03, I05.04, I05.05, I05.06, I05.07, I05.08, I06.01, I06.02, I06.03, I08.01, I08.02, I08.03, I08.04, I09.01, I09.02, I09.03, I13.06, I18.01, I18.02, I18.03, I18.04, I18.05, I18.06, I18.07, I18.09 | U06.03, U06.09, U21.07 | F004 Associação |
| OP-20 Listar convites recebidos | `GET /convites` | I06.01, I06.02, I06.03, I06.04, I09.01, I18.01, I18.08, I18.09 | - | F004 Associação |
| OP-21 Aceitar convite | `POST /convites/{conviteId}/aceitacao` | I03.02, I18.01, I18.02, I18.04, I18.05, I18.06, I18.07, I18.08, I18.10 | U06.09 | F004 Associação |
| OP-22 Recusar convite | `POST /convites/{conviteId}/recusa` | I18.03, I18.04, I18.05, I18.09, I18.10 | - | F004 Associação |
| OP-23 Promover Familiar a Cuidador | `PUT /fichas/{fichaId}/pessoas/{utilizadorId}/papel` | I14.01, I14.05, I14.06, I14.07, I14.08, I14.09 | - | F004 Associação |
| OP-24 Despromover Cuidador a Familiar | `PUT /fichas/{fichaId}/pessoas/{utilizadorId}/papel` | I14.02, I14.03, I14.04, I14.05, I14.06, I14.07, I14.08, I21.03 | - | F004 Associação |
| OP-25 Remover acesso de uma pessoa | `DELETE /fichas/{fichaId}/pessoas/{utilizadorId}` | I17.01, I17.02, I17.03, I17.04, I17.05, I17.06, I17.07, I21.08 | - | F004 Associação |
| OP-26 Transferir o papel de Cuidador principal | `POST /fichas/{fichaId}/cuidador-principal` | I03.03, I21.01, I21.02, I21.03, I21.04, I21.05, I21.06, I21.07, I21.08, I21.09 | U07.05, U09.05 | F004 Associação |
| OP-27 Registar tratamento | `POST /fichas/{fichaId}/tratamentos` | I13.01, I13.02, I13.03, I13.04, I13.05, I13.07, I13.08, I20.01, I20.02, I20.03, I20.04, I20.05, I20.06, I20.07, I20.08, I22.01, I22.02, I22.03, I22.04, I22.05, I22.06, I22.07 | - | F005 Registo de medicação |
| OP-28 Listar tratamentos da ficha | `GET /fichas/{fichaId}/tratamentos` | I22.01 | - | F005 Registo de medicação |
| OP-29 Consultar tratamento | `GET /tratamentos/{tratamentoId}` | I22.01, I22.02 | - | F005 Registo de medicação |
| OP-30 Atualizar tratamento | `PUT /tratamentos/{tratamentoId}` | I22.03, I22.05, I22.06 | - | F005 Registo de medicação |
| OP-31 Terminar tratamento antecipadamente | `POST /tratamentos/{tratamentoId}/termino` | I22.03, I22.05, I22.06 | - | F005 Registo de medicação |
| OP-32 Listar tomas da ficha | `GET /fichas/{fichaId}/tomas` | I20.01, I20.02 | U34.10 | F006 Horários e confirmação de toma |
| OP-33 Confirmar toma | `POST /tomas/{tomaId}/confirmacao` | I13.03, I20.03, I20.04, I20.05, I20.06, I20.07 | U39.04 | F006 Horários e confirmação de toma |
| OP-34 Sincronizar o calendário de tomas | `GET /calendario/tomas` | I20.08 | - | F006 Horários e confirmação de toma |
| OP-41 Registar compromisso | `POST /fichas/{fichaId}/compromissos` | I02.01, I02.02, I02.03, I02.04 | U17.01 | F008 Compromissos e agenda |
| OP-42 Listar compromissos da ficha | `GET /fichas/{fichaId}/compromissos` | I02.01, I02.02, I02.03, I02.04 | U17.06, U17.07 | F008 Compromissos e agenda |
| OP-43 Editar compromisso | `PUT /compromissos/{compromissoId}` | I02.01, I02.03, I02.04 | U17.08, U17.09 | F008 Compromissos e agenda |
| OP-44 Remover compromisso | `DELETE /compromissos/{compromissoId}` | I02.01, I02.03, I02.04 | U17.09, U17.10 | F008 Compromissos e agenda |
| OP-45 Consultar agenda | `GET /agenda` | I01.01, I01.02, I01.03, I01.04, I02.01 | - | F008 Compromissos e agenda |
| OP-46 Registar ocorrência | `POST /fichas/{fichaId}/ocorrencias` | I16.01, I16.03, I16.04, I16.05, I16.06, I16.08, I16.09 | - | F009 Ocorrências e notas partilhadas |
| OP-47 Registar nota partilhada | `POST /fichas/{fichaId}/notas` | I16.02, I16.03, I16.04, I16.05, I16.06, I16.08 | - | F009 Ocorrências e notas partilhadas |
| OP-48 Consultar ocorrências | `GET /fichas/{fichaId}/registos?tipo=Ocorrencia` | I16.01, I16.02, I16.04, I16.05, I16.07 | U31.11 | F009 Ocorrências e notas partilhadas |
| OP-49 Consultar notas partilhadas | `GET /fichas/{fichaId}/registos?tipo=Nota` | I16.02, I16.04, I16.05 | U31.11 | F009 Ocorrências e notas partilhadas |
| OP-50 Consultar lista de tarefas diária | `GET /lista-diaria` | I12.01, I12.02, I12.03, I12.04, I12.05, I12.06 | - | F010 Lista diária |
| OP-51 Consultar histórico de notificações | `GET /notificacoes` | I13.02, I13.04, I13.05, I13.06 | U29.04, U30.01 | F011 Notificações |
| Pedidos recusados com 405 (secção 4 da documentação da API) | `POST /compromissos/{compromissoId}/confirmacao`; `PUT`, `PATCH` e `DELETE /fichas/{fichaId}/registos/{registoId}` | I02.05, I16.06 | - | F008 Compromissos e agenda; F009 Ocorrências e notas partilhadas |
| Verificação do estado (sem identificador OP) | `GET /saude` | I19.01 | - | - |

## 9. Lacunas, defeitos conhecidos e casos de teste planeados

As situações seguintes foram observadas em 2026-11-02, com pedidos reais a uma API local no commit `7bf212c`, durante a revisão da documentação da API (Issue I042), ou estão registadas na própria documentação da API. Em cada uma, o resultado esperado segue o contrato. Os casos planeados não são acrescentados às suites enquanto o grupo não decidir se corrige o código ou a documentação. Um caso que falha bloquearia a pipeline (CS-01 e CS-02) e o plano não permite desativar testes.

| N.º | Situação | Comportamento observado | Caso de teste planeado e resultado esperado | Estado |
|---|---|---|---|---|
| L-01 | Notificação antes de um compromisso (RF-117) | A antecedência é validada e guardada (U02, U17), mas nenhum passo do processo periódico cria a notificação `ProximoCompromisso` (documentação da API, secção 8) | Executar o passo do processo periódico no instante «data e hora do compromisso menos a antecedência»: cria uma notificação `ProximoCompromisso` para cada Cuidador e Familiar da ficha e o respetivo e-mail, uma só vez (RNF-043), e nenhuma numa ficha arquivada (RF-122) | Por implementar; depende da correção do código numa Issue própria |
| L-02 | Formato das datas nos parâmetros de consulta (`AAAA-MM-DD`) | `10-03-2027` é lido como 3 de outubro de 2027 (mês primeiro), em vez de ser recusado: OP-45 e OP-51 respondem `200`; OP-32, com `de=10-03-2027&ate=2027-03-10`, responde `400` no campo `ate` («anterior a `de`»), um erro que não indica a causa. OP-50 recusa o formato com `400` no campo `data`. OP-42 e OP-48 usam o mesmo tipo de parâmetro, mas não foram experimentadas | Em cada operação com datas na consulta, uma data noutro formato dá `400 VALIDACAO_INVALIDA` com o campo da data, como já verifica I12.05 para OP-50 | Por decidir (corrigir o código ou documentar) |
| L-03 | Ficheiros acima do limite do servidor | Um documento de 11 MB dá `413 FICHEIRO_DEMASIADO_GRANDE`, como o contrato. Um de 31 MB em OP-15 dá `400 VALIDACAO_INVALIDA` no campo `corpo` («tem de ser JSON válido»), por exceder o limite por omissão do servidor web (30 000 000 bytes); um cliente que continua a enviar pode ver a ligação terminada | Um pedido com um documento acima de 30 000 000 bytes em OP-09, OP-11, OP-15, OP-27 e OP-30 dá `413 FICHEIRO_DEMASIADO_GRANDE` | Por decidir (corrigir o código ou documentar) |
| L-04 | Convite do próprio criador como Cuidador principal (OP-09) | OP-09 com `cuidadorPrincipal.eu=false` e o e-mail do próprio criador responde `201` e cria um convite para o próprio, que depois não pode ser aceite (`PESSOA_JA_ASSOCIADA`). OP-19 recusa o mesmo caso com `409 PESSOA_JA_ASSOCIADA` | OP-09 com o e-mail do próprio criador dá `409 PESSOA_JA_ASSOCIADA` (ou `400`, conforme a decisão) e não cria a ficha | Por decidir; correção do código numa Issue própria |
| L-05 | Corpo comum dos erros (documentação da API, secção 3.3) | Erros produzidos antes dos controllers não usam o corpo comum: identificador que não é GUID na rota e rota inexistente dão `404` sem corpo; método não suportado (por exemplo `PATCH /fichas`) dá `405` sem corpo; `Content-Type` não aceite dá `415` sem corpo ou em `application/problem+json` | Cada um destes pedidos dá o corpo comum com um código de erro do contrato, ou a documentação regista a exceção | Por decidir, em conjunto com a revisão da documentação da API (I042) |

## 10. Verificação desta versão

- O catálogo foi extraído do código dos testes do commit `7bf212c`: 77 classes e 520 métodos `[Fact]` ou `[Theory]`, todos com comentário `<summary>`, nenhum com `Skip`.
- O número de casos (631: 454 unitários e 177 de integração) é a soma de 1 por `[Fact]` e do número de `[InlineData]` por `[Theory]`. Coincide com a lista de testes do próprio `dotnet test --list-tests` em cada projeto.
- As duas suites foram executadas em 2026-11-03 (procedimentos P-01 a P-03, Windows, SQL Server 2022 em contentor): 631 casos passados, 0 falhados e 0 ignorados, com 98,9% de cobertura de linhas e 93,8% de ramos. São números de verificação desta especificação; os resultados oficiais são os da Issue I049.
- Os 96 requisitos funcionais ativos têm pelo menos um caso de teste (secção 8.1). O RF-117 está coberto só em parte (L-01).
- As 45 operações são chamadas por pelo menos um caso de teste de integração (secção 8.3). As operações de cada caso foram obtidas das chamadas HTTP do próprio método e dos auxiliares que usa.
- Os 13 requisitos não funcionais do contrato sem caso automatizado têm a forma de verificação indicada na secção 8.2.
- Nenhum caso de teste se refere à F007, exceto os que verificam a sua retirada (I24, I25).
- A contagem da collection Postman (128 pedidos e 277 verificações) foi feita sobre o ficheiro `m3-collection-postman-v02.json` do mesmo commit.
