# PLANO DE TESTES DO FRONTEND MOBILE

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m4 |
| Versão | v01 |
| Issue | I062 (Sprint 07) |
| Artefacto da Checklist de Controlo de Artefactos | OF-M4-017 Plano de testes do frontend mobile |
| Código de referência | Estrutura base da aplicação mobile da Issue I058 `<commit a confirmar>`; backend na `main`, commit `ed0faf1` |
| Documentos complementares | Especificação dos testes de sistema (OF-M4-018) e especificação dos testes de aceitação (OF-M4-020) do frontend mobile |

| Versão | Alterações |
|---|---|
| v01 | Primeira versão: âmbito, níveis e técnicas de teste, ambiente, dados, critérios de entrada, saída, suspensão e retoma, quality gates, entregáveis, responsabilidades, calendário e riscos dos testes do frontend mobile em m4 |

## 1. Finalidade e âmbito

Este documento define, antes da implementação dos testes, como é verificada a aplicação mobile do DoseCerta na milestone m4: o que se testa e o que fica de fora, a que níveis, com que técnicas, ferramentas, dados e ambientes, com que critérios se decide que os testes terminaram com sucesso e quem faz o quê. Os cenários concretos não estão aqui: são descritos na especificação dos testes de sistema (OF-M4-018) e na especificação dos testes de aceitação (OF-M4-020), que seguem este plano.

O objeto de teste é a aplicação Android em Flutter 3 (`frontend-mobile/`) nas funcionalidades F001 a F011, sem a F007, retirada do âmbito por alteração autorizada de âmbito, e só na parte que pertence à aplicação mobile: os ecrãs, a navegação, as validações prévias, a apresentação das respostas e dos erros da API, a sessão, a sincronização das tomas, o aviso local da toma e o comportamento sem ligação. As regras do servidor foram testadas no backend em m3 (`m3-plano-testes-backend-v01.md`, Issues I048 e I049) e não se repetem aqui. O frontend web tem um plano próprio (`m4-plano-testes-frontend-web-v01.md`), com a mesma abordagem; este plano descreve sobretudo o que é diferente na aplicação mobile.

## 2. Referências

| Referência | Utilização |
|---|---|
| Especificação de Requisitos `m3-especificacao-requisitos-v02.md` | Requisitos cujo campo «Componentes abrangidos» inclui o frontend mobile; critérios de aceitação (Dado / Quando / Então); condições de referência CR1 a CR6 |
| Especificações de Casos de Uso `m3-especificacoes-casos-uso-v02.md` | Fluxos principais e alternativos, base dos cenários de sistema e de aceitação |
| Modelo de Classes do Frontend Mobile `m3-modelo-classes-frontend-mobile-v02.md` | Ecrãs, navegação e serviços; tomas sincronizadas e aviso local (`avisos_locais`, secção 7); tabela de requisitos cobertos e não cobertos pela aplicação mobile (secção 13) |
| Protótipo de Baixa Fidelidade `m3-prototipo-baixa-fidelidade-v02.pdf` | Ecrãs da aplicação mobile, campos, textos e navegação esperados |
| Matriz de Permissões `m3-matriz-permissoes-ficha-v02.md` | O que cada papel vê e pode fazer em cada ecrã; base das tabelas de decisão |
| Documentação da API `m3-documentacao-api-v03.md` (Issue I042) | Respostas e códigos de erro que a aplicação apresenta; dados do calendário de tomas (OP-34) |
| Descrição da Pipeline CI/CD `docs/ci-cd/m3-descricao-pipeline-ci-cd-v03.md` (Issue I050) | Quality gates do backend e indicação de que os gates dos frontends entram em m4 |
| Plano de Testes do Backend `m3-plano-testes-backend-v01.md` (Issue I048) | Modelo deste plano e regras do servidor já verificadas |
| Regulamento Interno do Grupo, secções 10, 11.2 a 11.6 e 14.2 | Ferramentas, quality gates QG-07, QG-10 e QG-11, cobertura «Não aplicável» nos frontends, segredos |
| Regulamento de Funcionamento da UC, secções 16 a 19 | Organização dos artefactos e conteúdo mínimo do plano de testes (tabela final) |

