# PLANO DE TESTES DO FRONTEND WEB

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m4 |
| Versão | v01 |
| Issue | I062 (Sprint 07) |
| Artefacto da Checklist de Controlo de Artefactos | OF-M4-011 Plano de testes do frontend web |
| Código de referência | Estrutura base do frontend web da Issue I057 (branch `feature/i057-frontend-web-autenticacao`, commit `f5db93e`); backend na `main`, commit `ed0faf1` |
| Documentos complementares | Especificação dos testes de sistema (OF-M4-012) e especificação dos testes de aceitação (OF-M4-014) do frontend web |

| Versão | Alterações |
|---|---|
| v01 | Primeira versão: âmbito, níveis e técnicas de teste, ambiente, dados, critérios de entrada, saída, suspensão e retoma, quality gates, entregáveis, responsabilidades, calendário e riscos dos testes do frontend web em m4 |

## 1. Finalidade e âmbito

Este documento define, antes da implementação dos testes, como é verificado o frontend web do DoseCerta na milestone m4: o que se testa e o que fica de fora, a que níveis, com que técnicas, ferramentas, dados e ambientes, com que critérios se decide que os testes terminaram com sucesso e quem faz o quê. Os cenários concretos não estão aqui: são descritos na especificação dos testes de sistema (OF-M4-012) e na especificação dos testes de aceitação (OF-M4-014), que seguem este plano.

O objeto de teste é a aplicação web em Angular 18 (`frontend-web/`) nas funcionalidades F001 a F011, sem a F007, retirada do âmbito por alteração autorizada de âmbito, e só na parte que pertence ao frontend web: os ecrãs, a navegação, as validações prévias, a apresentação das respostas e dos erros da API, a sessão e o comportamento sem ligação. As regras do servidor foram testadas no backend em m3 (`m3-plano-testes-backend-v01.md`, Issues I048 e I049) e não se repetem aqui. A aplicação mobile tem um plano próprio (`m4-plano-testes-frontend-mobile-v01.md`).

## 2. Referências

| Referência | Utilização |
|---|---|
| Especificação de Requisitos `m3-especificacao-requisitos-v02.md` | Requisitos cujo campo «Componentes abrangidos» inclui o frontend web; critérios de aceitação (Dado / Quando / Então); condições de referência CR1 a CR6 |
| Especificações de Casos de Uso `m3-especificacoes-casos-uso-v02.md` | Fluxos principais e alternativos, base dos cenários de sistema e de aceitação |
| Modelo de Classes do Frontend Web `m3-modelo-classes-frontend-web-v02.md` | Ecrãs, rotas, guardas e serviços; tabela de requisitos cobertos e não cobertos pelo frontend web (secção 12) |
| Protótipo de Baixa Fidelidade `m3-prototipo-baixa-fidelidade-v02.pdf` | Ecrãs E01 a E25 (sem E19 e E20), campos, textos e navegação esperados |
| Matriz de Permissões `m3-matriz-permissoes-ficha-v02.md` | O que cada papel vê e pode fazer em cada ecrã; base das tabelas de decisão |
| Documentação da API `m3-documentacao-api-v03.md` (Issue I042) | Respostas e códigos de erro que o frontend apresenta; formato `ProblemaApi` |
| Descrição da Pipeline CI/CD `docs/ci-cd/m3-descricao-pipeline-ci-cd-v03.md` (Issue I050) | Quality gates do backend (QG-01 a QG-05) e indicação de que os gates dos frontends entram em m4 |
| Plano de Testes do Backend `m3-plano-testes-backend-v01.md` (Issue I048) | Modelo deste plano e regras do servidor já verificadas |
| Regulamento Interno do Grupo, secções 10, 11.2 a 11.6 e 14.2 | Ferramentas, quality gates QG-06, QG-08 e QG-09, cobertura «Não aplicável» nos frontends, segredos |
| Regulamento de Funcionamento da UC, secções 16 a 19 | Organização dos artefactos e conteúdo mínimo do plano de testes (tabela final) |

## 3. Itens de teste

