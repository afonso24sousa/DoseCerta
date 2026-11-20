# ESPECIFICAÇÃO DOS TESTES DE SISTEMA DO FRONTEND MOBILE

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m4 |
| Versão | v02 |
| Issue | I063 (Sprint 07); I070 (Sprint 08) |
| Artefacto da Checklist de Controlo de Artefactos | OF-M4-018 Especificação dos testes de sistema do frontend mobile |
| Pasta oficial | `03-artefactos-de-qualidade-de-produto/03.03-frontend-mobile/03.03.02-testes-de-sistema` |
| Plano de referência | `m4-plano-testes-frontend-mobile-v01.md` (I062) |
| Código de referência | Aplicação mobile da I058 e da I060 (na `main`); pipeline da I061 |

| Versão | Alterações |
|---|---|
| v01 | Primeira versão: cenários de sistema da F001 (RF-010 a RF-017), automatizados com `integration_test` em emulador Android |
| v02 | Cenários de F002 e F003 (I070), com a fonte de ficheiros falsa; F004 a F006 com justificação por requisito (secção 6.1), porque o código mobile dessas funcionalidades não está na `main`; rastreabilidade de todos os requisitos de F002 a F006; secção 6 (justificações e desvios ao plano) |

## 1. Âmbito

Esta especificação descreve os cenários de sistema da aplicação mobile, segundo o plano de testes (secções 4.1 e 5). A v01 cobriu a F001. A v02 acrescenta a F002 Fichas de utentes e a F003 Ficha própria (ecrãs E05 a E11) e justifica, requisito a requisito, a F004 a F006 (secção 6.1): os ecrãs mobile dessas funcionalidades (I065, I067 e I069) ainda não estão na `main`, onde correm os cenários da frontend-mobile-ci. Os cenários são os mesmos do frontend web (`m4-especificacao-testes-sistema-frontend-web-v02.md`), adaptados à aplicação.

## 2. Execução