## 3. Itens de teste

| Item | Localização | Descrição |
|---|---|---|
| Aplicação Flutter | `frontend-mobile/lib` `<organização das pastas a confirmar com o modelo de classes mobile v02 e a I058>` | Ecrãs, navegação, estado da sessão, acesso à API e validações prévias |
| Tomas sincronizadas e aviso local | Componente `avisos_locais` (modelo de classes mobile v02, secção 7) | Sincronização das tomas das 48 horas seguintes (OP-34), agendamento e cancelamento dos avisos locais e pedido de autorização das notificações |
| Pacote de instalação | APK de depuração (`flutter build apk --debug`) | Aplicação instalada no emulador ou no dispositivo de referência |

A versão de referência de cada execução é o commit testado. Os testes acompanham o código: cada Pull Request que altera a aplicação mobile corre as suites da secção 5.5.

## 4. Funcionalidades a testar e excluídas

### 4.1 A testar

Os requisitos abaixo são os 113 cujo campo «Componentes abrangidos» inclui o frontend mobile (90 funcionais e 23 não funcionais). Os identificadores dos cenários são atribuídos nas especificações OF-M4-018 e OF-M4-020. Antes de cada especificação, a lista é cruzada com a secção 13 do modelo de classes do frontend mobile.

| Funcionalidade | Requisitos funcionais | Ecrãs | Tipo de teste |
|---|---|---|---|
| F001 Autenticação e gestão de conta | RF-010 a RF-017 (8) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F002 Fichas de utentes | RF-020 a RF-029, RF-120 a RF-124 (15) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F003 Ficha própria | RF-030 a RF-034 (5) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F004 Associação de Cuidadores e Familiares | RF-040 a RF-049, RF-130 a RF-136 (17) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F005 Registo de medicação | RF-050 a RF-057 (8) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F006 Horários, avisos locais e confirmação de toma | RF-061 a RF-065, RF-067, RF-069 (7) | `<a confirmar com o protótipo v02>` | Sistema e aceitação; aviso local também em dispositivo (secção 5.8) |
| F008 Agenda e compromissos | RF-080 a RF-089 (10) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F009 Ocorrências e notas partilhadas | RF-090 a RF-095 (6) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F010 Lista diária consolidada | RF-100 a RF-102, RF-104, RF-105 (5) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F011 Notificações | RF-110 a RF-113, RF-115 a RF-117, RF-119, RF-140 (9) | `<a confirmar com o protótipo v02>` | Sistema (apresentação) e aceitação |

| Requisitos não funcionais | Como são verificados na aplicação mobile |
|---|---|
| RNF-002 Comunicação cifrada | Sistema e inspeção: todos os pedidos usam o endereço base HTTPS; a aplicação não permite tráfego sem cifra |
| RNF-003 Palavra-passe | Sistema: partições e valores-limite da validação prévia; o servidor continua a validar |
| RNF-008 Formatos e tamanho dos documentos | Sistema: PDF, JPG e PNG aceites; outros formatos e ficheiros acima de 10 MB recusados com a mensagem da API |
| RNF-010 a RNF-014, RNF-016 e RNF-017 Tempos | Medição (secção 9) |
| RNF-020 Língua da interface | Sistema e inspeção: textos, mensagens e avisos locais em português europeu |
| RNF-021 Interações para confirmar uma toma | Sistema: contagem das interações a partir da lista diária |
| RNF-022 e RNF-023 Eficácia e satisfação | Simulação dos testes de aceitação com utilizadores (secção 5.6) |
| RNF-025 Alvos de toque de 48 × 48 dp | Sistema: verificação automática das áreas de toque em cada ecrã (por exemplo com a diretriz `androidTapTargetGuideline` do Flutter) |
| RNF-026 Tamanho de letra a 150% | Sistema: cada ecrã com o fator de escala do texto a 1,5, sem conteúdo cortado nem sobreposto |
| RNF-027 Estados sem dependência exclusiva da cor | Sistema: cada estado da toma tem texto ou ícone |
| RNF-028 Mensagens de erro de validação | Sistema: o erro aparece junto ao campo, com a regra, e os dados escritos mantêm-se |
| RNF-029 Formatos de data e hora | Sistema: datas e horas em hora de Lisboa, no formato da especificação |
| RNF-033 Indicação de ausência de ligação | Sistema, com falha de rede simulada: aviso em no máximo 10 segundos |
| RNF-041 Precisão do aviso local | Medição em dispositivo (secção 5.8 e secção 9) |
| RNF-054 Autenticação por token | Sistema: token JWT enviado nos pedidos autenticados, guardado de forma segura e apagado ao terminar sessão |
| RNF-056 Tecnologia | Inspeção do `pubspec.yaml` (Dart 3 e Flutter 3, para Android) |

