# ESPECIFICAÇÃO DOS TESTES DE SISTEMA DO FRONTEND WEB

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m4 |
| Versão | v03 |
| Issue | I063 (Sprint 07); I070 (Sprint 08); I079 (Sprint 09) |
| Artefacto da Checklist de Controlo de Artefactos | OF-M4-012 Especificação dos testes de sistema do frontend web |
| Pasta oficial | `03-artefactos-de-qualidade-de-produto/03.02-frontend-web/03.02.02-testes-de-sistema` |
| Plano de referência | `m4-plano-testes-frontend-web-v01.md` (I062) |
| Código de referência | Frontend web da I057 e da I059 (na `main`), da I064 (F004), da I066 (F005), da I068 (F006), da I071 (F008) e da I077 (F011); pipeline da I061 |

| Versão | Alterações |
|---|---|
| v01 | Primeira versão: cenários de sistema da F001 (RF-010 a RF-017), automatizados com Cypress |
| v02 | Cenários de F002 a F006 (I070), com sucesso e recusa por requisito; F006 segundo o contrato de interface da F006 (I068); rastreabilidade de todos os requisitos de F002 a F006; secção 6 (dependência da I068, tempo nas tomas e ficheiros de teste) |
| v03 | Cenários da F008 Agenda e compromissos (I071) e da F011 Histórico de notificações (I077), independentes da hora do dia e do fuso da pipeline (I079); rastreabilidade de F008 e F011; secção 6.4 (dependências da I079, incluindo a F009 e a F010 do frontend web, ainda por integrar) |

## 1. Âmbito

Esta especificação descreve os cenários de sistema do frontend web, segundo o plano de testes (secções 4.1 e 5.2 a 5.4). A v01 cobriu a F001. A v02 acrescenta a F002 Fichas de utentes, a F003 Ficha própria, a F004 Associação de Cuidadores e Familiares, a F005 Registo de medicação (ecrãs E05 a E17) e a F006 Confirmação de toma (calendário de tomas da I068, secção 6.1). A v03 acrescenta a F008 Agenda e compromissos (E21 e E22, I071) e a F011 Histórico de notificações (E25, I077). Os cenários da F009 Ocorrências e notas (E23 e E24, I073) e da F010 Lista diária (E04, I075) entram quando esses ecrãs forem integrados (secção 6.4).

## 2. Execução

| Elemento | Definição |
|---|---|
| Ferramenta | Cypress 13.17.0 (versão fixa em `frontend-web/package.json`) |
| Código dos testes | `frontend-web/cypress/e2e/f001-*.cy.ts` a `f006-*.cy.ts`, `f008-agenda.cy.ts` e `f011-notificacoes.cy.ts`; apoio em `cypress/support/e2e.ts` (F001) e `cypress/support/dados.ts` (preparação pela API e ficheiros fictícios); configuração em `frontend-web/cypress.config.ts` |
| Endereço da aplicação | `CYPRESS_baseUrl`; na frontend-web-ci, `https://localhost:4200` (`ng serve` com proxy `/api` para a API da execução) |
| Backend | API real (`DoseCerta.Api`) e SQL Server 2022 criados e destruídos em cada execução da frontend-web-ci (QG-06) |
| Localização dos elementos | Rótulo exato do campo (`<label for>`), texto do botão ou da ligação e papel acessível (`role="alert"`, `role="status"`), nunca a posição nem as classes de estilo (a única exceção é `.cabecalho`, a região do cabeçalho, nos cenários da F001 da I063) |
| Esperas | Pelas respostas da API (`cy.intercept` com alias) e pelos elementos; sem esperas fixas |
| Duplos | `cy.intercept` com resposta substituída só nas condições impossíveis de provocar no backend real (plano, secção 5.3): `503` `DOCUMENTO_NAO_GUARDADO` e `503` `DOCUMENTO_INDISPONIVEL`; na F006, as tomas que precisam de tempo real a passar (secção 6.2); na F011, os atrasos de tomas e o e-mail não entregue (secção 6.4). Os cenários com duplo têm «[duplo 503]» ou «[duplo]» no título; cada funcionalidade tem também cenários contra o backend real |
| Comando local | `npx cypress run` em `frontend-web/`, com a aplicação servida e a API ativa |
| Quality gate | QG-06: 100% dos cenários passam. A falha de um cenário termina a pipeline sem sucesso |

## 3. Dados de teste