| Item | Localização | Descrição |
|---|---|---|
| Núcleo | `frontend-web/src/app/core` | Configuração do endereço da API (só HTTPS), sessão (`AuthService`, `TokenStorage`), interceptores do token e dos erros, guardas das rotas, mensagens de erro e falta de ligação |
| Modelos e acesso à API | `frontend-web/src/app/models`, `frontend-web/src/app/api` | Objetos de transferência e uma classe por grupo de operações da API |
| Componentes partilhados | `frontend-web/src/app/shared` | Validadores prévios e componentes de formulário e de erro |
| Estrutura | `frontend-web/src/app/layout` | Cabeçalho, barra lateral e área das páginas autenticadas |
| Páginas | `frontend-web/src/app/features` | Ecrãs de cada funcionalidade, segundo o protótipo v02 |
| Build de produção | `frontend-web/dist/dosecerta-web` | Aplicação construída com `ng build`, servida no mesmo domínio da API |

A versão de referência de cada execução é o commit testado. Os testes acompanham o código: cada Pull Request que altera o frontend web corre as suites da secção 5.5.

## 4. Funcionalidades a testar e excluídas

### 4.1 A testar

Os requisitos abaixo são os 107 cujo campo «Componentes abrangidos» inclui o frontend web (87 funcionais e 20 não funcionais). Os identificadores dos cenários são atribuídos nas especificações OF-M4-012 e OF-M4-014. Antes de cada especificação, a lista é cruzada com a secção 12 do modelo de classes do frontend web.

| Funcionalidade | Requisitos funcionais | Ecrãs | Tipo de teste |
|---|---|---|---|
| F001 Autenticação e gestão de conta | RF-010 a RF-017 (8) | E01 Registo, E02 Início de sessão, E03 Conta | Sistema e aceitação |
| F002 Fichas de utentes | RF-020 a RF-029, RF-120 a RF-124 (15) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F003 Ficha própria | RF-030 a RF-034 (5) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F004 Associação de Cuidadores e Familiares | RF-040 a RF-049, RF-130 a RF-136 (17) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F005 Registo de medicação | RF-050 a RF-057 (8) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F006 Confirmação de toma | RF-063, RF-064, RF-065, RF-067 (4) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F008 Agenda e compromissos | RF-080 a RF-089 (10) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F009 Ocorrências e notas partilhadas | RF-090 a RF-095 (6) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F010 Lista diária consolidada | RF-100 a RF-102, RF-104, RF-105 (5) | `<a confirmar com o protótipo v02>` | Sistema e aceitação |
| F011 Notificações | RF-110 a RF-113, RF-115 a RF-117, RF-119, RF-140 (9) | `<a confirmar com o protótipo v02>` | Sistema (apresentação) e aceitação |

| Requisitos não funcionais | Como são verificados no frontend web |
|---|---|
| RNF-002 Comunicação cifrada | Sistema: todos os pedidos à API usam o endereço base HTTPS; a aplicação recusa um endereço `http://` no arranque |
| RNF-003 Palavra-passe | Sistema: partições e valores-limite da validação prévia; o servidor continua a validar |
| RNF-008 Formatos e tamanho dos documentos | Sistema: ficheiros PDF, JPG e PNG aceites; outros formatos e ficheiros acima de 10 MB recusados com a mensagem da API |
| RNF-010 a RNF-014 e RNF-019 Tempos | Medição (secção 9) |
| RNF-020 Língua da interface | Sistema e inspeção: textos e mensagens em português europeu |
| RNF-021 Interações para confirmar uma toma | Sistema: contagem das interações a partir da lista diária |
| RNF-022 e RNF-023 Eficácia e satisfação | Simulação dos testes de aceitação com utilizadores (secção 5.6) |
| RNF-024 Acessibilidade | Verificação automática dos critérios WCAG 2.1 AA verificáveis por ferramenta, em cada ecrã |
| RNF-027 Estados sem dependência exclusiva da cor | Sistema: cada estado da toma tem texto ou ícone |
| RNF-028 Mensagens de erro de validação | Sistema: o erro aparece junto ao campo, com a regra, e os dados escritos mantêm-se |
| RNF-029 Formatos de data e hora | Sistema: datas e horas em hora de Lisboa, no formato da especificação |
| RNF-033 Indicação de ausência de ligação | Sistema, com falha de rede simulada: aviso em no máximo 10 segundos |
| RNF-054 Autenticação por token | Sistema: token JWT enviado nos pedidos autenticados, guardado no `sessionStorage` e apagado ao terminar sessão |
| RNF-055 Tecnologia | Inspeção do `package.json` (TypeScript 5 e Angular 18) |