### 4.2 Excluídas

| Exclusão | Justificação |
|---|---|
| F007 (tarefas de cuidados recorrentes) | Retirada do âmbito por alteração autorizada de âmbito |
| Requisitos só do backend ou da base de dados: RF-060, RF-066, RF-068, RF-118, RF-141, RF-142, RNF-001, RNF-004 a RNF-007, RNF-009, RNF-015, RNF-018, RNF-030 a RNF-032, RNF-034, RNF-040, RNF-042 a RNF-053 e RNF-057 | Não têm parte na aplicação mobile; foram verificados no backend em m3 (I048, I049) |
| Requisitos só do frontend web: RNF-019, RNF-024 e RNF-055 | Verificados no plano do frontend web |
| Regras do servidor por trás dos requisitos da aplicação (por exemplo a geração das tomas, a deteção dos atrasos, o envio e o reenvio de e-mails, a autorização por papel no servidor) | Já testadas no backend; na aplicação verifica-se só a apresentação, a navegação, a sincronização e os avisos locais |
| iOS e versões de Android anteriores à 12 | A condição de referência CR4 fixa Android 12 ou superior; RNF-056 fixa a aplicação para Android |
| Testes de carga | Requisito do servidor (RNF-015), fora da aplicação |

## 5. Abordagem

### 5.1 Níveis de teste

| Nível | Objetivo | Objeto e isolamento | Ferramentas | Quality gate | Obrigatório |
|---|---|---|---|---|---|
| Sistema | Verificar cada funcionalidade de ponta a ponta na aplicação instalada: ecrãs, navegação, validações, sincronização, avisos locais e apresentação dos resultados e dos erros | Aplicação completa num emulador Android; API substituída por um cliente HTTP falso com as respostas da documentação da API (secção 5.3); um subconjunto corre contra o backend publicado | `integration_test` em emulador Android | QG-07 | Sim |
| Aceitação (simulação) | Confirmar com utilizadores que os critérios de aceitação dos requisitos estão cumpridos e medir a eficácia e a satisfação (RNF-022, RNF-023) | APK instalado no dispositivo de referência (CR4), contra o ambiente de demonstração (CR1), com dados fictícios | Guião de cenários, registo de resultados, questionário SUS | Não é gate; é o OF-M4-021 | Sim |
| Unitário e de widgets | Verificar isoladamente serviços, validadores e widgets | Classes e widgets da aplicação, sem dispositivo nem rede | `flutter_test` | Nenhum | Não: opcional, sem limiar de cobertura (secção 5.7) |
| Análise estática | Detetar defeitos e desvios das regras de codificação e de formatação | Todo o código Dart | `flutter analyze --fatal-warnings`, `dart format` | QG-11 | Sim |
| Build | Garantir que a aplicação compila e gera o pacote | Projeto completo | `flutter build apk --debug` | QG-10 | Sim |

### 5.2 Técnicas de conceção