- Só dados fictícios: nomes «Fictícia/Fictício», e-mails `<prefixo>.<carimbo>@exemplo.pt`, contactos `+351 910 000 0xx`.
- Cada cenário cria no início, pela API, as suas contas (OP-01, OP-02), fichas (OP-09), convites e associações (OP-19, OP-21), tratamentos (OP-27) e arquivos (OP-12), com as funções de `cypress/support/dados.ts`. Os cenários são independentes da ordem e podem ser repetidos.
- Os ficheiros (PDF mínimo, `.txt` e PDF com mais de 10 MB) são gerados em memória no momento do cenário; nenhum ficheiro de teste fica no repositório.
- As datas dos tratamentos são relativas ao dia da execução (hoje, hoje mais 30 dias, hoje menos 2 dias).
- F008: os compromissos e as tomas ficam amanhã, no calendário de Lisboa (`dataLisboa(1)`), e os instantes são enviados com o desvio de Lisboa desse dia (`instanteLisboa`): os cenários não dependem da hora do dia nem do fuso da pipeline (UTC). F011: a ocorrência e o compromisso próximo usam instantes relativos ao momento da execução (`agoraLisboa`); a espera pelo aviso do compromisso (até 60 segundos, RNF-040) é feita por consultas à OP-51, sem esperas fixas no ecrã.
- Novas funções de `cypress/support/dados.ts`: `compromissoNaApi` e `compromissosNaApi` (OP-41, OP-42), `ocorrenciaNaApi` (OP-46) e `instanteLisboa`.

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
| TS-W-F002-01 | RF-020 Criar ficha de terceiro; RF-021 Registar necessidades; RF-022 Carregar documentos; RF-024 Criador como Cuidador principal (UC-05; E05, E06/E07, E08) | Conta criada pela API; sessão iniciada | Ficha «Maria Fictícia Santos», nascida em 11/02/1946, contacto `+351 910 000 004`; documento `relatorio-ficticio.pdf` | 1. Em E05, «Criar ficha». 2. Escolher «Ficha de terceiro» e «Eu sou o Cuidador principal». 3. Preencher a identificação. 4. Adicionar o documento e escolher «Relatório clínico». 5. «Criar ficha» | `POST /api/v1/fichas` responde 201; E08 mostra «Ficha criada.», o nome, «O meu papel: Cuidador principal» e o documento |
| TS-W-F002-02 | RF-025 Convidar outra pessoa para Cuidador principal (UC-06; E06, E08, E12) | Conta criada pela API; sessão iniciada | Ficha «João Fictício Lopes»; e-mail `principal.<carimbo>@exemplo.pt` | 1. Em E06, «Ficha de terceiro» e «Convidar outra pessoa para Cuidador principal». 2. Indicar o e-mail e a identificação. 3. «Criar ficha». 4. Abrir «Pessoas da ficha» | E08 mostra «Ficha criada e convite enviado para Cuidador principal.»; em E12 o criador aparece como «Cuidador principal (provisório)» |
| TS-W-F002-03 | RF-021; RNF-028 Mensagens de erro de validação (E06) | Conta criada pela API; sessão iniciada | Data de nascimento `2099-01-01`; contacto `+351 910 000 006`; nome vazio | 1. Em E06, ficha de terceiro, eu como principal. 2. Deixar o nome vazio e indicar uma data futura. 3. «Criar ficha» | «Nome completo: é obrigatório.» e «Data de nascimento: não pode ser uma data futura.» junto aos campos; o contacto mantém-se; nenhum pedido `POST /api/v1/fichas` |
| TS-W-F002-04 | RF-029 Editar ficha; RNF-028 (UC-07; E08, E09) | Ficha criada pela API | Nome novo «Rosa Fictícia Pereira»; contacto `+351 910 000 007` | 1. Em E08, «Editar ficha». 2. Apagar o nome, alterar o contacto e «Guardar alterações». 3. Escrever o nome novo e «Guardar alterações» | Primeiro, «Nome completo: é obrigatório.» e o contacto escrito mantém-se; depois, `PUT` 200, «Alterações guardadas.» e os dados novos em E08 |
| TS-W-F002-05 | RF-022; RF-028 Consultar ficha; RNF-008 (UC-09; E08) | Ficha criada pela API; sessão iniciada em E08 | `receita-ficticia.pdf` (PDF mínimo) | 1. Escolher a categoria «Receita médica» e o ficheiro. 2. «Carregar documento». 3. «Descarregar receita-ficticia.pdf» | `POST .../documentos` 201; «Documento «receita-ficticia.pdf» carregado.»; o documento aparece com a categoria e o formato; `GET /api/v1/documentos/{id}` 200 |
| TS-W-F002-06 | RNF-008 Formatos e tamanho dos documentos (E08) | Como em TS-W-F002-05 | `notas-ficticias.txt`; `documento-grande.pdf` com mais de 10 MB | 1. Escolher o `.txt`. 2. Escolher o PDF grande. 3. «Carregar documento» | Cada ficheiro é recusado antes do envio («não é PDF, JPG nem PNG»; «tem mais de 10 MB»); nenhum pedido de carregamento |
| TS-W-F002-07 | RF-023 Recusar a operação quando o documento não é guardado (E08). **Duplo:** `cy.intercept` responde `503` `DOCUMENTO_NAO_GUARDADO` | Como em TS-W-F002-05 | `cartao-ficticio.pdf`, categoria «Documento de identificação» | 1. Escolher a categoria e o ficheiro. 2. «Carregar documento» | «Não foi possível guardar o documento e nada foi gravado…»; a categoria escolhida mantém-se; a ficha continua sem documentos |
| TS-W-F002-08 | RF-124 Informar falha ao consultar documento (E08). **Duplo:** `cy.intercept` responde `503` `DOCUMENTO_INDISPONIVEL` ao descarregar | Documento carregado no próprio cenário (backend real) | `receita-indisponivel.pdf` | 1. Carregar o documento. 2. «Descarregar» com a resposta substituída | No lugar do documento: «Não foi possível obter o ficheiro neste momento. Tente novamente.» e o botão «Tentar novamente descarregar …»; o resto da ficha continua visível |
| TS-W-F002-09 | RF-120 Arquivar ficha; RF-122 (só consulta); RF-028 (UC-10; E08, E05) | Ficha criada pela API | Ficha «Lúcia Fictícia» | 1. Em E08, «Arquivar ficha» e confirmar. 2. Em E05, filtro «Arquivadas» | «Ficha arquivada. Fica só de consulta até ser reativada.»; sem «Editar ficha» nem «Carregar documento»; a ficha aparece nas arquivadas com o estado «Arquivada» |
| TS-W-F002-10 | RF-121 Reativar ficha (UC-11; E08) | Ficha criada e arquivada pela API | Ficha «Paulo Fictício» | 1. Em E08, «Reativar ficha» e confirmar | «Ficha reativada.»; «Editar ficha» volta a aparecer |
| TS-W-F002-11 | RF-123 Eliminar ficha sem registos (UC-12; E08, E05) | Ficha criada pela API, sem medicação | Ficha «Sérgio Fictício» | 1. Em E08, «Eliminar ficha» e confirmar | `DELETE` 204; E05 com «Ficha eliminada.» e sem a ficha |
| TS-W-F002-12 | RF-123 (recusa com histórico) (E08) | Ficha com um tratamento registado pela API | Ficha «Teresa Fictícia»; tratamento «Paracetamol 1 g» | 1. Em E08, «Eliminar ficha» e confirmar | `DELETE` 409 `FICHA_COM_HISTORICO`; «… não pode ser eliminada; só pode ser arquivada»; a ficha mantém-se |
| TS-W-F002-13 | RF-028; RF-048 Restringir o Familiar a consulta (E05, E08) | Ficha de um Cuidador; Familiar associado pela API (OP-19, OP-21) | Ficha «Helena Fictícia»; Familiar «Filipe Fictício» | 1. O Familiar abre E05 e a ficha | E05 mostra o papel «Familiar»; E08 mostra «O meu papel: Familiar» e o aviso de consulta, sem «Editar ficha», «Arquivar», «Eliminar» nem «Carregar documento» |
| TS-W-F002-14 | RF-024; RF-025 (recusas); RNF-028 (E06/E07) | Conta criada pela API; sessão iniciada | Ficha «Vera Fictícia», nascida em 03/10/1944 | 1. Ficha de terceiro, identificação preenchida, sem escolher o Cuidador principal; «Criar ficha». 2. Escolher «Convidar outra pessoa…» sem e-mail; «Criar ficha» | «Escolha quem é o Cuidador principal.» e depois «E-mail da pessoa a convidar: é obrigatório.»; os dados mantêm-se; nenhum pedido de criação |
| TS-W-F002-15 | RF-028 (recusa); RNF-005 (E08) | Ficha de outra conta, criada pela API | Ficha «Beatriz Fictícia»; conta «Gil Fictício» | 1. A conta não associada abre `/fichas/{id}` | OP-10 responde 404; «A ficha não existe ou não tem acesso a ela.»; nenhum dado da ficha aparece |
| TS-W-F003-01 | RF-030 Criar ficha própria (UC-05; E06, E08) | Conta sem ficha própria | Ficha «Ana Fictícia», nascida em 21/09/1985 | 1. Em E06, «Ficha própria». 2. Preencher e «Criar ficha» | Não aparece a escolha do Cuidador principal; o pedido leva `propria: true`; E08 mostra «Ficha própria» e «O meu papel: Cuidador principal» |
| TS-W-F003-02 | RF-031 Limitar a uma ficha própria por Utilizador (E06) | Ficha própria criada pela API | — | 1. Abrir E06 | A opção «Ficha própria» está desativada, com «Já tem uma ficha própria e só pode ter uma; pode criar fichas de terceiros.» |
| TS-W-F003-03 | RF-032 Regras das restantes fichas; RF-033 Proteger o titular; RF-034 Titular sempre principal (E12, E13) | Ficha própria criada pela API | E-mail `familiar.f003.<carimbo>@exemplo.pt` | 1. Abrir E13 na ficha própria. 2. Convidar como Familiar. 3. Ver E12 | E13 não tem «Oferecer o papel de Cuidador principal»; `POST .../convites` 201; em E12 a linha do titular não tem ações e aparece a explicação da proteção |
| TS-W-F004-01 | RF-040 Convidar pessoa; RF-043 Impedir convite pendente duplicado; RNF-028 (UC-13; E08, E12, E13) | Ficha criada pela API | E-mail `convidado.<carimbo>@exemplo.pt`; papel Familiar | 1. E08 → «Pessoas da ficha» → «Convidar pessoa». 2. Convidar. 3. Repetir o mesmo convite | 201 e «Convite enviado.»; depois 409 com «Já existe um convite pendente para este e-mail nesta ficha.» junto ao e-mail, que se mantém escrito |
| TS-W-F004-02 | RF-042 Convites na área de convites; RF-045 Aceitar convite; RF-048 (UC-14; E05, E14, E08, E12) | Convite pendente para Familiar, criado pela API | Ficha «Rita Fictícia»; convidado «Bruno Fictício» | 1. Em E05, «Convites recebidos (1)». 2. Em E14, «Aceitar» e confirmar. 3. «Abrir a ficha» e «Pessoas da ficha» | E14 mostra a ficha, o papel e quem convidou; «Convite aceite.»; E08 com «O meu papel: Familiar»; E12 só com a lista, sem ações nem «Convidar pessoa» |
| TS-W-F004-03 | RF-046 Recusar convite; RF-043 (novo convite depois da recusa) (UC-14; E14) | Convite pendente para Cuidador, criado pela API | Ficha «Vasco Fictício»; convidada «Carla Fictícia» | 1. Em E14, «Recusar» e confirmar. 2. O Cuidador volta a convidar (API). 3. Recarregar E14 | «Convite recusado.» e «Não tem convites pendentes.»; depois o novo convite aparece |
| TS-W-F004-04 | RF-044 Convites pendentes após o registo (E01, E02, E14) | Convite criado pela API para um e-mail sem conta | Conta nova «Diana Fictícia» | 1. Registar a conta com esse e-mail em E01. 2. Iniciar sessão e abrir E14 | E02 mostra «Tem 1 convite pendente para ver em Convites.»; E14 mostra o convite |
| TS-W-F004-05 | RF-025; RF-026 Transferir o principal quando o convite é aceite (E14, E08) | Ficha criada pela API com convite para Cuidador principal | Ficha «Óscar Fictício»; convidada «Eva Fictícia» | 1. Em E14, ver a explicação e aceitar o convite «Cuidador principal». 2. «Abrir a ficha» | E08 com «O meu papel: Cuidador principal»; o criador deixa de ter acesso (OP-10 responde 404) |
| TS-W-F004-06 | RF-027 Manter o criador quando o convite é recusado (E14) | Como em TS-W-F004-05 | Ficha «Pedro Fictício»; convidado «Gil Fictício» | 1. Em E14, recusar o convite para Cuidador principal | «Convite recusado.»; OP-18 mostra o criador como Cuidador principal, já não provisório |
| TS-W-F004-07 | RF-120, RF-122 (ficha arquivada: gestão só de consulta); resposta a convites permitida (matriz, RP-13) (E12, E13) | Ficha com convite pendente, arquivada pela API | Ficha «Zita Fictícia»; convidado «Hugo Fictício» | 1. O Cuidador abre E12. 2. Abre E13 à mão. 3. O convidado aceita (API) | E12 com «Ficha arquivada: as pessoas associadas só podem ser consultadas.» e sem «Convidar pessoa»; E13 volta a E08 com o aviso; a aceitação é aceite e o convidado fica Familiar |
| TS-W-F004-08 | RF-047 Acesso completo do Cuidador; RF-049 Promover; RF-130 Despromover (UC-15; E12) | Ficha com um Familiar e um Cuidador associados pela API | Ficha «Marta Fictícia»; «Bruno Fictício» (Familiar), «Carla Fictícia» (Cuidador) | 1. «Promover a Cuidador» o Bruno e confirmar. 2. «Despromover a Familiar» e confirmar | «Bruno Fictício passou a Cuidador.» e depois «… passou a Familiar.», com o papel atualizado na lista |
| TS-W-F004-09 | RF-131 Remover acesso à ficha (UC-15; E12) | Como em TS-W-F004-08 | — | 1. «Remover» o Bruno e confirmar | «Bruno Fictício deixou de ter acesso à ficha.»; deixa de aparecer na lista |
| TS-W-F004-10 | RF-132 Transferir o principal; RF-133 Exigir substituto; RF-134 Ficha sempre com um Cuidador (UC-16; E12) | Como em TS-W-F004-08 | — | 1. Ver a linha do Cuidador principal (o próprio). 2. «Tornar principal» a Carla e confirmar | Na linha do principal, «Despromover a Familiar» e «Remover» inativos, com «Indique outro Cuidador para assumir o papel…» e «A ficha tem de ficar com pelo menos um Cuidador.»; depois, «Carla Fictícia é agora o Cuidador principal.» e o anterior fica Cuidador |
| TS-W-F004-11 | RF-048 (guarda de papel) (E13, E08) | Familiar associado pela API | — | 1. O Familiar abre `/fichas/{id}/pessoas/convidar` | Volta a E08 com «Como Familiar, só pode consultar esta ficha.» |
| TS-W-F004-12 | RF-045, RF-046, RF-026, RF-027 (recusa: convite já respondido) (E14) | Convite pendente, aceite pela API noutra sessão depois de E14 abrir | Ficha «Tiago Fictício»; convidada «Joana Fictícia» | 1. Em E14, «Aceitar». 2. Entretanto, o convite é aceite noutra sessão. 3. «Sim, aceitar o convite» | OP-21 responde 409 `CONVITE_JA_RESPONDIDO`; «O convite já foi aceite ou recusado.»; a lista é carregada de novo e o convite deixa de aparecer |
| TS-W-F005-01 | RF-050 Registar medicação; RF-051 Grau de importância; RF-052 Carregar receita; RF-054 (UC-17; E08, E15, E16, E17) | Ficha criada pela API | «Metformina», «500 mg», 20:00 e 08:00, de hoje a hoje + 30 dias, Moderado, `receita-ficticia.pdf` | 1. E08 → «Medicação» → «Novo tratamento». 2. Preencher e escolher a receita. 3. «Guardar». 4. Abrir o tratamento | `POST .../tratamentos` 201; E15 com «Tratamento registado.», «08:00 e 20:00», «Moderado (tolerância 1 h)» e o período em dd/mm/aaaa; E17 com «Abrir a receita» |
| TS-W-F005-02 | RF-050 (tratamento contínuo); RF-060 só pela apresentação (E16, E15) | Como em TS-W-F005-01 | «Insulina», «10 UI», 12:00, Grave, sem data de fim | 1. Registar sem data de fim | E15 mostra «desde dd/mm/aaaa, contínuo» |
| TS-W-F005-03 | RF-050; RF-051; RNF-028 (E16) | Como em TS-W-F005-01 | «Varfarina»; 09:00 duas vezes; fim = hoje − 2 dias | 1. «Guardar» sem nada. 2. Preencher o medicamento, repetir a hora e indicar um fim anterior ao início. 3. «Guardar» | «Medicamento: é obrigatório.», «Indique pelo menos uma hora de toma.» e «Escolha o grau de importância.»; «A hora 09:00 já está na lista…»; «Data de fim: não pode ser anterior à data de início.»; os dados mantêm-se; nenhum pedido de registo |
| TS-W-F005-04 | RF-052; RNF-008 (E16) | Como em TS-W-F005-01 | `notas-ficticias.txt`; PDF com mais de 10 MB | 1. Escolher cada ficheiro como receita | Cada ficheiro é recusado antes do envio, junto ao campo da receita |
| TS-W-F005-05 | RF-053 Recusar a operação quando a receita não é guardada (E16). **Duplo:** `cy.intercept` responde `503` `DOCUMENTO_NAO_GUARDADO` | Como em TS-W-F005-01 | «Atorvastatina», «20 mg», 21:00, Leve, `receita-atorvastatina.pdf` | 1. Preencher, escolher a receita e «Guardar» | «A receita não foi guardada. O tratamento não foi registado.»; os dados e a receita escolhida mantêm-se; continua em E16 |
| TS-W-F005-06 | RF-055 Atualizar tratamento em curso (UC-18; E17, E16) | Tratamento registado pela API | Dose nova «850 mg»; hora nova 20:00 | 1. Em E17, «Editar». 2. Alterar a dose, acrescentar a hora e «Guardar». 3. Terminar pela API e abrir `.../editar` | A data de início está bloqueada e há o aviso dos horários; o `PUT` envia só `dose` e `horarios`; «Alterações guardadas.»; depois de terminado, a edição volta a E17 com «O tratamento já terminou e não pode ser editado» |
| TS-W-F005-07 | RF-056 Terminar tratamento; RF-054; RF-057 Preservar histórico (UC-19; E17, E15) | Tratamento registado pela API | «Amoxicilina 1 g» | 1. Em E17, «Terminar tratamento» e confirmar. 2. Em E15, «Terminados» | A confirmação explica que as tomas futuras deixam de existir; «Tratamento terminado.» e a etiqueta «Terminado», sem «Editar»; aparece nos terminados |
| TS-W-F005-08 | RF-048; RF-054 (E15, E17, E16) | Familiar associado; tratamento registado pela API | «Omeprazol 20 mg» | 1. O Familiar abre E15 e E17. 2. Abre `.../medicacao/novo` à mão | Vê os dados sem «Novo tratamento», «Editar» nem «Terminar tratamento»; `.../novo` volta a E08 com o aviso |
| TS-W-F005-09 | RF-056 (permitido em ficha arquivada); RF-120 (E15, E17) | Tratamento registado e ficha arquivada pela API | «Paracetamol 1 g» | 1. Abrir E15 e E17. 2. «Terminar tratamento» e confirmar | E15 com o aviso de ficha arquivada e sem «Novo tratamento»; E17 sem «Editar»; o tratamento é terminado |
| TS-W-F005-10 | RF-054 (recusa); RNF-005 (E15) | Ficha de outra conta com um tratamento | «Lisinopril 10 mg»; conta «Gil Fictício» | 1. A conta não associada abre `/fichas/{id}/medicacao` | O `fichaGuard` reencaminha para E05 com «A ficha não existe ou não tem acesso a ela.»; o tratamento não aparece |
| TS-W-F006-01 | RF-063 Calendário de tomas; RNF-027; RNF-029 (E15, separador «Calendário de tomas» da I068) | Tratamento contínuo registado hoje pela API (Moderado, 08:00) | Medicamento único `Metformina TS-W-F006-01 <carimbo>`, «850 mg» | 1. Em E08, «Medicação». 2. Em E15, separador «Calendário de tomas». 3. «Período seguinte» | Cabeçalho «Tomas de <ontem> a <hoje + 5>»; a toma com «08:00», o medicamento, a dose, «Moderado» e «Pendente» (texto e ícone); depois, «Tomas de <hoje + 6> a <hoje + 12>» |
| TS-W-F006-02 | RF-064 Confirmar uma toma; RF-065 Autor e momento; RNF-021 (uma interação) | Toma de hoje às 23:59, grau Leve (backend real) | — | 1. «Confirmar toma» (um só clique) | OP-33 200; «Toma confirmada.» (`role="status"`); a toma fica «Confirmada», com «Confirmada por Ana Fictícia em <hoje> às …», sem botão |
| TS-W-F006-03 | RF-067 Toma confirmada fora da tolerância. **Duplo:** `cy.intercept` responde à OP-33 `200` com o estado `ConfirmadaComAtraso` | Toma de hoje às 23:59, grau Grave (backend real) | — | 1. «Confirmar toma» | «Toma confirmada com atraso.»; a toma fica «Confirmada com atraso», com «Confirmada por Ana Fictícia em <hoje> às 23:59» |
| TS-W-F006-04 | RF-064 (recusa: fora da janela) | Toma de amanhã às 09:00 | — | 1. «Confirmar toma» | OP-33 409 `TOMA_FORA_DA_JANELA`; «A toma só pode ser confirmada a partir das 00:00 do dia previsto.» (`role="alert"`); a lista é carregada de novo e a toma continua «Pendente» |
| TS-W-F006-05 | RF-068 Toma não confirmada (recusa `TOMA_NAO_CONFIRMAVEL`). **Duplo:** `cy.intercept` responde à OP-33 `409` `TOMA_NAO_CONFIRMAVEL` e, depois da recusa, devolve a toma da OP-32 com o estado `NaoConfirmada` | Toma de hoje às 23:59, grau Leve (backend real) | — | 1. «Confirmar toma» | «A toma ficou não confirmada e já não pode ser confirmada.» (`role="alert"`); a lista é carregada de novo e a toma aparece «Não confirmada», sem «Confirmar toma» |
| TS-W-F006-06 | RF-105 Impedir a confirmação repetida (E18 no web: confirmação no calendário) | Toma de hoje às 23:59; outro Cuidador («Carla Fictícia») associado confirma pela API depois de o ecrã abrir | — | 1. «Confirmar toma» | OP-33 409 `TOMA_JA_CONFIRMADA`; «A toma já foi confirmada por Carla Fictícia em …»; a lista é carregada de novo e mostra «Confirmada por Carla Fictícia em …» |
| TS-W-F006-07 | RF-048 Restringir o Familiar a consulta | Toma de hoje às 23:59; Familiar associado pela API | — | 1. O Familiar abre o calendário | «Como Familiar, pode consultar as tomas, mas não as confirmar.» (`role="status"`); a toma aparece «Pendente», sem botão |
| TS-W-F006-08 | RF-120 e RF-064 (ficha arquivada: ainda se confirma o que ficou por confirmar). **Duplo:** `cy.intercept` acrescenta à OP-32 a toma que ficou por confirmar antes do arquivo e responde à OP-33 `200` `Confirmada` | Toma de hoje às 23:59; ficha arquivada pela API (backend real), o que remove as tomas futuras por confirmar | — | 1. «Confirmar toma» | Aviso «Ficha arquivada: …» (`role="status"`); «Toma confirmada.»; «Confirmada por Ana Fictícia em …» |
| TS-W-F008-01 | RF-080 Registar compromisso; RF-116 (antecedência por omissão); RF-089 | Cuidador com a ficha «Maria Fictícia Santos» e um tratamento com toma amanhã às 09:00 | Amanhã, 10:30; «Centro de Saúde Fictício»; «Consulta de rotina» | 1. Barra lateral, «Agenda». 2. «Novo compromisso». 3. Preencher Data, Hora, Local e Motivo, sem mudar a antecedência. 4. «Guardar» | E22 mostra «Ficha: Maria Fictícia Santos» e «24 horas (por omissão)»; volta a E21 no dia do compromisso com «Compromisso registado.» (`role="status"`); o evento mostra «10:30», o utente e «Compromisso: Consulta de rotina»; OP-42 devolve a antecedência 1440 e a data e hora com o desvio de Lisboa |
| TS-W-F008-02 | RF-080 (recusa: campos em falta); RNF-028 | Como TS-W-F008-01 | Só o Local preenchido | 1. Abrir E22. 2. «Guardar» | «Data: é obrigatório.», «Hora: é obrigatório.» e «Motivo: é obrigatório.» junto de cada campo; o Local mantém-se; nada é gravado (OP-42 vazio) |
| TS-W-F008-03 | RF-081 Editar compromisso; RF-116 | Compromisso de amanhã às 10:30, registado pela API | Hora 11:00; Motivo «Consulta de cardiologia»; antecedência «1 hora» | 1. E21 no dia de amanhã. 2. «Editar» no compromisso. 3. Alterar e «Guardar» | E22 abre com os valores atuais; «Compromisso atualizado.»; o evento passa a «11:00» e «Consulta de cardiologia»; OP-42 devolve a antecedência 60 |
| TS-W-F008-04 | RF-082 Remover compromisso (com confirmação) | Compromisso «Fisioterapia» amanhã às 15:00, pela API | — | 1. «Editar». 2. «Remover» e «Cancelar». 3. «Remover» e «Sim, remover o compromisso» | A pergunta «Remover o compromisso «Fisioterapia» de <amanhã> às 15:00?»; depois de cancelar, o compromisso mantém-se (OP-42); depois de confirmar, «Compromisso removido.» e o evento desaparece |
| TS-W-F008-05 | RF-084, RF-085, RF-086 Vistas diária, semanal e mensal; RF-087 e RF-088 (tomas só de consulta); RNF-012; RNF-027 | Toma amanhã às 09:00 e compromisso amanhã às 10:30 | — | 1. E21, dia de amanhã. 2. Separador «Semana». 3. Separador «Mês» | Dia: a toma às 09:00 («Toma: Metformina 500 mg (Pendente, só consulta)») antes do compromisso às 10:30, sem «Confirmar»; Semana: «Semana de … a …», 7 dias a começar à segunda-feira; Mês: grelha que começa à segunda-feira, com o compromisso; um só pedido à OP-45 por vista |
| TS-W-F008-06 | RF-083 Reunir os eventos das fichas do Utilizador; RF-089 | Três fichas do Cuidador, cada uma com um compromisso amanhã; a terceira arquivada pela API | «Consulta A», «Consulta B», «Consulta C» | 1. E21, dia de amanhã | «Consulta A» com «Maria Fictícia Santos» e «Consulta B» com «João Fictício Lopes»; «Consulta C» e a ficha arquivada não aparecem; a escolha da ficha do novo compromisso tem só as 2 fichas ativas |
| TS-W-F008-07 | RF-048 Restringir o Familiar a consulta | Compromisso amanhã; Familiar «Rui Fictício» associado pela API | — | 1. O Familiar abre E21 no dia de amanhã. 2. Abre o endereço de E22 | Vê o compromisso e a toma com o utente, sem «Novo compromisso» nem «Editar»; a rota de E22 é recusada pela guarda |
| TS-W-F008-08 | RF-120 (recusa: ficha arquivada) | Cuidador com E22 aberto; a ficha é arquivada pela API antes de guardar | Amanhã, 10:30; «Análises» | 1. Preencher. 2. «Guardar». 3. Abrir de novo o endereço de E22 | OP-41 409 `FICHA_ARQUIVADA`: «A ficha está arquivada…» (`role="alert"`); os dados mantêm-se e nada é gravado; depois, a guarda recusa a rota de E22 |
| TS-W-F011-01 | RF-140 Consultar o histórico; RF-041 (convite); RF-115 (ocorrência); RF-117 (compromisso próximo) | «Ana Fictícia» cria a ficha e associa «Rui Fictício» como Cuidador; regista, pela API, uma ocorrência (há 10 minutos) e um compromisso daqui a 20 minutos com aviso de 30 minutos | — | 1. Esperar pelas 3 notificações do Rui (OP-51). 2. O Rui abre «Notificações» | 3 notificações, da mais recente para a mais antiga: «Compromisso próximo» (motivo), «Nova ocorrência» («Ana Fictícia registou uma ocorrência…») e «Convite recebido» (com «Ver os convites»); cada uma com o utente, «dd/mm/aaaa hh:mm» e o estado do e-mail; a ficha liga a E08 |
| TS-W-F011-02 | RF-140 (recusa: só as do próprio Utilizador); RF-115 (o autor não é notificado) | A Ana regista uma ocorrência numa ficha com o Familiar «Rui Fictício»; o Rui recebe 2 notificações | — | 1. A Ana abre «Notificações» | «Não tem notificações.»; nenhuma notificação do Rui aparece à Ana |
| TS-W-F011-03 | RF-140 (atualizar ao abrir ou atualizar o ecrã) | «Sofia Fictícia» sem notificações | Convite da Ana para a ficha «João Fictício Lopes», feito com o ecrã aberto | 1. A Sofia abre «Notificações». 2. A Ana convida-a pela API. 3. «Atualizar» | Primeiro «Não tem notificações.»; depois de «Atualizar», a notificação «Convite recebido» da ficha «João Fictício Lopes» |
| TS-W-F011-04 | RF-110 a RF-112 (Cuidadores); RF-113 (Familiares); RF-118 e RF-119 (estado do e-mail). **Duplo:** `cy.intercept` responde à OP-51 com uma `TomaEmAtraso` (e-mail `Pendente`) e uma `TomaGraveFamiliares` (e-mail `NaoEntregue`) | Sessão iniciada (backend real) | — | 1. Abrir «Notificações» | Ordem pela hora da notificação; «Toma de grau Grave em atraso prolongado», «25/11/2026 11:00», «Toma: Insulina, prevista para 25/11/2026 às 10:00» e «E-mail não entregue» destacado; «Toma em atraso» com «E-mail pendente (reenvio em curso)»; o tipo com ícone além do texto |
| TS-W-F011-05 | RF-140 (erro). **Duplo:** falha de rede na OP-51 | Sessão iniciada | — | 1. Abrir «Notificações» | Mensagem de erro em `role="alert"`; nenhuma notificação |