### 4.2 Excluídas

| Exclusão | Justificação |
|---|---|
| F007 (tarefas de cuidados recorrentes) | Retirada do âmbito por alteração autorizada de âmbito |
| Requisitos só do backend ou da base de dados: RF-060, RF-066, RF-068, RF-118, RF-141, RF-142, RNF-001, RNF-004 a RNF-007, RNF-009, RNF-015, RNF-018, RNF-030 a RNF-032, RNF-034, RNF-040, RNF-042 a RNF-053 e RNF-057 | Não têm parte no frontend web; foram verificados no backend em m3 (I048, I049) |
| Requisitos só da aplicação mobile: RF-061, RF-062, RF-069, RNF-016, RNF-017, RNF-025, RNF-026, RNF-041 e RNF-056 | Verificados no plano do frontend mobile |
| Regras do servidor por trás dos requisitos do frontend (por exemplo a deteção dos atrasos em RF-110 a RF-113, o envio e o reenvio de e-mails em RF-041 e RF-136, a autorização por papel no servidor) | Já testadas no backend; no frontend web verifica-se só a apresentação e a navegação |
| Testes de carga (50 Utilizadores em simultâneo) | Requisito do servidor (RNF-015), fora do frontend |
| Navegadores além do Chrome | A condição de referência CR5 fixa o Google Chrome estável |

## 5. Abordagem

### 5.1 Níveis de teste

| Nível | Objetivo | Objeto e isolamento | Ferramentas | Quality gate | Obrigatório |
|---|---|---|---|---|---|
| Sistema | Verificar cada funcionalidade de ponta a ponta no navegador, como o Utilizador a usa: ecrãs, navegação, validações, chamadas à API e apresentação dos resultados e dos erros | Aplicação construída e servida localmente, contra o backend em Docker (secção 6); duplos só para as falhas difíceis de provocar (secção 5.3) | Cypress `<versão a confirmar no Regulamento Interno, secção 11.2>` | QG-06 | Sim |
| Aceitação (simulação) | Confirmar com utilizadores que os critérios de aceitação dos requisitos estão cumpridos e medir a eficácia e a satisfação (RNF-022, RNF-023) | Build de produção contra o ambiente de demonstração (CR1), com dados fictícios | Guião de cenários, registo de resultados, questionário SUS | Não é gate; é o OF-M4-015 | Sim |
| Unitário | Verificar isoladamente serviços, validadores e componentes | Classes do frontend, sem rede | Jasmine e Karma | Nenhum | Não: opcional, sem limiar de cobertura (secção 5.7) |
| Análise estática | Detetar defeitos e desvios das regras de codificação e de formatação | Todo o `frontend-web/src` | ESLint 9 com angular-eslint 18, Prettier 3 | QG-09 | Sim |
| Build | Garantir que a aplicação compila em modo de produção | Projeto completo | `ng build` | QG-08 | Sim |

### 5.2 Técnicas de conceção

| Técnica | Onde se aplica | Exemplo |
|---|---|---|
| Partições de equivalência | Campos de cada formulário: válido, vazio, formato errado, valor rejeitado pelo servidor | E-mail válido, sem `@` e já registado (RF-011) |
| Valores-limite | Palavra-passe (7 e 8 caracteres, sem letra, sem algarismo; RNF-003), antecedência dos compromissos (30, 60, 120, 1440 e 2880 minutos e um valor fora da lista), tamanho dos documentos (10 MB e acima; RNF-008), comprimentos máximos dos campos | Palavra-passe `Abcdef1` recusada e `Abcdefg1` aceite |
| Tabelas de decisão | Papel na ficha (Cuidador principal, Cuidador, Familiar, não associado) e estado da ficha (ativa, arquivada) contra as ações visíveis em cada ecrã, segundo a matriz de permissões | O Familiar não vê os botões de edição; a ficha arquivada só se consulta |
| Transição de estados | Sessão (sem sessão, ativa, expirada, terminada) e toma (pendente, confirmada, confirmada com atraso, não confirmada) tal como aparecem no ecrã | Um 401 a meio da utilização leva ao início de sessão com aviso |
| Casos de uso | Fluxos principais e alternativos das especificações de casos de uso | Registo, início de sessão, criação de ficha, confirmação de toma |
| Injeção de falhas | Falta de ligação, erro interno, documento não guardado, sessão recusada | Aviso de falta de ligação em no máximo 10 segundos (RNF-033) |