| Técnica | Onde se aplica | Exemplo |
|---|---|---|
| Partições de equivalência | Campos de cada formulário: válido, vazio, formato errado, valor rejeitado pelo servidor | E-mail válido, sem `@` e já registado (RF-011) |
| Valores-limite | Palavra-passe (7 e 8 caracteres, sem letra, sem algarismo; RNF-003), antecedência dos compromissos (30, 60, 120, 1440 e 2880 minutos), tamanho dos documentos (10 MB e acima; RNF-008), janela de sincronização de 48 horas (RF-061), desvio de 60 segundos do aviso local (RNF-041) | Toma às 48 h exatas e às 48 h e 1 min |
| Tabelas de decisão | Papel na ficha (Cuidador principal, Cuidador, Familiar, não associado) e estado da ficha (ativa, arquivada) contra as ações visíveis em cada ecrã, segundo a matriz de permissões | Ficha arquivada sem avisos locais (RF-069) |
| Transição de estados | Sessão (sem sessão, ativa, expirada, terminada), toma (pendente, confirmada, confirmada com atraso, não confirmada) e aviso local (agendado, emitido, cancelado) | Confirmar a toma cancela o aviso local ainda por emitir `<a confirmar com o modelo de classes mobile v02>` |
| Casos de uso | Fluxos principais e alternativos das especificações de casos de uso | Iniciar sessão, sincronizar, receber o aviso, confirmar a toma |
| Injeção de falhas | Falta de ligação, erro interno, sessão recusada, notificações não autorizadas | Sem rede, a aplicação mostra o aviso de ausência de ligação (RNF-033) e mantém os avisos já agendados |

### 5.3 Duplos de teste

Na pipeline, os testes de sistema correm no emulador com a API substituída por um cliente HTTP falso, injetado no arranque da aplicação de teste, que devolve as respostas e os códigos de erro da documentação da API v03. Esta escolha deve-se a três razões: o emulador não confia no certificado de desenvolvimento do backend local, o RNF-002 não permite usar HTTP sem cifra, e o resultado fica determinístico e independente da rede. Para garantir que o cliente falso não se afasta da API real, um subconjunto de cenários (pelo menos um por funcionalidade) corre também contra o backend publicado antes da simulação dos testes de aceitação, com contas fictícias criadas para essa execução. O relógio e a falta de rede são controlados no cliente falso e no emulador.

### 5.4 Regras de escrita dos testes

As regras são as do plano do frontend web (secção 5.4), adaptadas ao Flutter: cada cenário em Dado / Quando / Então com os requisitos, os casos de uso e os ecrãs que verifica; cenários independentes, com os seus próprios dados; elementos localizados por chaves estáveis (`Key`) ou pelo texto e pela semântica, nunca pela posição; esperas pelo estado da interface (`pumpAndSettle` ou espera por um elemento), sem esperas fixas; só dados fictícios; nenhum cenário desativado; o código dos testes passa pelo QG-11.

### 5.5 Regressão

A pipeline do frontend mobile (Issue I061) corre a build, a análise estática e todos os cenários `integration_test` no emulador em cada Pull Request para a `main` que altere `frontend-mobile/**` e em cada integração na `main`. A suite completa é a suite de regressão. O subconjunto contra o backend publicado e as medições em dispositivo são repetidos antes da simulação dos testes de aceitação e sempre que uma correção afete a sincronização ou os avisos locais.

### 5.6 Simulação dos testes de aceitação

| Elemento | Definição |
|---|---|
| Cenários | Escritos na especificação OF-M4-020 a partir dos critérios de aceitação (Dado / Quando / Então) dos requisitos funcionais da secção 4.1, agrupados pelas tarefas-chave do método de verificação de RNF-022 `<tarefas-chave a confirmar na especificação de requisitos>` |
| Participantes | `<número e perfil a confirmar>`; pessoas que não desenvolveram a aplicação, com contas fictícias criadas para a sessão. Cada participante é identificado no relatório só por um código (P01, P02, …) |
| Execução | No dispositivo de referência (CR4), contra o ambiente de demonstração (CR1); cada participante executa as tarefas sem ajuda; um observador regista o resultado |
| Registo | Participantes, cenários executados, resultado de cada um (cumprido ou não cumprido), desvios observados, tempos, evidências (capturas do ecrã) e conclusão |
| Critérios | RNF-022: pelo menos 80% dos participantes concluem cada tarefa-chave sem ajuda e à primeira tentativa. RNF-023: pontuação média da System Usability Scale igual ou superior a 68 |
| Relatório | OF-M4-021 Relatório da simulação dos testes de aceitação do frontend mobile |