| Elemento | Definição |
|---|---|
| Ferramenta | `integration_test` (SDK do Flutter) |
| Código dos testes | `frontend-mobile/integration_test/f001_test.dart` (F001) e `f002_f003_test.dart` (F002 e F003); apoio em `integration_test/apoio.dart` (preparação pela API, fonte de ficheiros falsa, ações comuns) |
| Endereço da API | `--dart-define=URL_API=<raiz do servidor>`, o nome que a aplicação lê em `ConfiguracaoApi`; na frontend-mobile-ci, `http://10.0.2.2:<porta>` (só aceite em builds debug no emulador) |
| Emulador | Android API 34, x86_64, `google_apis`, animações desativadas (frontend-mobile-ci, QG-07) |
| Backend | API real (`DoseCerta.Api`) e SQL Server 2022 criados e destruídos em cada execução da frontend-mobile-ci |
| Localização dos elementos | Rótulo do campo (`InputDecoration.labelText`), texto e tipo do botão, dica da entrada da barra inferior; nunca a posição |
| Esperas | Espera ativa pelo elemento (`esperarPor`, no máximo 30 segundos); sem esperas fixas |
| Independência | Cada cenário apaga a sessão guardada e arranca a aplicação de novo. A sessão fica num armazenamento em memória (`ArmazenamentoMemoria`, em `apoio.dart`), entregue ao `InicializadorApp` no lugar do `FlutterSecureStorage`: no emulador da pipeline, o armazenamento seguro do Android bloqueava de vez em quando no arranque de um cenário e encravava o QG-07 (execução 23 do PR #112; PR #121). O código de produção não muda; a gravação e a leitura da sessão ficam cobertas pelos testes da I058 (`test/core/sessao_test.dart`) |
| Comando local | `flutter test integration_test --dart-define=URL_API=http://10.0.2.2:5180`, com emulador e API ativos |
| Quality gate | QG-07: 100% dos cenários passam. A falha de um cenário termina a pipeline sem sucesso |

**Desvio ao plano (secção 5.3 do plano mobile):** o plano prevê, na pipeline, um cliente HTTP falso. A frontend-mobile-ci da I061 já arranca a API real e o emulador chega-lhe por `10.0.2.2`, que a aplicação aceita em debug. Por isso estes cenários correm contra o backend real, o que já cumpre o «subconjunto contra o backend» do plano para a F001. O cliente falso fica para os cenários de falhas difíceis de provocar (falta de ligação, erro 500), em versões seguintes.

## 3. Dados de teste

- Só dados fictícios: nomes «Ana Fictícia», «Bruno Fictício» e «Carla Fictícia»; e-mails `<prefixo>.<microssegundos>@exemplo.pt`, únicos por execução.
- As contas das precondições são criadas pela API (`POST /api/v1/auth/registo`) no início de cada cenário.
- Palavras-passe fictícias que cumprem RNF-003 (`Teste2026a1`, `Nova2026b2`), válidas só na base de dados efémera da pipeline.
- Nos cenários de F002 e F003, as contas, as fichas, as associações e os tratamentos das precondições são criados pela API (OP-01, OP-02, OP-09, OP-19, OP-21, OP-27), com as funções de `integration_test/apoio.dart`.
- Os ficheiros escolhidos (um PDF mínimo e um `.txt`) são gerados em memória e entregues pela fonte de ficheiros falsa (secção 6.2).
- As datas das precondições são calculadas no calendário de Lisboa (`dataLisboa`, em `apoio.dart`), que é o do servidor; o emulador e a pipeline correm em UTC.

## 4. Cenários

| ID | Requisito | Precondições | Dados fictícios | Passos | Resultado esperado |
|---|---|---|---|---|---|
| TS-M-F001-01 | RF-010 Criar conta | Aplicação sem sessão; e-mail ainda não registado | Nome «Ana Fictícia»; e-mail `registo.<carimbo>@exemplo.pt`; palavra-passe `Teste2026a1` | 1. No Início de sessão, tocar em «Criar conta». 2. Preencher Nome, E-mail e Palavra-passe. 3. Tocar em «Criar conta» | A aplicação volta ao Início de sessão com a mensagem «Conta criada. Inicie sessão.» |
| TS-M-F001-02 | RF-011 Impedir conta com e-mail já registado | Sem sessão; conta criada pela API com o mesmo e-mail | Nome «Bruno Fictício»; e-mail `repetido.<carimbo>@exemplo.pt` | 1. Abrir o Registo. 2. Preencher com o e-mail já registado. 3. Tocar em «Criar conta» | A aplicação fica no Registo e mostra «Já existe uma conta com este e-mail.» |
| TS-M-F001-03 | RF-012 Iniciar sessão (credenciais válidas) | Conta criada pela API; sem sessão | E-mail `login.<carimbo>@exemplo.pt`; palavra-passe `Teste2026a1` | 1. Preencher E-mail e Palavra-passe. 2. Tocar em «Entrar» | A aplicação abre a área autenticada (barra inferior visível) e deixa o Início de sessão |
| TS-M-F001-04 | RF-012 Iniciar sessão (credenciais inválidas) | Conta criada pela API; sem sessão | E-mail `invalido.<carimbo>@exemplo.pt`; palavra-passe errada `Errada123` | 1. Preencher o e-mail certo e a palavra-passe errada. 2. Tocar em «Entrar» | Aparece «E-mail ou palavra-passe incorretos.»; a barra inferior não aparece |
| TS-M-F001-05 | RF-013 Consultar dados da conta; RF-014 Atualizar nome e e-mail | Conta criada pela API; sessão iniciada | Nome novo «Carla Fictícia»; e-mail novo `conta-nova.<carimbo>@exemplo.pt` | 1. Tocar em «Conta» na barra inferior. 2. Confirmar o nome e o e-mail atuais. 3. Substituir o Nome e o E-mail. 4. Tocar em «Guardar dados» | Os campos mostram os dados atuais; aparece «Dados guardados.»; os campos ficam com o nome e o e-mail novos |
| TS-M-F001-06 | RF-015 Alterar palavra-passe | Conta criada pela API; sessão iniciada | Palavra-passe atual `Teste2026a1`; nova `Nova2026b2` | 1. Abrir a Conta. 2. Preencher «Palavra-passe atual» e «Nova palavra-passe». 3. Tocar em «Alterar palavra-passe». 4. Tocar em «Terminar sessão». 5. Iniciar sessão com a nova palavra-passe | Aparece «Palavra-passe alterada.»; o início de sessão com a nova palavra-passe abre a área autenticada |
| TS-M-F001-07 | RF-016 Terminar sessão; RF-017 Exigir sessão iniciada | Conta criada pela API; sessão iniciada | E-mail `logout.<carimbo>@exemplo.pt` | 1. Abrir a Conta. 2. Tocar em «Terminar sessão». 3. Reabrir a aplicação | A aplicação mostra o Início de sessão; ao reabrir, continua no Início de sessão, sem barra inferior (a sessão não ficou guardada) |
| TS-M-F002-01 | RF-020 Criar ficha de terceiro; RF-021; RF-022 Carregar documentos; RF-024 Criador como Cuidador principal (UC-05; E05, E06, E07, E08) | Conta criada pela API; fonte falsa com `receita-ficticia.pdf` | Ficha «Maria Fictícia Santos», 17/05/1941, `+351 910 000 010` | 1. Fichas → «Nova ficha». 2. «Ficha de uma terceira pessoa» e a identificação; «Seguinte». 3. «Eu sou o Cuidador principal». 4. «Carregar: Receita médica» → «Galeria». 5. «Criar ficha» | O documento aparece em E07; a ficha é criada e abre E08 com o nome; a fonte recebeu o pedido da galeria |
| TS-M-F002-02 | RF-025 Convidar outra pessoa para Cuidador principal (UC-06; E06, E07) | Conta criada pela API | Ficha «João Fictício Lopes»; e-mail `principal.<carimbo>@exemplo.pt` | 1. E06 de terceiro; «Seguinte». 2. «Convidar outra pessoa…» e o e-mail. 3. «Criar ficha» | Abre E08; OP-18 mostra o criador como Cuidador principal provisório |
| TS-M-F002-03 | RF-021; RNF-028 Mensagens de erro de validação (E06) | Conta criada pela API | Data de nascimento 01/01/2099; contacto `+351 910 000 011`; nome vazio | 1. E06 de terceiro sem nome e com data futura. 2. «Seguinte» | «Nome completo: é obrigatório.» e o erro da data junto aos campos; a data e o contacto escritos mantêm-se; não avança para E07 |
| TS-M-F002-04 | RF-029 Editar ficha (UC-07; E08, E09) | Ficha criada pela API | Nome novo «Rosa Fictícia Pereira» | 1. Abrir a ficha; «Editar». 2. Alterar o nome; «Guardar» | Volta a E08 com o nome novo |
| TS-M-F002-05 | RF-022; RNF-008 Formatos e tamanho dos documentos (UC-09; E08, E10) | Ficha criada pela API; fonte falsa com `notas-ficticias.txt` e `relatorio-ficticio.pdf` | — | 1. Menu «Documentos». 2. «Carregar: Documento a acrescentar» → «Relatório clínico» → «Ficheiro» (o `.txt`). 3. Repetir com o PDF | O `.txt` é recusado antes do envio («O formato do ficheiro não é suportado…») e não aparece; o PDF é carregado e aparece na lista |
| TS-M-F002-06 | RF-120 Arquivar ficha; RF-121 Reativar ficha (UC-10, UC-11; E08, E11) | Ficha criada pela API | Ficha «Lúcia Fictícia» | 1. «Arquivar ou eliminar» → «Arquivar» e confirmar. 2. «Reativar ou eliminar» → «Reativar» e confirmar | «Ficha arquivada.» e sem «Editar»; depois da reativação, «Editar» volta a aparecer |
| TS-M-F002-07 | RF-123 Eliminar ficha sem registos (recusa com histórico) (UC-12; E11) | Ficha com um tratamento registado pela API | Ficha «Teresa Fictícia» | 1. «Arquivar ou eliminar» → «Eliminar» e confirmar | «A ficha tem medicação ou histórico e não pode ser eliminada. Arquive-a.»; a ficha mantém-se (OP-10 responde 200) |
| TS-M-F002-08 | RF-028 Consultar ficha; RF-048 Restringir o Familiar a consulta (E08, E10) | Familiar associado pela API (OP-19, OP-21) | Ficha «Helena Fictícia»; Familiar «Filipe Fictício» | 1. O Familiar abre a ficha. 2. Menu «Documentos» | E08 sem «Editar» nem «Arquivar ou eliminar»; E10 sem «Carregar» |
| TS-M-F003-01 | RF-030 Criar ficha própria (UC-05; E06, E07) | Conta sem ficha própria | Ficha «Ana Fictícia» | 1. «Gerir a minha própria saúde» e a identificação; «Seguinte». 2. «Criar ficha» | E07 sem o passo do Cuidador principal; OP-07 mostra a ficha como própria e o titular como principal |
| TS-M-F003-02 | RF-031 Limitar a uma ficha própria (E06) | Ficha própria criada pela API | — | 1. Abrir E06 | «Já tem uma ficha própria. Só pode ter uma.» |
| TS-M-F004 a TS-M-F006 | RF-026, RF-027, RF-032 a RF-034, RF-040 a RF-049, RF-130 a RF-134 (F004); RF-050 a RF-057 (F005); RF-061 a RF-068 (F006) | — | — | — | Sem cenários de sistema mobile nesta versão: justificação por requisito na secção 6.1 |

## 5. Rastreabilidade

| Requisito | Cenários ou justificação |
|---|---|
| RF-010 a RF-017 | Como na v01: TS-M-F001-01 a TS-M-F001-07 |
| RF-020 Criar ficha de terceiro | TS-M-F002-01 |
| RF-021 Registar necessidades específicas | TS-M-F002-01, TS-M-F002-03 |
| RF-022 Carregar documentos na ficha | TS-M-F002-01, TS-M-F002-05 |
| RF-023 Recusar a operação quando o documento não é guardado | Não se provoca no backend real; coberto pelos testes de widgets da I060 (`fichas_e06_e07_test.dart`, E09 e E10, com o servidor falso) e, no frontend web, por TS-W-F002-07 |
| RF-024 Criador como Cuidador principal | TS-M-F002-01 |
| RF-025 Convidar outra pessoa para Cuidador principal | TS-M-F002-02 |
| RF-026, RF-027 | Justificação J1 (secção 6.1) |
| RF-028 Consultar ficha | TS-M-F002-01, TS-M-F002-08 |
| RF-029 Editar ficha | TS-M-F002-04 |
| RF-030 Criar ficha própria | TS-M-F003-01 |
| RF-031 Limitar a uma ficha própria | TS-M-F003-02 |
| RF-032 a RF-034 | Justificação J1 (secção 6.1) |
| RF-040, RF-042 a RF-049 | Justificação J1 (secção 6.1) |
| RF-041, RF-135, RF-136 | Regras do servidor, testadas no backend (plano mobile, secção 4.2); só a apresentação, com a F004 mobile |
| RF-050 a RF-057 | Justificação J2 (secção 6.1) |
| RF-061 a RF-068 | Justificação J3 (secção 6.1) |
| RF-120 Arquivar ficha | TS-M-F002-06 |
| RF-121 Reativar ficha | TS-M-F002-06 |
| RF-122 Suspender tarefas, avisos e notificações | A suspensão é regra do servidor, testada no backend; a ficha só de consulta verifica-se em TS-M-F002-06 |
| RF-123 Eliminar ficha sem registos | TS-M-F002-07 (recusa com histórico); a eliminação sem histórico fica coberta pelo teste de widgets `fichas_e11_test.dart` e, no frontend web, por TS-W-F002-11 |
| RF-124 Informar falha ao consultar documento | Não se provoca no backend real; coberto pelo teste de widgets `fichas_e10_test.dart` e, no frontend web, por TS-W-F002-08 |
| RF-130 a RF-134 | Justificação J1 (secção 6.1) |
| RNF-008 Formatos e tamanho dos documentos | TS-M-F002-05 |
| RNF-028 Mensagens de erro de validação | TS-M-F002-03 |

Todos os requisitos de F001 a F006 da secção 4.1 do plano mobile têm cenário ou justificação (critério CS-M04).

## 6. Justificações e desvios ao plano

### 6.1 F004 a F006: justificação por requisito

Os cenários de sistema mobile correm na frontend-mobile-ci sobre o código da branch, que parte da `main`. Os ecrãs mobile da F004 (I065, PR #124), da F005 (I067, PR #123) e da F006 (I069, PR #126) ainda não estão na `main`: estão em revisão, em branches separadas. Escrever aqui os cenários obrigava a juntar essas três branches, ainda não revistas, à da I070, e a I070 passaria a depender do merge delas. Por isso, nesta versão, cada requisito fica coberto da forma seguinte, que juntas verificam o mesmo comportamento de ponta a ponta:

1. **Cenário de sistema web contra o backend real** (especificação web v02): o servidor, os códigos de erro e o contrato de cada operação são os mesmos para os dois frontends.
2. **Testes de widgets da aplicação mobile**, na Issue que implementa o ecrã, com o repositório falso: verificam no ecrã mobile os textos, as ações por papel, as recusas e a recarga.
3. **Regras do servidor** cobertas pelos testes do backend (plano mobile, secção 4.2).

| Ref. | Requisitos | Cenários web (backend real) | Testes de widgets mobile | Servidor |
|---|---|---|---|---|
| J1 | F004: RF-026, RF-027 (convite para Cuidador principal); RF-032 a RF-034 (titular da ficha própria); RF-040, RF-042 a RF-049 (convidar, convites recebidos, aceitar, recusar, Familiar só consulta); RF-130 a RF-134 (promover, despromover, remover, transferir o principal, principal sem substituto) | TS-W-F003-03, TS-W-F004-01 a 12 | I065: `test/features/pessoas_e12_test.dart` (E12), `pessoas_e13_test.dart` (E13), `pessoas_e14_test.dart` (E14), `fichas_e08_test.dart` (ligações em E08) e `test/data/repositorios_pessoas_convites_test.dart` (OP-18 a OP-26) | RF-041, RF-135, RF-136 (e-mail e notificação do convite) |
| J2 | F005: RF-050 a RF-057 (registar, validações, receita, receita não guardada, consultar, atualizar, terminar, histórico) | TS-W-F005-01 a 10 | I067: `test/features/medicacao_e15_test.dart`, `medicacao_e16_test.dart`, `medicacao_e17_test.dart`, `medicacao_acessibilidade_test.dart` (RNF-025, RNF-026) e `test/data/repositorios_tratamentos_test.dart` (OP-27 a OP-31) | Geração das tomas (RF-060) |
| J3 | F006: RF-063 a RF-065, RF-067, RF-068 (calendário, confirmar, autor e momento, com atraso, não confirmada); RF-061, RF-062 (sincronização OP-34 e aviso local, só mobile) | TS-W-F006-01 a 08 | I069: `test/features/tomas_e15_calendario_test.dart` (calendário e confirmação), `test/avisos_locais/avisos_locais_test.dart` e `observador_avisos_locais_test.dart` (sincronização, agendamento e cancelamento dos avisos), `test/data/repositorios_tomas_test.dart` (OP-32 a OP-34) | RF-066 (tolerâncias), RF-068 (processo periódico) |

Os cenários TS-M-F004 a TS-M-F006, com os mesmos casos dos cenários web e os textos do contrato de interface da F006, ficam propostos para o Backlog Refinement do Sprint 09, para serem implementados depois do merge da I065, da I067 e da I069 (por exemplo, com a I079).

### 6.2 Seletores de ficheiros (desvio ao plano)

O `integration_test` não opera a câmara, a galeria nem o seletor de ficheiros do sistema. Os cenários substituem o `selecionadorFicheirosProvider` por um `SelecionadorFicheiros` com uma fonte falsa (`FonteFicticia`, em `integration_test/apoio.dart`), que devolve os ficheiros fictícios indicados no cenário. A substituição é feita no `ProviderScope` do próprio teste: o código de produção (`lib/`) não muda. O resto do percurso (validação prévia, envio `multipart`, resposta do servidor real) é o da aplicação. Os seletores nativos ficam cobertos pela verificação manual da I060 no emulador.

### 6.3 Avisos locais (I069)

Um aviso agendado no sistema operativo não se verifica de forma fiável no emulador. Quando os cenários da F006 forem implementados, substituem o serviço de avisos locais por um falso, pelo provedor, e verificam o agendamento (hora e texto) e o cancelamento.