### 5.3 Duplos de teste

Os testes de sistema usam o backend real (secção 6). O Cypress só substitui a resposta da API com `cy.intercept` quando a condição é impossível ou instável de provocar no backend real: falta de ligação (RNF-033), erro interno (500), documento não guardado (503), token recusado (401) e tempos de resposta lentos. Cada cenário com duplo indica-o no nome e na especificação, e a mesma funcionalidade tem também pelo menos um cenário contra o backend real.

### 5.4 Regras de escrita dos testes

- Cada cenário é escrito em Dado / Quando / Então e indica os requisitos (RF, RNF), os casos de uso e os ecrãs que verifica. Esta indicação é a fonte da rastreabilidade das especificações e da RTM v3 (OF-M4-001).
- Cada cenário é independente dos outros e da ordem de execução: cria os seus próprios dados pela API no início (secção 7) e não depende de dados de outro cenário.
- Os elementos são localizados por atributos estáveis próprios dos testes (por exemplo `data-cy`), pelo rótulo ou pelo papel acessível, nunca pela posição ou por classes de estilo.
- Não se usam esperas fixas: as esperas são pelas respostas da API (`cy.intercept` com alias) ou pelos elementos.
- Só se usam dados fictícios (secção 7).
- Não se desativa nenhum cenário. Um cenário que falha corrige-se no código ou no teste.
- O código dos testes passa pela mesma análise estática e formatação do código de produção (QG-09).

### 5.5 Regressão

A pipeline do frontend web (Issue I061) corre a build, a análise estática e todos os cenários Cypress em cada Pull Request para a `main` que altere `frontend-web/**` e em cada integração na `main`. A suite completa é a suite de regressão. A simulação dos testes de aceitação é feita uma vez sobre a versão candidata à entrega da m4 e repetida só nos cenários afetados por uma correção.

### 5.6 Simulação dos testes de aceitação

| Elemento | Definição |
|---|---|
| Cenários | Escritos na especificação OF-M4-014 a partir dos critérios de aceitação (Dado / Quando / Então) dos requisitos funcionais da secção 4.1, agrupados pelas tarefas-chave do método de verificação de RNF-022 `<tarefas-chave a confirmar na especificação de requisitos>` |
| Participantes | `<número e perfil a confirmar>`; pessoas que não desenvolveram o frontend, com contas fictícias criadas para a sessão. Cada participante é identificado no relatório só por um código (P01, P02, …) |
| Execução | No ambiente de demonstração (CR1), em Chrome (CR5); cada participante executa as tarefas sem ajuda; um observador regista o resultado. Os cenários que não precisam de utilizadores podem ser executados de forma automática |
| Registo | Participantes, cenários executados, resultado de cada um (cumprido ou não cumprido), desvios observados, tempos, evidências (capturas) e conclusão |
| Critérios | RNF-022: pelo menos 80% dos participantes concluem cada tarefa-chave sem ajuda e à primeira tentativa. RNF-023: pontuação média da System Usability Scale igual ou superior a 68 |
| Relatório | OF-M4-015 Relatório da simulação dos testes de aceitação do frontend web |

### 5.7 Testes unitários

O Regulamento Interno do Grupo (secção 11.5) indica que os testes unitários e a cobertura dos frontends são «Não aplicável» como exigência: o grupo concentra o esforço nos testes de sistema. Os testes unitários com Jasmine e Karma são, por isso, opcionais, não têm limiar de cobertura e não são gate da pipeline. Se forem escritos, correm na pipeline e seguem as regras da secção 5.4.

## 6. Ambiente e ferramentas