Se o grupo fizer a mesma sessão com os dois frontends, os participantes e o questionário podem ser partilhados, mas os resultados são registados em separado no OF-M4-015 e no OF-M4-021.

### 5.7 Testes unitários e de widgets

O Regulamento Interno do Grupo (secção 11.5) indica que os testes unitários e a cobertura dos frontends são «Não aplicável» como exigência: o grupo concentra o esforço nos testes de sistema. Os testes com `flutter_test` são, por isso, opcionais, não têm limiar de cobertura e não são gate da pipeline. Se forem escritos, correm na pipeline e seguem as regras da secção 5.4.

### 5.8 Sincronização, aviso local, ausência de rede e retoma

| Aspeto | Requisitos | Como se verifica |
|---|---|---|
| Sincronização das tomas das 48 horas seguintes | RF-061, RNF-017 | Sistema: depois de iniciar sessão e ao abrir a aplicação, as tomas devolvidas por OP-34 dão origem aos avisos locais; tomas fora das 48 horas não são agendadas. Medição do tempo em dispositivo (secção 9) |
| Aviso local da toma prevista | RF-062 | Sistema: os avisos ficam agendados para a hora prevista (consulta dos avisos pendentes no emulador). Em dispositivo, emissão real com a aplicação fechada |
| Precisão do aviso | RNF-041 | Medição em dispositivo CR4, com as notificações autorizadas, sem ligação à Internet e com o ecrã bloqueado: diferença entre a hora prevista e a hora de apresentação do aviso, no máximo 60 segundos |
| Ficha arquivada | RF-069 | Sistema: depois de arquivar a ficha e sincronizar, os avisos das suas tomas deixam de estar agendados |
| Autorização das notificações | RF-062 | Sistema: pedido de autorização na primeira utilização; com a autorização recusada, a aplicação informa o Utilizador e continua a funcionar |
| Ausência de rede e retoma | RNF-033 | Sistema: sem rede, aviso de ausência de ligação em no máximo 10 segundos, sem perda dos avisos já agendados; com a rede de volta, a sincronização seguinte atualiza os avisos |

## 6. Ambiente e ferramentas

| Elemento | Local (computador de cada aluno) | Pipeline do frontend mobile (Issue I061) | Simulação e medições |
|---|---|---|---|
| Sistema | Windows, Linux ou macOS com Android SDK | `ubuntu-latest` (GitHub Actions) com aceleração do emulador `<a confirmar na I061>` | Dispositivo Android 12 ou superior com 4 GB de RAM (CR4) |
| Flutter e Dart | Flutter 3 e Dart 3 `<versões exatas a confirmar no Regulamento Interno, secção 11.2>` | Igual | - |
| Emulador | Android 12 (API 31) ou superior, com 4 GB de RAM | Igual, criado e destruído em cada execução | - |
| Backend | Cliente HTTP falso; subconjunto contra o ambiente de demonstração | Cliente HTTP falso | Ambiente de demonstração (CR1), `https://dosecerta.spaincentral.cloudapp.azure.com` |
| Rede | Sem limitação | Sem rede externa nos cenários com cliente falso | Perfil CR3 (10/5 Mbit/s, 50 ms) nas medições de desempenho |
| Testes de sistema | `integration_test` | Igual, com capturas das falhas como artefactos | - |
| Análise estática e formatação | `flutter analyze --fatal-warnings`, `dart format` | Igual | - |
| Build | `flutter build apk --debug` | Igual | - |

## 7. Dados de teste

- Todos os dados são fictícios (CR2): nomes como «Ana Fictícia», e-mails dos domínios reservados `exemplo.pt` e `example.com`, contactos `+351 910 000 00x`, medicamentos e doses fictícios e ficheiros mínimos gerados para os testes. Não há dados de saúde reais.
- Nos cenários com cliente HTTP falso, os dados são as respostas fixas definidas em cada cenário, com os formatos da documentação da API v03.
- No subconjunto contra o backend publicado, cada execução cria as suas contas e fichas pela API, com e-mails únicos.
- Na simulação dos testes de aceitação e nas medições usam-se os dados fictícios do ambiente de demonstração, aplicados pelo script `deploy/azure/aplicar-dados-ficticios.sh` (CR2), e contas fictícias criadas para cada participante.
- Nenhuma credencial nem token está nos testes ou no repositório, nem no APK de teste: as palavras-passe das contas de teste são geradas em cada execução e as variáveis da pipeline vêm de GitHub Secrets (Regulamento Interno do Grupo, secção 14.2).