## 5. Rastreabilidade

Para cada requisito funcional de F002 a F006 com componente web: os cenários de sucesso e os de recusa ou erro. Quando o requisito não tem um caminho de recusa no ecrã, fica a justificação.

| Requisito (prioridade) | Sucesso | Recusa ou erro |
|---|---|---|
| RF-010 a RF-017 | Como na v01: TS-W-F001-01, 03, 05, 06, 07 | TS-W-F001-02, 04, 07 |
| RF-020 Criar ficha de terceiro (Essencial) | TS-W-F002-01 | TS-W-F002-03 |
| RF-021 Registar necessidades específicas (Essencial) | TS-W-F002-01 | TS-W-F002-03 |
| RF-022 Carregar documentos na ficha (Essencial) | TS-W-F002-01, 05 | TS-W-F002-06 |
| RF-023 Recusar a operação quando o documento não é guardado (Essencial) | TS-W-F002-05 (documento guardado) | TS-W-F002-07 (duplo 503) |
| RF-024 Criador como Cuidador principal (Essencial) | TS-W-F002-01 | TS-W-F002-14 |
| RF-025 Convidar outra pessoa para Cuidador principal (Essencial) | TS-W-F002-02, TS-W-F004-05 | TS-W-F002-14 |
| RF-026 Transferir o principal quando o convite é aceite (Essencial) | TS-W-F004-05 | TS-W-F004-12 |
| RF-027 Manter o criador quando o convite é recusado (Essencial) | TS-W-F004-06 | TS-W-F004-12 |
| RF-028 Consultar ficha (Essencial) | TS-W-F002-05, 09, 13 | TS-W-F002-15 |
| RF-029 Editar ficha (Essencial) | TS-W-F002-04 | TS-W-F002-04 (primeiro passo) |
| RF-030 Criar ficha própria (Essencial) | TS-W-F003-01 | TS-W-F003-02 |
| RF-031 Limitar a uma ficha própria (Essencial) | TS-W-F003-01 | TS-W-F003-02 |
| RF-032 Regras das restantes fichas na ficha própria (Importante) | TS-W-F003-03 | TS-W-F003-03 (sem «para principal») |
| RF-033 Proteger o titular da ficha própria (Essencial) | TS-W-F003-03 | TS-W-F003-03 (ações sobre o titular indisponíveis) |
| RF-034 Titular sempre Cuidador principal (Essencial) | TS-W-F003-03 | TS-W-F003-03 |
| RF-040 Convidar pessoa (Essencial) | TS-W-F004-01, TS-W-F003-03 | TS-W-F004-01 (repetido) |
| RF-041 Notificação e e-mail de convite (Essencial) | Regra do servidor, testada no backend (plano, secção 4.2); no frontend, E13 informa o envio (TS-W-F004-01) | — |
| RF-042 Convites na área de convites (Essencial) | TS-W-F004-02 | TS-W-F004-03 (sem convites pendentes) |
| RF-043 Impedir convite pendente duplicado (Essencial) | TS-W-F004-03 (novo convite depois da recusa) | TS-W-F004-01 |
| RF-044 Convites pendentes após o registo (Essencial) | TS-W-F004-04 | Sem caminho de recusa no ecrã: a apresentação depende só do e-mail da conta (regra do servidor) |
| RF-045 Aceitar convite (Essencial) | TS-W-F004-02 | TS-W-F004-12 |
| RF-046 Recusar convite (Essencial) | TS-W-F004-03 | TS-W-F004-12 |
| RF-047 Acesso completo do Cuidador (Essencial) | TS-W-F004-08 | TS-W-F004-11 (o Familiar não tem esse acesso) |
| RF-048 Restringir o Familiar a consulta (Essencial) | TS-W-F002-13, TS-W-F004-02, TS-W-F005-08, TS-W-F006-07 | TS-W-F004-11, TS-W-F005-08 (rotas de alteração recusadas) |
| RF-049 Promover Familiar a Cuidador (Essencial) | TS-W-F004-08 | TS-W-F004-02 (o Familiar não promove) |
| RF-050 Registar medicação (Essencial) | TS-W-F005-01, 02 | TS-W-F005-03 |
| RF-051 Grau de importância (Essencial) | TS-W-F005-01 | TS-W-F005-03 |
| RF-052 Carregar receita (Importante) | TS-W-F005-01 | TS-W-F005-04 |
| RF-053 Recusar a operação quando a receita não é guardada (Essencial) | TS-W-F005-01 (receita guardada) | TS-W-F005-05 (duplo 503) |
| RF-054 Consultar medicação (Essencial) | TS-W-F005-01, 07, 08 | TS-W-F005-10 |
| RF-055 Atualizar tratamento em curso (Essencial) | TS-W-F005-06 | TS-W-F005-06 (tratamento terminado) |
| RF-056 Terminar tratamento antecipadamente (Importante) | TS-W-F005-07, 09 | TS-W-F005-08 (o Familiar não termina) |
| RF-057 Preservar histórico de tratamentos terminados (Essencial) | TS-W-F005-07 | TS-W-F005-06 (o terminado não se edita) |
| RF-063 Calendário de tomas (Essencial) | TS-W-F006-01 | TS-W-F006-04 (recusa com a lista carregada de novo) |
| RF-064 Confirmar uma toma (Essencial) | TS-W-F006-02, 08 | TS-W-F006-04, 06 |
| RF-065 Autor e momento da confirmação (Essencial) | TS-W-F006-02, 03 | TS-W-F006-06 (mostra o autor da primeira confirmação) |
| RF-067 Toma confirmada fora da tolerância (Essencial) | TS-W-F006-03 (duplo) | TS-W-F006-02 (dentro da tolerância não fica com atraso) |
| RF-068 Toma não confirmada (Essencial) | Regra do servidor (processo periódico), testada no backend | TS-W-F006-05 (duplo: recusa `TOMA_NAO_CONFIRMAVEL` e toma «Não confirmada» sem botão) |
| RF-080 Registar um compromisso de saúde (Essencial) | TS-W-F008-01 | TS-W-F008-02, 08 |
| RF-081 Editar um compromisso de saúde (Essencial) | TS-W-F008-03 | TS-W-F008-07 (o Familiar não edita) |
| RF-082 Remover um compromisso de saúde (Essencial) | TS-W-F008-04 | TS-W-F008-04 (cancelar mantém o compromisso), TS-W-F008-07 |
| RF-083 Reunir na agenda os eventos das fichas do Utilizador (Essencial) | TS-W-F008-06 | TS-W-F008-06 (a ficha arquivada fica fora da agenda) |
| RF-084 Vista diária (Essencial) | TS-W-F008-05 | Sem caminho de recusa no ecrã: a vista mostra o que a OP-45 devolve |
| RF-085 Vista semanal (Essencial) | TS-W-F008-05 | Idem |
| RF-086 Vista mensal (Essencial) | TS-W-F008-05 | Idem |
| RF-087 Impedir a confirmação de compromissos (Essencial) | TS-W-F008-05 (sem «Confirmar») | A recusa `COMPROMISSO_NAO_CONFIRMAVEL` não se provoca no ecrã, que não tem esse controlo; testada no backend |
| RF-088 Impedir a alteração de tomas a partir da agenda (Importante) | TS-W-F008-05 («só consulta») | — |
| RF-089 Identificar a ficha de cada evento (Essencial) | TS-W-F008-01, 06 | TS-W-F008-06 |
| RF-090 a RF-095 Ocorrências e notas (Essencial) | Pendente dos ecrãs da I073 (secção 6.4) | Pendente da I073 |
| RF-100 a RF-105 Lista diária (Essencial) | Pendente do ecrã da I075 (secção 6.4) | Pendente da I075 |
| RF-110, RF-111, RF-112 Notificar Cuidadores de toma não confirmada (Essencial) | TS-W-F011-04 (duplo) | A criação no momento certo é do processo periódico, testada no backend |
| RF-113 Notificar Familiares de toma Grave (Essencial) | TS-W-F011-04 (duplo) | Idem |
| RF-115 Notificar o registo de uma nova ocorrência (Essencial) | TS-W-F011-01 | TS-W-F011-02 (o autor não é notificado) |
| RF-116 Antecedência do aviso (Importante) | TS-W-F008-01, 03 | — |
| RF-117 Notificar antes de um compromisso (Essencial) | TS-W-F011-01 | O momento do aviso e o cancelamento ao remover (TS-W-F008-04) são do servidor, testados no backend |
| RF-118 Reenviar o e-mail após falha (Importante) | TS-W-F011-04 («E-mail pendente (reenvio em curso)», duplo) | — |
| RF-119 Identificar o e-mail não entregue (Importante) | TS-W-F011-04 (duplo) | — |
| RF-140 Consultar o histórico de notificações (Essencial) | TS-W-F011-01, 03 | TS-W-F011-02, 05 |
| RF-141 Notificação independente do e-mail (Essencial) | TS-W-F011-01, 04 (notificações com e-mail pendente ou não entregue) | Regra do servidor, testada no backend |
| RF-142 Suspender notificações de ficha arquivada (Essencial) | Regra do servidor (processo periódico), testada no backend | — |
| RF-120 Arquivar ficha (Essencial) | TS-W-F002-09 | TS-W-F004-07, TS-W-F005-09 (alterações indisponíveis) |
| RF-121 Reativar ficha (Essencial) | TS-W-F002-10 | TS-W-F002-09 (sem edição antes de reativar) |
| RF-122 Suspender tarefas, avisos e notificações de fichas arquivadas (Essencial) | Regra do servidor, testada no backend; no frontend, a ficha só de consulta (TS-W-F002-09) e as tomas por confirmar (TS-W-F006-08) | TS-W-F004-07 |
| RF-123 Eliminar ficha sem registos (Importante) | TS-W-F002-11 | TS-W-F002-12 |
| RF-124 Informar falha ao consultar documento (Importante) | TS-W-F002-05 | TS-W-F002-08 (duplo 503) |
| RF-130 Despromover Cuidador a Familiar (Essencial) | TS-W-F004-08 | TS-W-F004-10 (principal sem substituto) |
| RF-131 Remover acesso (Essencial) | TS-W-F004-09 | TS-W-F004-10 |
| RF-132 Transferir o papel de Cuidador principal (Essencial) | TS-W-F004-10 | TS-W-F003-03 (ficha própria: não se transfere) |
| RF-133 Exigir substituto antes de despromover ou remover o principal (Essencial) | TS-W-F004-10 (depois da transferência) | TS-W-F004-10 |
| RF-134 Ficha sempre com um Cuidador (Essencial) | TS-W-F004-10 | TS-W-F004-10 (apresentação da regra); a recusa `FICHA_SEM_CUIDADOR` é testada no backend |
| RF-135 Manter o convite quando o e-mail não é entregue (Importante) | Regra do servidor, testada no backend; o convite aparece em E14 sem depender do e-mail (TS-W-F004-02, 04) | — |
| RF-136 Reenviar e-mail de convite (Importante) | Regra do servidor, testada no backend (plano, secção 4.2); E13 informa o reenvio até 3 vezes | — |
| RNF-008 Formatos e tamanho dos documentos | TS-W-F002-05 | TS-W-F002-06, TS-W-F005-04 |
| RNF-012 Agenda num só pedido por vista | TS-W-F008-05 | — |
| RNF-021 Interações para confirmar uma toma | TS-W-F006-02 (um só clique, sem diálogo) | — |
| RNF-027 Estados sem dependência exclusiva da cor | TS-W-F006-01 a 05 (estado em texto e ícone); TS-W-F008-05 (tipo do evento e estado da toma); TS-W-F011-04 (tipo da notificação) | — |
| RNF-028 Mensagens de erro de validação | — | TS-W-F002-03, 04, 14, TS-W-F004-01, TS-W-F005-03, TS-W-F008-02 |
| RNF-054 Autenticação por token | TS-W-F001-03, TS-W-F001-07 | — |