| Elemento | Local (computador de cada aluno) | Pipeline do frontend web (Issue I061) | Simulação dos testes de aceitação |
|---|---|---|---|
| Sistema | Windows ou Linux | `ubuntu-latest` (GitHub Actions) | Computador com 8 GB de RAM (CR5) |
| Node.js e gestor de pacotes | Node.js 20 LTS, npm com `package-lock.json` (`npm ci`) | Igual | - |
| Aplicação | `ng serve` em `https://localhost:4200`, com o proxy `/api` (`proxy.conf.json` ou `proxy.local.conf.json`) | Build servida localmente com o mesmo proxy | Build de produção no mesmo domínio da API |
| Backend | Backend local (`https://localhost:7180`) ou contentores de `deploy/azure/docker-compose.yml` | Contentores do backend (API e SQL Server 2022) criados e destruídos em cada execução | Ambiente de demonstração (CR1), `https://dosecerta.spaincentral.cloudapp.azure.com` |
| Navegador | Google Chrome estável (CR5) | Chrome do Cypress, em modo sem interface | Google Chrome estável (CR5) |
| Rede | Sem limitação | Sem limitação | Perfil CR3 (10/5 Mbit/s, 50 ms) nas medições de desempenho |
| Testes de sistema | Cypress `<versão a confirmar>` | Igual, com vídeos e capturas das falhas como artefactos | - |
| Análise estática e formatação | ESLint 9.39.5, angular-eslint 18.4.3, Prettier 3.9.10 (`npm run lint`, `npm run format:check`) | Igual | - |
| Build | Angular 18.2, TypeScript 5.5 (`npm run build`) | Igual | - |

O backend não aceita pedidos de outra origem (não tem política CORS). Por isso, os testes correm sempre com a aplicação e a API na mesma origem: o proxy do `ng serve` em desenvolvimento e na pipeline, e o mesmo domínio em produção.

## 7. Dados de teste

- Todos os dados são fictícios (CR2): nomes como «Ana Fictícia», e-mails dos domínios reservados `exemplo.pt` e `example.com`, contactos `+351 910 000 00x` e ficheiros mínimos gerados para os testes. Não há dados de saúde reais.
- Nos testes de sistema, cada cenário cria no início, pela API, as contas, as fichas e os registos de que precisa, com e-mails únicos por execução. Assim os cenários não dependem uns dos outros nem do estado deixado por outra execução.
- Na pipeline, a base de dados do backend é criada para a execução e destruída no fim. Localmente, os testes usam o backend local ou os contentores, nunca o ambiente de demonstração.
- Na simulação dos testes de aceitação e nas medições de desempenho usam-se os dados fictícios do ambiente de demonstração, aplicados pelo script `deploy/azure/aplicar-dados-ficticios.sh` (CR2), e contas fictícias criadas para cada participante.
- Nenhuma credencial nem token está nos testes ou no repositório: as palavras-passe das contas de teste são geradas em cada execução e as variáveis da pipeline vêm de GitHub Secrets (Regulamento Interno do Grupo, secção 14.2).

## 8. Critérios

### 8.1 Entrada

- O código do frontend web compila em modo de produção (QG-08) e passa a análise estática e a formatação (QG-09).
- O backend de teste responde `200` em `GET /saude` e tem a base de dados atualizada.
- A funcionalidade a testar está integrada na branch em teste e os seus ecrãs foram confrontados com o protótipo v02.
- Para a simulação dos testes de aceitação: a versão candidata está publicada no ambiente de demonstração, os cenários (OF-M4-014) estão aprovados e os participantes estão marcados.

### 8.2 Saída (os testes do frontend web em m4 terminam com sucesso quando)

| N.º | Critério | Verificação |
|---|---|---|
| CS-W01 | 100% dos cenários Cypress passam | QG-06, relatório da pipeline |
| CS-W02 | A build de produção termina sem erros | QG-08 |
| CS-W03 | 0 erros do ESLint e 0 diferenças do Prettier | QG-09 |
| CS-W04 | Cada um dos 87 requisitos funcionais da secção 4.1 tem pelo menos um cenário de sistema ou de aceitação, ou uma justificação | Especificações OF-M4-012 e OF-M4-014; RTM v3 (OF-M4-001) |
| CS-W05 | A simulação dos testes de aceitação foi feita e cumpre RNF-022 (≥ 80%) e RNF-023 (SUS ≥ 68), ou os desvios estão registados com a decisão do grupo | OF-M4-015 |
| CS-W06 | 0 violações automáticas dos critérios WCAG 2.1 AA nos ecrãs verificados (RNF-024) | Relatório de resultados (OF-M4-013) |
| CS-W07 | As medições de desempenho da secção 9 estão registadas | OF-M4-013 |
| CS-W08 | Os defeitos encontrados estão registados com a decisão tomada (corrigir ou documentar) | OF-M4-013 e OF-M4-015 |

### 8.3 Suspensão e retoma