## 8. Critérios

### 8.1 Entrada

- A aplicação compila e gera o APK de depuração (QG-10) e passa a análise estática e a formatação (QG-11).
- O emulador arranca com Android 12 ou superior e a aplicação de teste instala-se.
- A funcionalidade a testar está integrada na branch em teste e os seus ecrãs foram confrontados com o protótipo v02.
- Para o subconjunto contra o backend publicado e para a simulação: o ambiente de demonstração responde `200` em `GET /saude`, o APK candidato está instalado no dispositivo de referência e os cenários (OF-M4-020) estão aprovados.

### 8.2 Saída (os testes do frontend mobile em m4 terminam com sucesso quando)

| N.º | Critério | Verificação |
|---|---|---|
| CS-M01 | 100% dos cenários `integration_test` passam no emulador | QG-07, relatório da pipeline |
| CS-M02 | `flutter build apk --debug` termina sem erros | QG-10 |
| CS-M03 | 0 avisos e erros do `flutter analyze --fatal-warnings` e 0 diferenças do `dart format` | QG-11 |
| CS-M04 | Cada um dos 90 requisitos funcionais da secção 4.1 tem pelo menos um cenário de sistema ou de aceitação, ou uma justificação | Especificações OF-M4-018 e OF-M4-020; RTM v3 (OF-M4-001) |
| CS-M05 | O subconjunto contra o backend publicado passa | OF-M4-019 |
| CS-M06 | A simulação dos testes de aceitação foi feita e cumpre RNF-022 (≥ 80%) e RNF-023 (SUS ≥ 68), ou os desvios estão registados com a decisão do grupo | OF-M4-021 |
| CS-M07 | O aviso local cumpre RNF-041 no dispositivo de referência e as medições de desempenho da secção 9 estão registadas | OF-M4-019 |
| CS-M08 | Os defeitos encontrados estão registados com a decisão tomada (corrigir ou documentar) | OF-M4-019 e OF-M4-021 |

### 8.3 Suspensão e retoma

| Situação | Suspensão | Retoma |
|---|---|---|
| A build ou a análise estática falha | Os testes de sistema não correm (a pipeline termina antes) | Depois da correção na branch do Executor |
| O emulador não arranca ou a aplicação não se instala | Os testes de sistema não correm | Quando o emulador arrancar; a execução é repetida uma vez antes de ser tratada como defeito |
| Mais de um cenário falha pela mesma causa de ambiente (por exemplo, tempo esgotado no emulador) | A execução é repetida uma vez; se voltar a falhar, é investigada como defeito | Depois de corrigida a causa |
| O ambiente de demonstração não responde | O subconjunto contra o backend publicado e a simulação são adiados | Depois de uma publicação bem-sucedida |
| Um defeito bloqueia uma tarefa-chave na simulação | A tarefa é registada como não cumprida e a sessão continua nas restantes | Depois da correção, repete-se a tarefa afetada |

### 8.4 Ligação aos quality gates

| Quality gate | Verificação | Quando passa a bloquear |
|---|---|---|
| QG-07 | Testes de sistema automáticos com `integration_test` em emulador Android, 100% a passar | A partir do primeiro cenário implementado (Regulamento Interno, secção 11.6) |
| QG-10 | `flutter build apk --debug` sem erros | Desde a criação da pipeline do frontend mobile (I061) |
| QG-11 | `flutter analyze --fatal-warnings` e `dart format` sem erros nem diferenças | Desde a criação da pipeline do frontend mobile (I061) |

Até a pipeline existir, a evidência de QG-10 e QG-11 são os logs locais dos comandos, anexados ao Pull Request de cada Issue.

## 9. Medição de desempenho