Todos os requisitos funcionais de F001 a F006, F008 e F011 da secção 4.1 do plano têm cenário ou justificação (critério CS-W04); os de F009 e F010 ficam pendentes dos ecrãs da I073 e da I075 (secção 6.4). RF-060 e RF-066 são do servidor (plano, secção 4.2).

## 6. Dependências, tempo nas tomas e ficheiros de teste

### 6.1 Cenários que dependem da I068

Os cenários TS-W-F006-01 a TS-W-F006-08 testam o calendário de tomas da I068 (Eduardo Alves Santos), com os textos e o comportamento fixados no contrato de interface da F006 do Sprint 08, acordado entre a I068, a I069 e a I070: separador «Calendário de tomas» de E15 Medicação (`role="tab"`), sem rota nova, aberto diretamente com `/fichas/:fichaId/medicacao?vista=calendario` (TS-W-F006-02 a 08) ou pelo separador (TS-W-F006-01), período por omissão de ontem a hoje + 5 dias, botão «Confirmar toma» com um só clique, mensagens em `role="status"` e `role="alert"`. A branch da I070 assenta na da I068 e a I070 é integrada por último (ordem de merge I064, I066, I068, I070), com a pipeline verde.

### 6.2 Tempo nas tomas (risco RW-03 do plano)