| Situação | Suspensão | Retoma |
|---|---|---|
| A build ou a análise estática falha | Os testes de sistema não correm (a pipeline termina antes) | Depois da correção na branch do Executor |
| O backend de teste não arranca ou não responde `200` em `GET /saude` | Os testes de sistema não correm | Quando os contentores ou o backend local responderem |
| Mais de um cenário falha pela mesma causa de ambiente (por exemplo, tempo esgotado) | A execução é repetida uma vez; se voltar a falhar, é investigada como defeito | Depois de corrigida a causa |
| O ambiente de demonstração não responde ou não tem a versão candidata | A simulação dos testes de aceitação é adiada | Depois de uma publicação bem-sucedida |
| Um defeito bloqueia uma tarefa-chave na simulação | A tarefa é registada como não cumprida e a sessão continua nas restantes | Depois da correção, repete-se a tarefa afetada |

### 8.4 Ligação aos quality gates

| Quality gate | Verificação | Quando passa a bloquear |
|---|---|---|
| QG-06 | Testes de sistema automáticos com Cypress, 100% a passar | A partir do primeiro cenário implementado (Regulamento Interno, secção 11.6) |
| QG-08 | `ng build` sem erros | Desde a criação da pipeline do frontend web (I061) |
| QG-09 | ESLint e Prettier sem erros nem diferenças | Desde a criação da pipeline do frontend web (I061) |

Até a pipeline existir, a evidência de QG-08 e QG-09 são os logs locais de `npm run build`, `npm run lint` e `npm run format:check`, anexados ao Pull Request de cada Issue.

## 9. Medição de desempenho

| Requisito | Medida no frontend web | Objetivo | Condições |
|---|---|---|---|
| RNF-019 | Maior elemento visível do ecrã de início de sessão (Largest Contentful Paint) | ≤ 2,5 s | CR3 e CR5 |
| RNF-013 | Do envio das credenciais ao primeiro ecrã autenticado | ≤ 2 s | CR1 a CR3 |
| RNF-010 | Carregamento da lista diária | ≤ 2 s | CR1 a CR3 |
| RNF-011 | Resultado da confirmação de uma toma | ≤ 1 s | CR1 a CR3 |
| RNF-012 | Carregamento da agenda (vistas diária, semanal e mensal) | ≤ 3 s | CR1 a CR3 |
| RNF-014 | Carregamento de um documento de 5 MB | ≤ 15 s | CR1 a CR3 |

As medições são feitas no Chrome contra o ambiente de demonstração, com o perfil de rede CR3 configurado nas ferramentas do navegador, e registadas no relatório de resultados (OF-M4-013). Sempre que possível, cada valor é o percentil 95 de pelo menos 100 medições (CR6); quando não for possível, o relatório indica o número de medições. Uma medição acima do objetivo é registada como observação com a decisão do grupo, não como falha de um cenário automático.

## 10. Entregáveis

| Código | Entregável | Pasta oficial |
|---|---|---|
| OF-M4-011 | Plano de testes do frontend web (este documento) | `03-artefactos-de-qualidade-de-produto/03.02-frontend-web/03.02.01-plano-de-testes` |
| OF-M4-012 | Especificação dos testes de sistema do frontend web | `03.02-frontend-web/03.02.02-testes-de-sistema` |
| OF-M4-013 | Resultados dos testes de sistema do frontend web | `03.02-frontend-web/03.02.02-testes-de-sistema` |
| OF-M4-014 | Especificação dos testes de aceitação do frontend web | `03.02-frontend-web/03.02.03-testes-de-aceitacao` |
| OF-M4-015 | Relatório da simulação dos testes de aceitação do frontend web | `03.02-frontend-web/03.02.03-testes-de-aceitacao` |
| OF-M4-016 | Relatório de análise estática do frontend web | `03.02-frontend-web/03.02.06-analise-estatica` |

A Checklist de Controlo de Artefactos não inclui testes unitários nem relatório de cobertura dos frontends, coerente com a secção 11.5 do Regulamento Interno: se o grupo os fizer, são opcionais e não são entregáveis obrigatórios. Este plano liga-se ainda à Requirements Traceability Matrix v3 (OF-M4-001), que recebe a rastreabilidade dos requisitos aos cenários, e à pipeline v2 (OF-M4-004 a OF-M4-006), que inclui QG-06, QG-08 e QG-09. O código dos testes fica em `frontend-web/` (pasta dos testes Cypress definida na especificação OF-M4-012).