| Requisito | Medida na aplicação mobile | Objetivo | Condições |
|---|---|---|---|
| RNF-016 | Primeiro ecrã utilizável depois do arranque a frio | ≤ 3 s | CR4 |
| RNF-017 | Sincronização das tomas das 48 horas seguintes, Cuidador associado a 10 fichas | ≤ 5 s | CR1 a CR4 |
| RNF-013 | Do envio das credenciais ao primeiro ecrã autenticado | ≤ 2 s | CR1 a CR3 |
| RNF-010 | Carregamento da lista diária | ≤ 2 s | CR1 a CR3 |
| RNF-011 | Resultado da confirmação de uma toma | ≤ 1 s | CR1 a CR3 |
| RNF-012 | Carregamento da agenda | ≤ 3 s | CR1 a CR3 |
| RNF-014 | Carregamento de um documento de 5 MB | ≤ 15 s | CR1 a CR3 |
| RNF-041 | Desvio do aviso local em relação à hora prevista | ≤ 60 s | CR4, sem Internet, ecrã bloqueado |

As medições são feitas no dispositivo de referência contra o ambiente de demonstração, com o perfil de rede CR3 quando aplicável, e registadas no relatório de resultados (OF-M4-019). Sempre que possível, cada valor é o percentil 95 de pelo menos 100 medições (CR6); quando não for possível, o relatório indica o número de medições. Uma medição acima do objetivo é registada como observação com a decisão do grupo, não como falha de um cenário automático.

## 10. Entregáveis

| Código | Entregável | Pasta oficial |
|---|---|---|
| OF-M4-017 | Plano de testes do frontend mobile (este documento) | `03-artefactos-de-qualidade-de-produto/03.03-frontend-mobile/03.03.01-plano-de-testes` |
| OF-M4-018 | Especificação dos testes de sistema do frontend mobile | `03.03-frontend-mobile/03.03.02-testes-de-sistema` |
| OF-M4-019 | Resultados dos testes de sistema do frontend mobile | `03.03-frontend-mobile/03.03.02-testes-de-sistema` |
| OF-M4-020 | Especificação dos testes de aceitação do frontend mobile | `03.03-frontend-mobile/03.03.03-testes-de-aceitacao` |
| OF-M4-021 | Relatório da simulação dos testes de aceitação do frontend mobile | `03.03-frontend-mobile/03.03.03-testes-de-aceitacao` |
| OF-M4-022 | Relatório de análise estática do frontend mobile | `03.03-frontend-mobile/03.03.06-analise-estatica` |

A Checklist de Controlo de Artefactos não inclui testes unitários nem relatório de cobertura dos frontends, coerente com a secção 11.5 do Regulamento Interno: se o grupo os fizer, são opcionais e não são entregáveis obrigatórios. Este plano liga-se ainda à Requirements Traceability Matrix v3 (OF-M4-001), que recebe a rastreabilidade dos requisitos aos cenários, e à pipeline v2 (OF-M4-004 a OF-M4-006), que inclui QG-07, QG-10 e QG-11.

## 11. Responsabilidades

| Papel | Responsabilidade nos testes |
|---|---|
| Executor de cada Issue da aplicação mobile | Garante que a funcionalidade passa QG-10 e QG-11 e que os cenários `integration_test` da funcionalidade, quando existirem, passam antes de abrir o Pull Request |
| Revisor | Confirma que os cenários cobrem os critérios de aceitação da Issue, que os resultados esperados correspondem à documentação da API e ao protótipo, e que a pipeline passou |
| Auditor | Confirma que as evidências de testes exigidas estão registadas |
| Executor da I062 (Ricardo André Santos Ribeiro) | Mantém este plano |
| Executor da I058 (estrutura base da aplicação mobile) | `<a confirmar no Sprint Planning>` |
| Executores das Issues de especificação, execução e simulação dos testes do frontend mobile (OF-M4-018 a OF-M4-022) | `<a confirmar no Sprint Planning>` |
| Executor da I061 (pipeline dos frontends) | `<a confirmar no Sprint Planning>`; inclui QG-07, QG-10 e QG-11 na pipeline |
| Participantes da simulação dos testes de aceitação | `<a confirmar>` |
| Pipeline do frontend mobile | Executa automaticamente a build, a análise estática e os cenários no emulador e guarda os relatórios |