O servidor usa a hora de Lisboa e a pipeline corre em UTC. As datas e as horas das precondições são calculadas na hora de Lisboa (`agoraLisboa` e `dataLisboa`, em `cypress/support/dados.ts`), nunca na hora do navegador:

O servidor só gera tomas a partir do momento em que a ficha foi criada (`Ficha.AtivaDesdeUtc`, RF-060) e, ao arquivar, remove as tomas futuras por confirmar (RF-122). Como cada cenário cria a sua ficha, não existem tomas passadas: uma toma «com atraso» só existiria mais de 30 minutos depois da hora prevista e uma toma «não confirmada» 24 horas depois. Por isso:

- «Confirmada» (TS-W-F006-02, 06, 07): toma de hoje às 23:59, contra o backend real. A janela de confirmação abre às 00:00 do dia previsto e a confirmação antes da hora prevista fica dentro da tolerância (estado `Confirmada`). O cenário só não funciona se começar no último minuto do dia (23:59 de Lisboa).
- Fora da janela (TS-W-F006-04): toma de amanhã, contra o backend real.
- «Confirmada com atraso» (TS-W-F006-03), «Não confirmada» (TS-W-F006-05) e a toma que ficou por confirmar numa ficha arquivada (TS-W-F006-08): duplos da OP-32 e da OP-33 (plano, secção 5.3: condição impossível de provocar no backend real sem esperar 30 minutos ou 24 horas), sobre uma toma real e com a ficha e o arquivo reais. A regra que decide o estado (tolerâncias, 24 horas, janela) é do servidor e está coberta pelos testes do backend; estes cenários verificam a apresentação do estado, as mensagens e a recarga da lista. `TOMA_NAO_CONFIRMAVEL` só se consegue mostrar assim, porque o botão «Confirmar toma» só aparece nas tomas pendentes (contrato da F006, secção 6).