## 11. Responsabilidades

| Papel | Responsabilidade nos testes |
|---|---|
| Executor de cada Issue do frontend web | Garante que a funcionalidade passa QG-08 e QG-09 e que os cenários Cypress da funcionalidade, quando existirem, passam antes de abrir o Pull Request |
| Revisor | Confirma que os cenários cobrem os critérios de aceitação da Issue, que os resultados esperados correspondem à documentação da API e ao protótipo, e que a pipeline passou |
| Auditor | Confirma que as evidências de testes exigidas estão registadas |
| Executor da I062 (Ricardo André Santos Ribeiro) | Mantém este plano |
| Executor da I057 (Ricardo André Santos Ribeiro) | Estrutura base do frontend web, sobre a qual correm os testes |
| Executores das Issues de especificação, execução e simulação dos testes do frontend web (OF-M4-012 a OF-M4-016) | `<a confirmar no Sprint Planning>` |
| Executor da I061 (pipeline dos frontends) | `<a confirmar no Sprint Planning>`; inclui QG-06, QG-08 e QG-09 na pipeline |
| Participantes da simulação dos testes de aceitação | `<a confirmar>` |
| Pipeline do frontend web | Executa automaticamente a build, a análise estática e os cenários Cypress e guarda os relatórios |

## 12. Calendário

| Data e hora | Marco | Issue |
|---|---|---|
| `<a confirmar>` | Entrega deste plano ao Revisor | I062 |
| `<a confirmar>` | Pipeline do frontend web com QG-06, QG-08 e QG-09 | I061 |
| `<a confirmar>` | Especificações dos testes de sistema e de aceitação | `<a confirmar>` |
| `<a confirmar>` | Simulação dos testes de aceitação | `<a confirmar>` |
| `<a confirmar>` | Resultados, relatório da simulação e relatório de análise estática | `<a confirmar>` |
| `<a confirmar>` | Data-limite da entrega da m4 | - |

## 13. Riscos e contingências

| N.º | Risco | Probabilidade e impacto | Mitigação ou contingência |
|---|---|---|---|
| RW-01 | O backend não tem política CORS: o frontend não chama a API a partir de outra origem | Confirmado, impacto alto | Testes sempre na mesma origem (proxy do `ng serve`); em produção, frontend servido no mesmo domínio da API, a decidir na publicação |
| RW-02 | Arranque do backend em contentores torna a pipeline lenta ou instável | Média, impacto médio | Espera ativa por `GET /saude` antes dos testes; repetição única (secção 8.3); imagens fixadas por versão |
| RW-03 | Cenários instáveis por esperas fixas, dados partilhados ou dependência da hora | Média, impacto médio | Regras da secção 5.4; dados criados por cenário; datas afastadas da meia-noite de Lisboa e da mudança de hora |
| RW-04 | Os ecrãs implementados divergem do protótipo v02 | Média, impacto médio | Critério de entrada da secção 8.1; o Revisor confronta cada ecrã com o protótipo |
| RW-05 | Poucos participantes disponíveis para a simulação dos testes de aceitação | Média, impacto médio | Sessões marcadas cedo; as tarefas sem utilizadores podem ser executadas de forma automática; o número de participantes fica registado no relatório |
| RW-06 | O ambiente de demonstração tem dados alterados pelo uso | Média, impacto baixo | Contas fictícias novas para cada participante; os testes automáticos não usam o ambiente de demonstração |
| RW-07 | Funcionalidades do frontend web entregues tarde, sem tempo para os cenários | Média, impacto alto | QG-06 só bloqueia a partir do primeiro cenário; prioridade aos fluxos dos requisitos essenciais |

## 14. Situação de partida

Em 2026-11-09, a estrutura base do frontend web e a F001 estão implementadas na Issue I057 (commit `f5db93e`, Pull Request ainda por integrar na `main`): a build de produção, o ESLint e o Prettier terminam sem erros, e os pedidos através do proxy contra o backend publicado devolvem as respostas esperadas (`CREDENCIAIS_INVALIDAS`, `VALIDACAO_INVALIDA` com os campos e `NAO_AUTENTICADO`). Ainda não há cenários Cypress nem a pipeline do frontend web (Issue I061), pelo que QG-06, QG-08 e QG-09 ainda não correm automaticamente. O backend publicado responde `200` em `GET /saude`.

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