## 12. Calendário

| Data e hora | Marco | Issue |
|---|---|---|
| `<a confirmar>` | Entrega deste plano ao Revisor | I062 |
| `<a confirmar>` | Estrutura base da aplicação mobile | I058 |
| `<a confirmar>` | Pipeline do frontend mobile com QG-07, QG-10 e QG-11 | I061 |
| `<a confirmar>` | Especificações dos testes de sistema e de aceitação | `<a confirmar>` |
| `<a confirmar>` | Medições em dispositivo e simulação dos testes de aceitação | `<a confirmar>` |
| `<a confirmar>` | Resultados, relatório da simulação e relatório de análise estática | `<a confirmar>` |
| `<a confirmar>` | Data-limite da entrega da m4 | - |

## 13. Riscos e contingências

| N.º | Risco | Probabilidade e impacto | Mitigação ou contingência |
|---|---|---|---|
| RM-01 | Emulador lento ou instável na pipeline | Média, impacto médio | Imagem do emulador fixada; espera pelo arranque completo; repetição única (secção 8.3) |
| RM-02 | O cliente HTTP falso afasta-se do comportamento real da API | Média, impacto médio | Respostas copiadas dos exemplos da documentação da API v03; subconjunto de cenários contra o backend publicado antes da simulação (secção 5.3) |
| RM-03 | Restrições do Android aos avisos locais: autorização das notificações (Android 13 ou superior) e alarmes exatos (Android 12 ou superior) afetam o RF-062 e a precisão do RNF-041 | Média, impacto alto | Cenários com a autorização concedida e recusada; medição em dispositivo real com as definições documentadas no relatório |
| RM-04 | Não há dispositivo de referência (Android 12, 4 GB) disponível para as medições e para a simulação | Média, impacto médio | Identificar cedo um dispositivo de um elemento do grupo; em último caso, emulador com a mesma configuração, indicado no relatório |
| RM-05 | Os ecrãs implementados divergem do protótipo v02 | Média, impacto médio | Critério de entrada da secção 8.1; o Revisor confronta cada ecrã com o protótipo |
| RM-06 | Poucos participantes disponíveis para a simulação dos testes de aceitação | Média, impacto médio | Sessões marcadas cedo e, se possível, partilhadas com o frontend web; número de participantes registado no relatório |
| RM-07 | Aplicação mobile entregue tarde, sem tempo para os cenários | Média, impacto alto | QG-07 só bloqueia a partir do primeiro cenário; prioridade à F001, à sincronização e aos avisos locais |

## 14. Situação de partida

Em 2026-11-09, a pasta `frontend-mobile/` da `main` está vazia: a estrutura base da aplicação é a Issue I058 `<estado a confirmar>`. Ainda não há cenários `integration_test` nem a pipeline do frontend mobile (Issue I061), pelo que QG-07, QG-10 e QG-11 ainda não correm. O backend publicado responde `200` em `GET /saude` e fornece em OP-34 os dados de que a sincronização e os avisos locais precisam, verificados nos testes do backend em m3.

## 15. Tabela de cobertura do conteúdo mínimo

| Exigência do conteúdo mínimo do plano de testes (Regulamento de Funcionamento da UC, secção 19) | Secção deste documento |
|---|---|
| Âmbito | 1, 3 e 4 |
| Funcionalidades e requisitos abrangidos e excluídos | 4.1 e 4.2 |
| Estratégia (níveis, técnicas, testes de sistema e simulação dos testes de aceitação) | 5 |
| Ambiente | 6 |
| Dados de teste | 7 |
| Critérios de entrada e de saída | 8.1 e 8.2 |
| Critérios de suspensão e de retoma | 8.3 |
| Relação com os quality gates | 8.4 |
| Entregáveis | 10 |
| Responsabilidades | 11 |
| Calendário | 12 |
| Riscos | 13 |

As exigências desta tabela foram tiradas dos critérios de aceitação da Issue I062. `<A confirmar com o texto da secção 19 do Regulamento de Funcionamento da UC.>`