Os tratamentos de F005 também usam a data de hoje em Lisboa.

### 6.3 Ficheiros de teste

O plano prevê ficheiros de teste em `cypress/fixtures/`. Os ficheiros são gerados em memória (`cypress/support/dados.ts`), para o PDF com mais de 10 MB não aumentar o repositório. As receitas e os documentos válidos são PDF: com o Cypress 13.17.0, um ficheiro binário (PNG ou JPG) escolhido com `selectFile` chega alterado ao servidor, que o recusa com `415`, quer seja criado em memória quer seja lido de `cypress/fixtures/`. O mesmo PNG é aceite quando enviado diretamente à API e pela aplicação no Chrome, pelo que se trata de uma limitação da ferramenta e não da aplicação. A aceitação de JPG e PNG fica coberta pelos testes do backend (RNF-008) e pela verificação manual da I066.

### 6.4 Dependências da I079 (Sprint 09)

Os cenários da F008 testam a agenda da I071 e os da F011 o histórico de notificações da I077. Foram verificados contra o backend publicado com as duas branches juntas à da I079: 68 de 68 cenários, incluindo os de F001 a F006. A branch da I079 assenta na da I070; depois do merge da I070, da I071 e da I077, é atualizada com a `main` e a frontend-web-ci volta a correr com todos os cenários.

A F009 (E23 e E24, I073) e a F010 (E04, I075) do frontend web ainda não estavam integradas nem disponíveis numa branch quando esta versão foi escrita. Os seus cenários (TS-W-F009 e TS-W-F010) entram nesta especificação e em `cypress/e2e/` quando esses ecrãs existirem, com o mesmo método: dados criados pela API, datas no calendário de Lisboa e duplos só para o que não se provoca no backend real.

Na F011, as notificações de tomas em atraso (RF-110 a RF-113) só existem 30 minutos a 2 horas depois da hora prevista, e o e-mail só fica `NaoEntregue` depois de 3 reenvios com 5 minutos de intervalo (RF-119). Esses estados usam o duplo TS-W-F011-04, sobre a mesma apresentação que os cenários reais TS-W-F011-01 a 03 já verificam.
