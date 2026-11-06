# DOCUMENTAÇÃO DA API

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m3 |
| Versão | v03 |

## 1. Finalidade e âmbito

Este documento define o contrato da API REST do DoseCerta entre o backend e os dois frontends (web e mobile). Descreve os 45 endpoints das funcionalidades F001 a F011 (sem F007, retirada do âmbito), com a operação, a autenticação, a autorização por papel, os parâmetros, os formatos, as respostas, os erros e exemplos, e indica em cada operação os requisitos funcionais e não funcionais que satisfaz. Descreve também o endpoint técnico de verificação do estado do servidor (`GET /saude`, secção 3.5), que não é uma operação funcional.

Nesta versão (v03) o contrato foi confrontado com o código do backend na branch `main`, no commit `7bf212c` (2026-10-30 12:25 UTC), endpoint a endpoint: rotas, autorização, validações, ordem das verificações, códigos de erro e corpos de resposta. Corrige as divergências D04 a D10 da Issue I054 (#68), classificadas como erro da documentação, e regista as decisões de implementação do Sprint 05 (Issues I038, I044, I045, I046 e I047). Onde o código ainda não cumpre o contrato, o documento di-lo expressamente (secções 4 e 8) em vez de descrever um comportamento que não existe. As alterações face à v02 estão na secção 10.

Alteração de âmbito: a funcionalidade F007 (tarefas de cuidados recorrentes) é retirada do âmbito através do pedido de alteração autorizada de âmbito de 2026-10-19 (Regulamento da UC, secção 7.5), que só produz efeitos depois da autorização do docente. Saem as operações OP-35 a OP-40, o código de erro `EXECUCAO_JA_CONFIRMADA`, a notificação `TarefaNaoConfirmada` e o tipo de evento `Tarefa`; os identificadores das restantes operações não mudam.

## 2. Referências

| Referência | Utilização |
|---|---|
| Especificação de Requisitos `m3-especificacao-requisitos-v02.md` | Requisitos RF-010 a RF-142 e RNF-001 a RNF-057 (96 requisitos funcionais ativos e 52 não funcionais) |
| Documentação de Arquitetura `m3-documentacao-arquitetura-v02.md` | Camadas, JWT (AD-02), processo periódico (AD-05) e e-mail (AD-08) |
| Modelo de Dados `m3-modelo-de-dados-dosecerta-v03.md` | Entidades, estados e restrições |
| Modelo de Classes do Backend `m3-modelo-classes-backend-v03.md` | Controllers e serviços que implementam cada endpoint |
| Código do backend, branch `main`, commit `7bf212c` | Implementação confrontada nesta versão (pasta `backend/src`) |
| Issue I054 (#68) e Pull Request #64 | Lista de divergências D01 a D25, C1 e C2 entre o código e a documentação |
| Matriz de Permissões `m3-matriz-permissoes-ficha-v02.md` | Autorização por papel de cada operação (identificadores OP-01 a OP-34 e OP-41 a OP-51) |
| Casos de Uso `m3-especificacoes-casos-uso-v02.md` | Casos de uso relacionados |
| Declaração de Âmbito `m3-declaracao-ambito-v05.pdf` | Limites do sistema |

## 3. Convenções gerais

| Tema | Convenção |
|---|---|
| Endereço base | `https://<servidor>/api/v1`. Todos os pedidos usam HTTPS; um pedido HTTP é recusado ou redirecionado para HTTPS (RNF-002) |
| Tecnologia | API REST implementada em ASP.NET Core 8 (RNF-053), com autenticação por JSON Web Token (RNF-054) |
| Formato | JSON em UTF-8 (`application/json`). Os pedidos com ficheiros usam `multipart/form-data`, com as partes descritas na secção 3.6. Os nomes dos campos usam `camelCase` em português. Os enumerados são enviados e devolvidos como texto (por exemplo `"Moderado"`) |
| Datas e horas | ISO 8601. Os instantes são enviados e recebidos com o desvio de Lisboa (por exemplo `2026-10-07T08:00:00+01:00`) e guardados em UTC pelo servidor. As datas simples usam `AAAA-MM-DD` e as horas `HH:mm`, no formato de 24 horas. O fuso de referência é Europe/Lisbon: os limites do dia (00:00), da semana e do mês, a lista diária e os cálculos em torno da mudança de hora seguem a hora de Lisboa (RNF-047, RF-100). A apresentação em `dd/mm/aaaa` e `hh:mm` cabe aos clientes; o servidor usa o mesmo formato nas mensagens das notificações e nos e-mails (RNF-029) |
| Estados | Toma: `Pendente`, `Confirmada`, `ConfirmadaComAtraso` e `NaoConfirmada` (RF-066, RF-067, RF-068). Tratamento: `EmCurso` e `Terminado` (RF-054). Ficha: `Ativa` e `Arquivada` (RF-120, RF-121) |
| Identificadores | GUID em texto, por exemplo `00000000-0000-4000-8000-000000000101` |
| Idioma | As mensagens de erro e de notificação estão em português europeu (RNF-020) |
| Listas | Devolvem a lista completa do âmbito pedido, sem paginação nesta versão (ambiente de demonstração). Os endpoints com intervalo de datas limitam o intervalo, como indicado em cada um |
| Cabeçalhos | `Authorization: Bearer <token>` em todos os pedidos, exceto o registo (OP-01) e o início de sessão (OP-02); `Content-Type` conforme o formato (RF-017) |
| Escritas | Cada operação de escrita é concluída na totalidade ou não guarda nenhum dos seus dados, incluindo os ficheiros e as referências a ficheiros (RNF-046) |

### 3.1 Autenticação

O início de sessão (OP-02) devolve um token JWT assinado pelo servidor, com validade de 8 horas (RNF-009, RNF-054). Todos os restantes endpoints, exceto o registo (OP-01), exigem o cabeçalho `Authorization: Bearer <token>` (RF-017). O token contém o identificador do Utilizador (`sub`) e o identificador da credencial (`jti`). O término de sessão (OP-03) regista o `jti` e o servidor deixa de aceitar o token (RF-016). A API não aceita tokens expirados, com assinatura alterada ou terminados, e não renova o token: passadas as 8 horas, o Utilizador inicia sessão de novo. As palavras-passe são guardadas apenas como hash com salt (RNF-001), cumprem o RNF-003 e nunca são devolvidas (RF-013); a resposta a credenciais inválidas não indica qual dos dois dados está errado (RF-012).

### 3.2 Autorização por papel

A autorização é verificada no servidor em cada pedido, independentemente do cliente usado e do que a interface mostra (RNF-004). O servidor determina a ficha do recurso pedido, procura a associação do Utilizador a essa ficha e aplica o papel:

- **Sem associação à ficha**, incluindo o Utilizador com convite pendente e a pessoa a quem o acesso foi removido: o servidor responde `404` (`FICHA_NAO_ENCONTRADA` ou `RECURSO_NAO_ENCONTRADO`), sem revelar se a ficha existe; os dados de uma ficha só são devolvidos a quem está associado a ela (RNF-005, RNF-007).
- **Familiar** a tentar uma operação de Cuidador: o servidor responde `403` (`ACESSO_PROIBIDO`). O Familiar só consulta os dados da ficha e o histórico de tomas confirmadas (RF-048); as notas são só para Cuidadores (RF-095).
- **Cuidador principal** e **Cuidador** têm as mesmas permissões de escrita (RF-047), exceto as proteções do Cuidador principal: o titular da ficha própria não é despromovido nem removido (RF-033) e o papel não se transfere nessa ficha (RF-034); noutras fichas, o Cuidador principal só sai depois de outro Cuidador assumir o papel (RF-133).

A matriz completa está em `m3-matriz-permissoes-ficha-v02.md`. A coluna «Papéis permitidos» de cada endpoint repete essa matriz.

### 3.3 Formato dos erros

Todos os erros usam o mesmo corpo. O campo `campos` só existe nos erros de validação e indica o campo e a regra violada, com o valor ou formato esperado (RNF-028). Nos conflitos de confirmação repetida (`TOMA_JA_CONFIRMADA`), o corpo inclui ainda o campo `confirmacao`, com o Cuidador (`confirmadaPor`) e o momento (`confirmadaEm`) da primeira confirmação, que se mantém (RF-105).

```json
{
  "codigo": "VALIDACAO_INVALIDA",
  "mensagem": "Os dados submetidos são inválidos.",
  "campos": [
    { "campo": "palavraPasse", "regra": "Tem de ter pelo menos 8 caracteres, com uma letra e um algarismo." }
  ]
}
```

| Código HTTP | Significado | Usado quando |
|---|---|---|
| 200, 201, 204 | Sucesso, criado, sem conteúdo | Resposta de sucesso |
| 400 | Pedido inválido | Dados em falta ou inválidos (RNF-028) |
| 401 | Não autenticado | Token em falta, expirado, alterado ou terminado; credenciais inválidas no início de sessão (RF-012, RF-016, RF-017) |
| 403 | Acesso proibido | Papel insuficiente na ficha (RNF-004); palavra-passe atual incorreta em OP-06 |
| 404 | Não encontrado | Recurso inexistente ou sem associação à ficha (RNF-005) |
| 405 | Método não permitido | Tentativa de alterar ou eliminar ocorrências e notas, ou de confirmar compromissos (RF-087, RF-090, RF-091); ver secção 4 |
| 409 | Conflito | A operação viola uma regra de negócio, incluindo a confirmação repetida (RF-105) |
| 413, 415 | Ficheiro inválido | Mais de 10 MB; formato diferente de PDF, JPG ou PNG (RNF-008) |
| 500 | Erro interno | Falha inesperada do servidor, não prevista pelas regras de negócio; o corpo é `{"codigo": "ERRO_INTERNO", "mensagem": "Ocorreu um erro inesperado. Tente novamente mais tarde."}`, sem detalhes internos, e a falha fica registada no servidor. Pode ocorrer em qualquer operação e não se repete nas tabelas de erros de cada endpoint |
| 503 | Serviço indisponível | O ficheiro não pôde ser guardado ou lido (RF-023, RF-053, RF-124) |

**Corpo ilegível.** Um corpo JSON que não pode ser lido (sintaxe inválida, corpo em falta ou valor com o tipo errado, por exemplo texto num campo numérico ou uma data mal formada) é recusado com `400` `VALIDACAO_INVALIDA` antes de qualquer outra verificação da operação; `campos` indica `corpo` quando o JSON é ilegível ou o nome do campo com o tipo errado. Um pedido HTTP mal formado também recebe `400` `VALIDACAO_INVALIDA`, com a mensagem «O pedido é inválido.».

**Ordem das verificações.** Cada pedido recebe só o primeiro erro encontrado. A sessão (`401`) é sempre verificada primeiro e, a seguir, a leitura do corpo (parágrafo anterior). A ordem seguinte depende da operação e é a do código confrontado nesta versão:

- Nas operações de escrita sobre uma ficha ou um recurso de ficha de F006 a F011 (OP-33, OP-41, OP-43, OP-44, OP-46 e OP-47), o servidor verifica primeiro o recurso e a associação (`404`), depois o papel (`403`) e o estado da ficha (`409` `FICHA_ARQUIVADA`) e só no fim valida os dados (`400`).
- Nas consultas com intervalo de datas OP-32, OP-42 e OP-51, a validação dos parâmetros (`400`) é feita antes da autorização; em OP-48 e OP-49 é feita depois da autorização, e o parâmetro `tipo` é validado antes de tudo.
- Nas restantes operações, a ordem é a descrita nas regras de cada endpoint; quando não é indicada, o cliente não deve depender dela.

**Ficha arquivada.** Uma ficha arquivada é só de consulta. As operações de escrita sobre ela são recusadas com `409` `FICHA_ARQUIVADA` (RF-120), exceto reativar (OP-13), eliminar se não tiver registos (OP-14), aceitar ou recusar um convite, que pertence ao convidado (OP-21 e OP-22), terminar um tratamento (OP-31) e confirmar as tomas que ficaram por confirmar antes do arquivo (OP-33). Estas operações não devolvem `FICHA_ARQUIVADA`. Todas as consultas continuam disponíveis, mas a ficha arquivada não aparece na agenda nem na lista diária.

### 3.4 Objetivos de desempenho

Os tempos seguintes são medidos nas condições de referência CR1 a CR3 da especificação, no percentil 95 (CR6), e orientam a implementação de cada operação.

| Operação | Objetivo | Requisito |
|---|---|---|
| OP-02 Iniciar sessão | Primeiro ecrã autenticado em 2 segundos no máximo | RNF-013 |
| OP-50 Consultar lista de tarefas diária | 2 segundos no máximo, com um Cuidador de 10 fichas com 20 itens cada | RNF-010 |
| OP-33 Confirmar toma | Resultado apresentado em 1 segundo no máximo | RNF-011 |
| OP-45 Consultar agenda | 3 segundos no máximo, com 10 fichas, em qualquer das vistas | RNF-012 |
| OP-09, OP-15, OP-27 Carregar documento | Documento de 5 MB guardado em 15 segundos no máximo | RNF-014 |
| OP-34 Sincronizar o calendário de tomas | Sincronização das 48 horas seguintes em 5 segundos no máximo | RNF-017 |
| Todas | Cumprir os tempos de OP-50 e OP-33 com 50 Utilizadores em simultâneo; primeira resposta em 5 segundos depois de 60 minutos sem pedidos | RNF-015, RNF-031 |

### 3.5 Verificação do estado do servidor

`GET /saude` (fora de `/api/v1`) é um endpoint técnico, sem autenticação, que permite verificar se a API está a funcionar e se consegue ligar-se à base de dados (RNF-030, RNF-031). Não é uma operação funcional, não tem identificador OP e não devolve dados de Utilizadores nem de fichas. Usa HTTPS como os restantes endpoints.

| Situação | Código HTTP | Corpo |
|---|---|---|
| A API está ativa e a base de dados responde | `200` | `{"estado": "Saudavel", "verificacoes": {"baseDados": "Saudavel"}}` |
| A API está ativa, mas não consegue ligar-se à base de dados | `503` | `{"estado": "Indisponivel", "verificacoes": {"baseDados": "Indisponivel"}}` |

A verificação `baseDados` tenta abrir uma ligação à base de dados. O corpo não segue o formato de erro da secção 3.3, porque não é uma resposta de erro de uma operação.

### 3.6 Pedidos com ficheiros (multipart)

Os pedidos com ficheiros usam `multipart/form-data` com as partes seguintes. O mesmo endpoint aceita `application/json` quando não há ficheiros; o servidor escolhe o tratamento pelo `Content-Type` do pedido.

| Operação | Parte | Conteúdo |
|---|---|---|
| OP-09, OP-11 | `dados` | O JSON do pedido, igual ao corpo `application/json` da operação. É obrigatória; em falta ou com JSON inválido, a resposta é `400` com o campo `dados` |
| OP-09, OP-11 | `documento` | Um ficheiro por parte, repetida uma vez por documento (zero ou mais) |
| OP-09, OP-11 | `categoria` | Texto com a categoria (`ReceitaMedica`, `RelatorioClinico` ou `DocumentoIdentificacao`), uma parte por documento. As partes `categoria` emparelham com as partes `documento` pela ordem: a primeira categoria é a do primeiro documento, e assim por diante. Uma categoria em falta ou inválida dá `400` com o campo `documentos[i].categoria`, em que `i` é a posição do documento a contar de 0 |
| OP-15 | `categoria`, `ficheiro` | Um só documento: a parte `categoria` com a categoria e a parte `ficheiro` com o ficheiro. Em erro, os campos são `categoria` e `ficheiro` |
| OP-27, OP-30 | `dados` | O JSON do pedido, igual ao corpo `application/json` da operação; mesmas regras que em OP-09 |
| OP-27, OP-30 | `receita` | O ficheiro da receita, opcional; a categoria é sempre `ReceitaMedica` e não se envia. Em erro, o campo é `receita.ficheiro` |

Em todos os casos o ficheiro tem de ser PDF, JPG ou PNG, com 10 MB no máximo (RNF-008). O formato é confirmado pelo conteúdo do ficheiro e não só pela extensão.

## 4. Comportamentos do servidor sem endpoint

Alguns requisitos são cumpridos pelo servidor sem que exista um pedido da API. O contrato com os clientes é o seguinte.

| Requisito | Comportamento | Como os clientes o veem |
|---|---|---|
| RF-060, RF-066 | Ao registar ou atualizar um tratamento (OP-27 e OP-30), o servidor gera uma toma `Pendente` por horário e por dia entre a data de início e a data de fim; sem data de fim, gera as tomas numa janela móvel de 30 dias, renovada pelo processo periódico. Alterar os horários só afeta as tomas futuras e terminar o tratamento (OP-31) remove as tomas previstas posteriores; as anteriores mantêm hora e estado. Os compromissos aparecem na agenda na data registada, sem limite | Tomas devolvidas por OP-32 (listar tomas), OP-34 (sincronizar), OP-45 (agenda) e OP-50 (lista diária) |
| RF-066, RF-068, RNF-040, RNF-047 | O processo periódico corre no arranque e depois de 30 em 30 segundos (intervalo configurável, nunca superior a 1 minuto), deteta os instantes-limite e atua no máximo 60 segundos depois deles. Uma toma mantém-se `Pendente` mesmo depois da tolerância; 24 horas depois da hora prevista, sem confirmação, passa a `NaoConfirmada`, estado final que já não pode ser confirmado. Os cálculos seguem a hora de Lisboa, incluindo nas mudanças de hora | Estados em OP-32 e OP-50; a confirmação é recusada em OP-33 |
| RF-110, RF-111, RF-112 | Quando uma toma `Pendente` continua sem confirmação depois de 2 horas (Leve), 1 hora (Moderado) ou 30 minutos (Grave) contados da hora prevista, o servidor notifica uma vez todos os Cuidadores da ficha, na aplicação e por e-mail. As tolerâncias são fixas e não há endpoint que as altere; o estado da toma não muda: fica só registado que os Cuidadores foram notificados. A mensagem tem o medicamento, sem a dose, o nome do utente e a hora prevista, por exemplo «A toma de Metformina de Maria Fictícia Santos das 08:00 não foi confirmada.» | Notificação `TomaEmAtraso` em OP-51 |
| RF-113 | Numa toma de grau Grave sem confirmação 60 minutos depois da hora prevista, o servidor notifica também, uma vez, os Familiares da ficha; nos graus Leve e Moderado os Familiares não são notificados. Numa ficha sem Familiares a toma é só marcada como tratada, sem notificações | Notificação `TomaGraveFamiliares` em OP-51 |
| RF-115 | Ao registar uma ocorrência (OP-46), o servidor notifica todos os Cuidadores e Familiares da ficha, exceto o autor, na aplicação e por e-mail; o registo de uma nota (OP-47) não gera notificações. A mensagem não inclui a descrição da ocorrência: «Ana Fictícia registou uma ocorrência na ficha de Maria Fictícia Santos, ocorrida em 07/10/2026 15:05. Pode consultá-la nas ocorrências da ficha.» | Notificação `NovaOcorrencia` em OP-51 |
| RF-116, RF-117 | Contrato: para cada compromisso, o servidor notifica os Cuidadores e os Familiares da ficha, na aplicação e por e-mail, quando faltar o tempo da antecedência definida (30 minutos, 1 hora, 2 horas, 24 horas ou 48 horas). Se o momento do aviso já tiver passado ao registar o compromisso, a notificação é criada nesse momento. Remover o compromisso (OP-44) cancela o aviso. **Estado no código confrontado (`7bf212c`): não implementado.** A antecedência é validada e guardada (RF-116), mas o processo periódico não tem o passo que cria estas notificações, pelo que nenhuma notificação `ProximoCompromisso` é criada (RF-117 por cumprir; secção 8) | Notificação `ProximoCompromisso` em OP-51, quando implementada |
| RF-041, RF-135, RF-136 | Ao criar um convite (OP-19 e OP-09), o servidor cria a notificação na aplicação, se o e-mail já tiver conta, e envia o e-mail de convite. Se o envio falhar, tenta reenviar até 3 vezes e, se continuar a falhar, o e-mail fica `NaoEntregue`; o convite mantém-se sempre disponível na área de convites | Convite em OP-20; notificação `ConviteRecebido` e campo `email` em OP-51 |
| RF-118, RF-119, RF-141, RNF-018 | O e-mail de cada notificação é submetido ao serviço de envio até 2 minutos depois de criada a notificação. Se o envio falhar, o servidor tenta reenviá-lo até 3 vezes, com 5 minutos de intervalo; se as 3 tentativas falharem, o e-mail fica identificado como `NaoEntregue`. A notificação na aplicação é criada sempre, mesmo que o e-mail falhe, e a falha nunca impede a operação que a originou. O assunto dos e-mails não tem dados de saúde (por exemplo «DoseCerta: toma por confirmar») | Campo `email` das notificações em OP-51: `Enviado`, `Pendente` (ainda não enviado ou com reenvio em curso) ou `NaoEntregue` |
| RF-122, RF-142, RF-069 | Enquanto uma ficha está arquivada, o servidor não gera tomas, avisos nem notificações para essa ficha. Ao reativar a ficha (OP-13), a geração retoma a partir desse momento, sem criar eventos nem notificações para o período em que esteve arquivada, incluindo as tomas previstas antes da reativação | OP-34, OP-45 e OP-50 não devolvem eventos da ficha arquivada |
| RNF-042, RNF-043 | Depois de um reinício, o servidor cria em 5 minutos no máximo as notificações cujo instante-limite ocorreu durante a indisponibilidade e cria no máximo uma notificação por evento e por destinatário. Se a indisponibilidade durar mais de 24 horas, as notificações em falta são criadas antes de a toma passar a `NaoConfirmada` | Notificações em OP-51, com o instante de criação em `criadaEm` |
| RF-061, RF-062, RNF-041 | O servidor só fornece as tomas das 48 horas seguintes aos Cuidadores (OP-34). O aviso à hora prevista é agendado e emitido pela aplicação mobile no dispositivo, sem depender da ligação nesse momento, com um desvio máximo de 60 segundos | Dados em OP-34; sem notificações push, as alterações chegam na sincronização seguinte |
| RF-087, RF-090, RF-091, RNF-049 | A API não tem operações de confirmação de compromissos nem de alteração ou eliminação de ocorrências e notas. `POST /api/v1/compromissos/{compromissoId}/confirmacao` responde `405` `COMPROMISSO_NAO_CONFIRMAVEL`; `PUT`, `PATCH` e `DELETE` em `/api/v1/fichas/{fichaId}/registos/{registoId}` respondem `405` `REGISTO_IMUTAVEL`. Estes pedidos recusados não contam como operações | Resposta `405`; compromissos com `confirmavel` falso em OP-45 e OP-50 |
| RNF-004, RNF-005 | Verificação da sessão e do papel do Utilizador na ficha em cada pedido, independentemente do cliente | Respostas 403 e 404 descritas na secção 3.2 |
| RNF-045, RNF-034 | Cópia de segurança diária da base de dados e dos documentos, conservando os últimos 7 dias | Sem efeito no contrato |

## 5. Endpoints

### 5.1 F001: Autenticação e gestão de conta

#### OP-01 Registar conta

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/auth/registo` |
| Autenticação e autorização | Pública (sem sessão) |
| Papéis permitidos | Qualquer pessoa, sem sessão (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F001 |
| Requisitos | RF-010, RF-011, RF-044, RNF-001, RNF-003 |
| Casos de uso | UC-01 |

**Regras:** Obrigatórios: `nome`, `email` e `palavraPasse` (RF-010). O e-mail tem formato válido e é único (RF-011). A palavra-passe tem pelo menos 8 caracteres, com pelo menos uma letra e um algarismo (RNF-003), e é guardada apenas como hash com salt (RNF-001). O registo não inicia sessão: o Utilizador usa OP-02 a seguir. Os convites pendentes dirigidos ao e-mail ficam disponíveis na área de convites da conta nova (RF-044).

**Exemplo de pedido:**

```json
{
  "nome": "Ana Fictícia",
  "email": "ana@exemplo.pt",
  "palavraPasse": "Segura123"
}
```

**Resposta de sucesso:** `201`

```json
{
  "id": "00000000-0000-4000-8000-000000000011",
  "nome": "Ana Fictícia",
  "email": "ana@exemplo.pt",
  "convitesPendentes": 1
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo obrigatório em falta, e-mail com formato inválido ou palavra-passe que não cumpre o RNF-003; a resposta indica o campo em erro (RF-010, RNF-028). |
| 409 | `EMAIL_JA_REGISTADO` | Já existe uma conta com o e-mail indicado; a conta não é criada (RF-011). |

#### OP-02 Iniciar sessão

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/auth/login` |
| Autenticação e autorização | Pública (sem sessão) |
| Papéis permitidos | Qualquer pessoa, sem sessão (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F001 |
| Requisitos | RF-012, RF-017, RNF-009, RNF-054 |
| Casos de uso | UC-02 |

**Regras:** O token JWT é assinado pelo servidor (RNF-054) e tem validade de 8 horas (RNF-009); `expiraEm` indica o instante de expiração. A resposta a credenciais inválidas é igual para um e-mail inexistente e para uma palavra-passe errada, para não revelar qual dos dois está errado (RF-012). Esta operação é pública, tal como OP-01 (RF-017).

**Exemplo de pedido:**

```json
{
  "email": "ana@exemplo.pt",
  "palavraPasse": "Segura123"
}
```

**Resposta de sucesso:** `200`

```json
{
  "token": "<jwt>",
  "tipo": "Bearer",
  "expiraEm": "2026-10-07T17:00:00+01:00"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido; a resposta indica o campo e o valor ou formato esperado (RNF-028). |
| 401 | `CREDENCIAIS_INVALIDAS` | E-mail inexistente ou palavra-passe errada; a mensagem é igual nos dois casos e não indica qual está errado (RF-012). |

#### OP-03 Terminar sessão

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/auth/logout` |
| Autenticação e autorização | Sessão válida (qualquer Utilizador) |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar, Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F001 |
| Requisitos | RF-016, RF-017 |
| Casos de uso | UC-04 |

**Regras:** Invalida o token: regista o identificador da credencial (`jti`) em `SessaoRevogada` e, a partir daí, qualquer pedido com esse token é recusado com `401` até o Utilizador iniciar sessão de novo (RF-016, RF-017).

**Resposta de sucesso:** `204` (sem corpo)

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |

#### OP-04 Consultar dados da conta

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/conta` |
| Autenticação e autorização | Sessão válida (qualquer Utilizador) |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar, Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F001 |
| Requisitos | RF-013 |
| Casos de uso | UC-03 |

**Regras:** Devolve o nome e o e-mail da conta; a resposta nunca inclui a palavra-passe nem o respetivo hash (RF-013).

**Resposta de sucesso:** `200`

```json
{
  "id": "00000000-0000-4000-8000-000000000011",
  "nome": "Ana Fictícia",
  "email": "ana@exemplo.pt"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |

#### OP-05 Atualizar dados da conta

| Campo | Valor |
|---|---|
| Método e caminho | `PUT /api/v1/conta` |
| Autenticação e autorização | Sessão válida (qualquer Utilizador) |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar, Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F001 |
| Requisitos | RF-011, RF-014 |
| Casos de uso | UC-03 |

**Regras:** Altera o nome e o e-mail da conta (RF-014). Se o novo e-mail pertencer a outra conta, a alteração é recusada e o e-mail anterior mantém-se (RF-011, RF-014). A palavra-passe altera-se em OP-06.

**Exemplo de pedido:**

```json
{
  "nome": "Ana Fictícia Costa",
  "email": "ana.costa@exemplo.pt"
}
```

**Resposta de sucesso:** `200`

```json
{
  "id": "00000000-0000-4000-8000-000000000011",
  "nome": "Ana Fictícia Costa",
  "email": "ana.costa@exemplo.pt"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido; a resposta indica o campo e o valor ou formato esperado (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 409 | `EMAIL_JA_REGISTADO` | O novo e-mail pertence a outra conta; o e-mail anterior mantém-se (RF-011, RF-014). |

#### OP-06 Alterar palavra-passe

| Campo | Valor |
|---|---|
| Método e caminho | `PUT /api/v1/conta/palavra-passe` |
| Autenticação e autorização | Sessão válida (qualquer Utilizador) |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar, Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F001 |
| Requisitos | RF-015, RNF-001, RNF-003 |
| Casos de uso | UC-03 |

**Regras:** Exige a palavra-passe atual e a nova (RF-015). A nova palavra-passe cumpre o RNF-003 (pelo menos 8 caracteres, com uma letra e um algarismo) e é guardada apenas como hash com salt (RNF-001). Depois da alteração, o início de sessão exige a nova palavra-passe e a anterior deixa de ser aceite; se a palavra-passe atual estiver errada, a alteração é recusada e a anterior mantém-se (RF-015).

**Exemplo de pedido:**

```json
{
  "palavraPasseAtual": "Segura123",
  "palavraPasseNova": "MaisSegura456"
}
```

**Resposta de sucesso:** `204` (sem corpo)

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou nova palavra-passe que não cumpre o RNF-003; a resposta indica o campo e a regra violada (RF-015, RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `PALAVRA_PASSE_ATUAL_INCORRETA` | A palavra-passe atual indicada está errada; a alteração é recusada e a palavra-passe anterior mantém-se (RF-015). |

### 5.2 F002: Criação e gestão de fichas de utentes

#### OP-07 Listar fichas do Utilizador

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/fichas` |
| Autenticação e autorização | Sessão válida (qualquer Utilizador) |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar, Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F002 |
| Requisitos | RF-028, RF-120, RF-121, RNF-005 |
| Casos de uso | UC-07 |

**Parâmetros e notas:** Query opcional: `estado=Ativa|Arquivada`; sem o parâmetro, devolve as fichas ativas e arquivadas (RF-120, RF-121). Devolve só as fichas a que o Utilizador está associado (RNF-005).

**Resposta de sucesso:** `200`

```json
[
  {
    "id": "00000000-0000-4000-8000-000000000101",
    "nomeCompleto": "Maria Fictícia Santos",
    "eProprio": false,
    "estado": "Ativa",
    "meuPapel": "Cuidador",
    "souPrincipal": true
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |

#### OP-08 Obter as listas de necessidades

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/dominios/necessidades` |
| Autenticação e autorização | Sessão válida (qualquer Utilizador) |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar, Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F002 |
| Requisitos | RF-021 |
| Casos de uso | UC-05, UC-06, UC-08 |

**Parâmetros e notas:** Devolve as listas fechadas de condições de saúde, alergias e graus de autonomia/mobilidade; os pedidos de criação e de edição de fichas só aceitam valores destas listas (RF-021).

**Resposta de sucesso:** `200`

```json
{
  "condicoesSaude": [
    {
      "id": 1,
      "designacao": "Diabetes"
    }
  ],
  "alergias": [
    {
      "id": 1,
      "designacao": "Medicamentos"
    }
  ],
  "grausAutonomia": [
    {
      "id": 2,
      "designacao": "Autonomia parcial"
    }
  ]
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |

#### OP-09 Criar ficha (de terceiro ou própria)

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/fichas` |
| Autenticação e autorização | Sessão válida (qualquer Utilizador) |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar, Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F002 |
| Requisitos | RF-020, RF-021, RF-022, RF-023, RF-024, RF-025, RF-030, RF-031, RF-041, RNF-006, RNF-008, RNF-014, RNF-046 |
| Casos de uso | UC-05, UC-06 |

**Parâmetros e notas:** Pedido `application/json` sem documentos, ou `multipart/form-data` com documentos: parte `dados` com o JSON abaixo, uma parte `documento` por ficheiro e uma parte `categoria` por ficheiro (`ReceitaMedica`, `RelatorioClinico` ou `DocumentoIdentificacao`), emparelhadas pela ordem (secção 3.6). São obrigatórios `nomeCompleto`, `dataNascimento` (não pode ser futura) e `contactoEmergencia` (RF-020); as necessidades específicas são escolhidas das listas fechadas de OP-08 (RF-021) e os documentos seguem o RNF-008 (PDF, JPG ou PNG até 10 MB; RF-022).

Com `propria=true` cria a ficha própria: o criador fica associado como Cuidador principal e só pode ter uma (RF-030, RF-031). Com `propria=false`, `cuidadorPrincipal` é obrigatório: `eu=true` associa o criador como Cuidador principal no momento da criação (RF-024); `eu=false` com o `email` da pessoa a convidar deixa o convite pendente e o criador como Cuidador principal provisório até à resposta (RF-025). O servidor envia o convite nos termos de OP-19 (RF-041). Se o convite for aceite (OP-21), o convidado passa a Cuidador principal e o criador deixa de estar associado; se for recusado (OP-22), o criador mantém-se.

A criação é atómica: se um documento não puder ser guardado, a ficha não é criada, nenhum dado é guardado e não fica nenhuma referência ao ficheiro (RF-023, RNF-046); o cliente mantém os dados preenchidos para nova submissão.

**Exemplo de pedido** (`application/json`, ou parte `dados` do pedido `multipart/form-data`):

```json
{
  "propria": false,
  "nomeCompleto": "Maria Fictícia Santos",
  "dataNascimento": "1941-05-17",
  "contactoEmergencia": "+351 910 000 001",
  "grauAutonomiaId": 2,
  "condicoesSaudeIds": [
    1
  ],
  "alergiasIds": [
    1
  ],
  "cuidadorPrincipal": {
    "eu": false,
    "email": "rui@exemplo.pt"
  }
}
```

**Resposta de sucesso:** `201`

```json
{
  "id": "00000000-0000-4000-8000-000000000101",
  "nomeCompleto": "Maria Fictícia Santos",
  "dataNascimento": "1941-05-17",
  "contactoEmergencia": "+351 910 000 001",
  "eProprio": false,
  "estado": "Ativa",
  "grauAutonomia": {
    "id": 2,
    "designacao": "Autonomia parcial"
  },
  "condicoesSaude": [
    {
      "id": 1,
      "designacao": "Diabetes"
    }
  ],
  "alergias": [
    {
      "id": 1,
      "designacao": "Medicamentos"
    }
  ],
  "meuPapel": "Cuidador",
  "souPrincipal": true
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido, por exemplo data de nascimento futura, valor fora das listas fechadas, categoria de documento em falta ou diferente das três previstas (campo `documentos[i].categoria`), parte `dados` em falta ou com JSON inválido (campo `dados`) ou `cuidadorPrincipal` em falta numa ficha de terceiro; a resposta indica o campo e a regra violada (RF-020, RF-022, RF-024, RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 409 | `FICHA_PROPRIA_JA_EXISTE` | O Utilizador já tem uma ficha própria; a criação é recusada (RF-031). |
| 413 | `FICHEIRO_DEMASIADO_GRANDE` | Ficheiro com mais de 10 MB (RNF-008). |
| 415 | `FORMATO_NAO_SUPORTADO` | Formato diferente de PDF, JPG ou PNG (RNF-008). |
| 503 | `DOCUMENTO_NAO_GUARDADO` | O ficheiro não pôde ser guardado; a ficha não é criada e nada foi gravado (RF-023). |

#### OP-10 Consultar ficha

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/fichas/{fichaId}` |
| Autenticação e autorização | Sessão válida; Cuidador principal, Cuidador ou Familiar da ficha |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F002 |
| Requisitos | RF-028, RNF-005 |
| Casos de uso | UC-07 |

**Parâmetros e notas:** Disponível para qualquer Cuidador ou Familiar associado (RF-028). Devolve os dados de identificação e as necessidades específicas; os documentos listam-se em OP-16 e obtêm-se em OP-17. Um Utilizador não associado recebe `404` (RNF-005).

**Resposta de sucesso:** `200`

```json
{
  "id": "00000000-0000-4000-8000-000000000101",
  "nomeCompleto": "Maria Fictícia Santos",
  "dataNascimento": "1941-05-17",
  "contactoEmergencia": "+351 910 000 001",
  "eProprio": false,
  "estado": "Ativa",
  "grauAutonomia": {
    "id": 2,
    "designacao": "Autonomia parcial"
  },
  "condicoesSaude": [
    {
      "id": 1,
      "designacao": "Diabetes"
    }
  ],
  "alergias": [
    {
      "id": 1,
      "designacao": "Medicamentos"
    }
  ],
  "meuPapel": "Cuidador",
  "souPrincipal": true
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |

#### OP-11 Editar ficha

| Campo | Valor |
|---|---|
| Método e caminho | `PUT /api/v1/fichas/{fichaId}` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F002 |
| Requisitos | RF-021, RF-022, RF-023, RF-029, RNF-008, RNF-046 |
| Casos de uso | UC-08 |

**Parâmetros e notas:** Só um Cuidador edita a ficha; o Familiar recebe `403` (RF-029). Pedido `application/json` ou, se acrescentar documentos, `multipart/form-data` com as partes `dados`, `documento` e `categoria`, como em OP-09 (secção 3.6). Se um documento incluído não puder ser guardado, nenhuma alteração é guardada e a ficha fica exatamente como estava antes (RF-023, RNF-046).

**Exemplo de pedido:**

```json
{
  "nomeCompleto": "Maria Fictícia Santos",
  "dataNascimento": "1941-05-17",
  "contactoEmergencia": "+351 910 000 002",
  "grauAutonomiaId": 2,
  "condicoesSaudeIds": [
    1
  ],
  "alergiasIds": []
}
```

**Resposta de sucesso:** `200`

```json
{
  "id": "00000000-0000-4000-8000-000000000101",
  "nomeCompleto": "Maria Fictícia Santos",
  "dataNascimento": "1941-05-17",
  "contactoEmergencia": "+351 910 000 002",
  "eProprio": false,
  "estado": "Ativa",
  "grauAutonomia": {
    "id": 2,
    "designacao": "Autonomia parcial"
  },
  "condicoesSaude": [
    {
      "id": 1,
      "designacao": "Diabetes"
    }
  ],
  "alergias": [],
  "meuPapel": "Cuidador",
  "souPrincipal": true
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido; a resposta indica o campo e o valor ou formato esperado (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |
| 413 | `FICHEIRO_DEMASIADO_GRANDE` | Documento com mais de 10 MB (RNF-008). |
| 415 | `FORMATO_NAO_SUPORTADO` | Formato diferente de PDF, JPG ou PNG (RNF-008). |
| 503 | `DOCUMENTO_NAO_GUARDADO` | O documento não pôde ser guardado; nenhuma alteração é guardada (RF-023). |

#### OP-12 Arquivar ficha

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/fichas/{fichaId}/arquivo` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F002 |
| Requisitos | RF-120, RF-122, RNF-049 |
| Casos de uso | UC-10 |

**Regras:** Só um Cuidador arquiva; o Familiar recebe `403` (RF-120). Preserva todo o histórico: a medicação e o histórico mantêm-se consultáveis (RF-120, RNF-049). Enquanto arquivada, a ficha deixa de gerar tomas, avisos e notificações e deixa de aparecer na agenda e na lista diária; ao arquivar, apagam-se apenas as tomas futuras ainda por confirmar (RF-122; secção 4).

**Resposta de sucesso:** `204` (sem corpo)

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 409 | `FICHA_JA_ARQUIVADA` | A ficha já está arquivada. |

#### OP-13 Reativar ficha

| Campo | Valor |
|---|---|
| Método e caminho | `DELETE /api/v1/fichas/{fichaId}/arquivo` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F002 |
| Requisitos | RF-121, RF-122 |
| Casos de uso | UC-11 |

**Regras:** Volta ao estado ativa e retoma a geração de tomas, avisos e notificações a partir do momento da reativação, sem criar eventos para o período em que esteve arquivada (RF-121, RF-122; secção 4).

**Resposta de sucesso:** `204` (sem corpo)

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 409 | `FICHA_NAO_ARQUIVADA` | A ficha não está arquivada. |

#### OP-14 Eliminar ficha

| Campo | Valor |
|---|---|
| Método e caminho | `DELETE /api/v1/fichas/{fichaId}` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F002 |
| Requisitos | RF-123, RNF-046 |
| Casos de uso | UC-12 |

**Regras:** Só é possível se a ficha não tiver medicação, tomas, ocorrências nem notas registadas (RF-123). Os compromissos, os documentos (incluindo os ficheiros guardados no servidor), os convites, as associações e as notificações da ficha são removidos com ela na mesma transação e, depois de a transação ser confirmada, são removidos também os ficheiros do servidor (RF-123, RNF-046).

**Resposta de sucesso:** `204` (sem corpo)

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 409 | `FICHA_COM_HISTORICO` | A ficha tem medicação, tomas, ocorrências ou notas; a eliminação é recusada, nada é removido (nem os ficheiros dos documentos) e a resposta indica que a ficha só pode ser arquivada (RF-123, OP-12). |

#### OP-15 Carregar documento na ficha

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/fichas/{fichaId}/documentos` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F002 |
| Requisitos | RF-022, RF-023, RNF-006, RNF-008, RNF-014, RNF-046 |
| Casos de uso | UC-08 |

**Parâmetros e notas:** `multipart/form-data` com a parte `categoria` e a parte `ficheiro` (secção 3.6). Categorias: `ReceitaMedica`, `RelatorioClinico`, `DocumentoIdentificacao` (RF-022). Formatos PDF, JPG e PNG, com 10 MB no máximo por ficheiro (RNF-008). O documento só fica associado à ficha depois de o ficheiro ser guardado no sistema de ficheiros do servidor (RNF-006); se a gravação falhar, a operação é recusada e a ficha fica exatamente como estava (RF-023, RNF-046).

**Exemplo de pedido** (partes do pedido `multipart/form-data`):

```json
{
  "categoria": "RelatorioClinico",
  "ficheiro": "parte multipart"
}
```

**Resposta de sucesso:** `201`

```json
{
  "id": "00000000-0000-4000-8000-000000000301",
  "categoria": "RelatorioClinico",
  "nomeOriginal": "relatorio.pdf",
  "formato": "PDF",
  "tamanhoBytes": 482311,
  "carregadoEm": "2026-10-07T09:10:00+01:00"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido, por exemplo uma categoria diferente das três previstas; a resposta indica o campo e a regra violada (RF-022, RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |
| 413 | `FICHEIRO_DEMASIADO_GRANDE` | Ficheiro com mais de 10 MB (RNF-008). |
| 415 | `FORMATO_NAO_SUPORTADO` | Formato diferente de PDF, JPG ou PNG (RNF-008). |
| 503 | `DOCUMENTO_NAO_GUARDADO` | O ficheiro não pôde ser guardado; nenhuma referência é criada e a ficha fica como estava (RF-023). |

#### OP-16 Listar documentos da ficha

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/fichas/{fichaId}/documentos` |
| Autenticação e autorização | Sessão válida; Cuidador principal, Cuidador ou Familiar da ficha |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F002 |
| Requisitos | RF-022, RF-028, RNF-007 |
| Casos de uso | UC-07 |

**Parâmetros e notas:** Disponível para Cuidadores e Familiares associados (RF-028, RNF-007). Devolve os metadados e a categoria de cada documento; o conteúdo obtém-se em OP-17.

**Resposta de sucesso:** `200`

```json
[
  {
    "id": "00000000-0000-4000-8000-000000000301",
    "categoria": "RelatorioClinico",
    "nomeOriginal": "relatorio.pdf",
    "formato": "PDF",
    "tamanhoBytes": 482311,
    "carregadoEm": "2026-10-07T09:10:00+01:00"
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |

#### OP-17 Descarregar documento

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/documentos/{documentoId}` |
| Autenticação e autorização | Sessão válida; Cuidador principal, Cuidador ou Familiar da ficha |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F002 |
| Requisitos | RF-028, RF-124, RNF-007 |
| Casos de uso | UC-09 |

**Parâmetros e notas:** Resposta binária com o tipo de conteúdo do ficheiro (`application/pdf`, `image/jpeg` ou `image/png`). A ficha é determinada pelo documento. Só os Utilizadores associados à ficha obtêm o ficheiro: os ficheiros não têm endereço público e todo o acesso passa por esta operação, com verificação da sessão e da associação (RNF-007).

**Resposta de sucesso:** `200` (conteúdo do ficheiro)

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 404 | `DOCUMENTO_NAO_ENCONTRADO` | O documento não existe ou pertence a uma ficha a que o Utilizador não está associado; as duas situações têm a mesma resposta (RNF-005, RNF-007). |
| 503 | `DOCUMENTO_INDISPONIVEL` | O ficheiro guardado não pôde ser obtido; os restantes dados da ficha não são afetados e o cliente apresenta o erro no lugar do ficheiro e permite repetir o pedido (RF-124). |

### 5.3 F004: Associação de Cuidadores e Familiares a uma ficha

#### OP-18 Listar pessoas associadas à ficha

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/fichas/{fichaId}/pessoas` |
| Autenticação e autorização | Sessão válida; Cuidador principal, Cuidador ou Familiar da ficha |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F004 |
| Requisitos | RF-025, RF-047, RF-048, RF-132 |
| Casos de uso | UC-15 |

**Parâmetros e notas:** Devolve os Cuidadores e os Familiares associados à ficha, com o papel, se é o Cuidador principal e se o é a título provisório (RF-025). Os clientes usam a lista para promover, despromover, remover e transferir o papel de Cuidador principal; as permissões de cada papel são as do RF-047 (Cuidador) e do RF-048 (Familiar).

**Resposta de sucesso:** `200`

```json
[
  {
    "utilizadorId": "00000000-0000-4000-8000-000000000012",
    "nome": "Rui Fictício",
    "email": "rui@exemplo.pt",
    "papel": "Familiar",
    "principal": false,
    "provisorio": false
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |

#### OP-19 Convidar pessoa para a ficha

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/fichas/{fichaId}/convites` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F004 |
| Requisitos | RF-025, RF-027, RF-032, RF-040, RF-041, RF-043, RF-135, RF-136, RNF-018, RNF-052 |
| Casos de uso | UC-13 |

**Regras:** O Cuidador escolhe o papel oferecido, `Cuidador` ou `Familiar` (RF-040). O servidor cria o convite pendente, cria a notificação na aplicação se o e-mail já tiver conta e envia o e-mail de convite (RF-041). Se o e-mail não tiver conta, o convite fica pendente e aparece na área de convites quando alguém se registar com esse endereço (RF-044). Se o envio do e-mail falhar, o servidor tenta reenviá-lo até 3 vezes (RF-136) e o convite mantém-se disponível na área de convites (RF-135).

Não é criado um segundo convite pendente para o mesmo e-mail e a mesma ficha (RF-043); depois de o convite ser recusado, ou noutra ficha, pode ser criado novo convite. `paraPrincipal=true` oferece o papel de Cuidador principal (o `papel` tem de ser `Cuidador`) e usa-se, por exemplo, quando o convite anterior foi recusado e o criador indica outra pessoa (RF-025, RF-027); não se aplica à ficha própria (RF-034). Só pode haver um convite para Cuidador principal pendente por ficha: enquanto esse convite não tiver resposta, um novo convite com `paraPrincipal=true` é recusado, mesmo para outro e-mail. Na ficha própria os convites seguem as mesmas regras das restantes fichas (RF-032).

**Exemplo de pedido:**

```json
{
  "email": "rui@exemplo.pt",
  "papel": "Familiar",
  "paraPrincipal": false
}
```

**Resposta de sucesso:** `201`

```json
{
  "id": "00000000-0000-4000-8000-000000000801",
  "fichaId": "00000000-0000-4000-8000-000000000101",
  "nomeFicha": "Maria Fictícia Santos",
  "papelOferecido": "Familiar",
  "paraPrincipal": false,
  "convidadoPor": "Ana Fictícia",
  "estado": "Pendente",
  "criadoEm": "2026-10-07T09:00:00+01:00"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido; a resposta indica o campo e o valor ou formato esperado (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 403 | `CUIDADOR_PRINCIPAL_PROTEGIDO` | Ficha própria: o papel de Cuidador principal não se transfere, por isso `paraPrincipal=true` é recusado (RF-034). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 409 | `CONVITE_PENDENTE_EXISTE` | Já existe um convite pendente para o mesmo e-mail e a mesma ficha (RF-043) ou, com `paraPrincipal=true`, já existe nesta ficha um convite para Cuidador principal pendente, para qualquer e-mail, porque só pode haver um de cada vez (RF-025); o convite não é criado e o Cuidador é informado. |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |
| 409 | `PESSOA_JA_ASSOCIADA` | O e-mail pertence a uma pessoa já associada à ficha. |

#### OP-20 Listar convites recebidos

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/convites` |
| Autenticação e autorização | Sessão válida (qualquer Utilizador) |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar, Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F004 |
| Requisitos | RF-042, RF-044, RF-135 |
| Casos de uso | UC-14 |

**Parâmetros e notas:** Devolve os convites pendentes dirigidos ao e-mail do Utilizador autenticado, com a ficha e o papel oferecido, para aceitar ou recusar (RF-042). Inclui os convites criados antes do registo da conta (RF-044) e os convites cujo e-mail não foi entregue (RF-135).

**Resposta de sucesso:** `200`

```json
[
  {
    "id": "00000000-0000-4000-8000-000000000801",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "nomeFicha": "Maria Fictícia Santos",
    "papelOferecido": "Cuidador",
    "paraPrincipal": false,
    "convidadoPor": "Ana Fictícia",
    "estado": "Pendente",
    "criadoEm": "2026-10-07T09:00:00+01:00"
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |

#### OP-21 Aceitar convite

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/convites/{conviteId}/aceitacao` |
| Autenticação e autorização | Sessão válida; Utilizador com convite pendente para o seu e-mail |
| Papéis permitidos | Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F004 |
| Requisitos | RF-026, RF-045 |
| Casos de uso | UC-14 |

**Regras:** Associa o Utilizador à ficha com o papel oferecido e o convite deixa de estar pendente (RF-045). Se o convite é para Cuidador principal, o convidado passa a Cuidador principal e o criador provisório deixa de estar associado à ficha (RF-026).

**Resposta de sucesso:** `200`

```json
{
  "utilizadorId": "00000000-0000-4000-8000-000000000012",
  "nome": "Rui Fictício",
  "email": "rui@exemplo.pt",
  "papel": "Familiar",
  "principal": false,
  "provisorio": false
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 404 | `CONVITE_NAO_ENCONTRADO` | O convite não existe ou não é dirigido ao e-mail do Utilizador. |
| 409 | `CONVITE_JA_RESPONDIDO` | O convite já foi aceite ou recusado. |
| 409 | `PESSOA_JA_ASSOCIADA` | O Utilizador já está associado à ficha do convite (por exemplo, foi associado por outro convite entretanto aceite); o convite não é aceite. |

#### OP-22 Recusar convite

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/convites/{conviteId}/recusa` |
| Autenticação e autorização | Sessão válida; Utilizador com convite pendente para o seu e-mail |
| Papéis permitidos | Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F004 |
| Requisitos | RF-027, RF-046 |
| Casos de uso | UC-14 |

**Regras:** Não cria qualquer associação e o convite deixa de estar pendente (RF-046). Se era para Cuidador principal, o criador mantém-se como Cuidador principal e pode convidar outra pessoa (RF-027). Depois da recusa, pode ser criado novo convite para o mesmo e-mail (RF-043).

**Resposta de sucesso:** `204` (sem corpo)

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 404 | `CONVITE_NAO_ENCONTRADO` | O convite não existe ou não é dirigido ao e-mail do Utilizador. |
| 409 | `CONVITE_JA_RESPONDIDO` | O convite já foi aceite ou recusado. |

#### OP-23 Promover Familiar a Cuidador

| Campo | Valor |
|---|---|
| Método e caminho | `PUT /api/v1/fichas/{fichaId}/pessoas/{utilizadorId}/papel` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F004 |
| Requisitos | RF-047, RF-049 |
| Casos de uso | UC-15 |

**Regras:** Mesmo endpoint da despromoção (OP-24); o corpo indica o papel pretendido. O Familiar promovido passa a ter as permissões do Cuidador (RF-049, RF-047).

**Exemplo de pedido:**

```json
{
  "papel": "Cuidador"
}
```

**Resposta de sucesso:** `200`

```json
{
  "utilizadorId": "00000000-0000-4000-8000-000000000012",
  "nome": "Rui Fictício",
  "email": "rui@exemplo.pt",
  "papel": "Cuidador",
  "principal": false,
  "provisorio": false
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido; a resposta indica o campo e o valor ou formato esperado (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 404 | `PESSOA_NAO_ENCONTRADA` | A pessoa não está associada à ficha. |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |

#### OP-24 Despromover Cuidador a Familiar

| Campo | Valor |
|---|---|
| Método e caminho | `PUT /api/v1/fichas/{fichaId}/pessoas/{utilizadorId}/papel` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F004 |
| Requisitos | RF-033, RF-048, RF-130, RF-133, RF-134 |
| Casos de uso | UC-15 |

**Regras:** Mesmo endpoint da promoção (OP-23); o corpo indica o papel pretendido. O Cuidador despromovido passa a ter apenas as permissões do Familiar (RF-130, RF-048). A operação é recusada se o alvo for o titular da ficha própria (RF-033), o Cuidador principal sem que outro Cuidador tenha assumido o papel (RF-133) ou o único Cuidador da ficha (RF-134).

**Exemplo de pedido:**

```json
{
  "papel": "Familiar"
}
```

**Resposta de sucesso:** `200`

```json
{
  "utilizadorId": "00000000-0000-4000-8000-000000000012",
  "nome": "Rui Fictício",
  "email": "rui@exemplo.pt",
  "papel": "Familiar",
  "principal": false,
  "provisorio": false
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido; a resposta indica o campo e o valor ou formato esperado (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 403 | `CUIDADOR_PRINCIPAL_PROTEGIDO` | Tentativa de despromover o titular da sua ficha própria (RF-033). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 404 | `PESSOA_NAO_ENCONTRADA` | A pessoa não está associada à ficha. |
| 409 | `CUIDADOR_PRINCIPAL_SEM_SUBSTITUTO` | Despromoção do Cuidador principal sem que outro Cuidador tenha assumido o papel; a resposta indica que o papel se transfere antes com OP-26 (RF-133). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |
| 409 | `FICHA_SEM_CUIDADOR` | A operação deixaria a ficha sem Cuidador; a resposta indica que deve primeiro promover um Familiar ou convidar um novo Cuidador (RF-133, RF-134). |

#### OP-25 Remover acesso de uma pessoa

| Campo | Valor |
|---|---|
| Método e caminho | `DELETE /api/v1/fichas/{fichaId}/pessoas/{utilizadorId}` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F004 |
| Requisitos | RF-033, RF-131, RF-133, RF-134 |
| Casos de uso | UC-15 |

**Regras:** Remove o acesso da pessoa à ficha; a pessoa removida deixa de ver a ficha e passa a receber `404` (RF-131, RNF-005). A operação é recusada se o alvo for o titular da ficha própria (RF-033), o Cuidador principal sem que outro Cuidador tenha assumido o papel (RF-133) ou o único Cuidador da ficha (RF-134).

**Resposta de sucesso:** `204` (sem corpo)

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 403 | `CUIDADOR_PRINCIPAL_PROTEGIDO` | Tentativa de remover o titular da sua ficha própria (RF-033). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 404 | `PESSOA_NAO_ENCONTRADA` | A pessoa não está associada à ficha. |
| 409 | `CUIDADOR_PRINCIPAL_SEM_SUBSTITUTO` | Remoção do Cuidador principal sem que outro Cuidador tenha assumido o papel; a resposta indica que o papel se transfere antes com OP-26 (RF-133). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |
| 409 | `FICHA_SEM_CUIDADOR` | A operação deixaria a ficha sem Cuidador; a resposta indica que deve primeiro promover um Familiar ou convidar um novo Cuidador (RF-133, RF-134). |

#### OP-26 Transferir o papel de Cuidador principal

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/fichas/{fichaId}/cuidador-principal` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F004 |
| Requisitos | RF-034, RF-132, RF-133, RNF-046 |
| Casos de uso | UC-16 |

**Regras:** Atribui o papel de Cuidador principal a outro Cuidador da mesma ficha: o destinatário passa a Cuidador principal, o anterior continua Cuidador e a ficha tem sempre exatamente um Cuidador principal, numa só transação (RF-132, RNF-046). Depois da transferência, o anterior Cuidador principal pode ser despromovido ou removido (RF-133). Não se aplica à ficha própria, em que o titular mantém sempre o papel (RF-034).

**Exemplo de pedido:**

```json
{
  "utilizadorId": "00000000-0000-4000-8000-000000000012"
}
```

**Resposta de sucesso:** `200`

```json
{
  "utilizadorId": "00000000-0000-4000-8000-000000000012",
  "nome": "Rui Fictício",
  "email": "rui@exemplo.pt",
  "papel": "Cuidador",
  "principal": true,
  "provisorio": false
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido; a resposta indica o campo e o valor ou formato esperado (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 403 | `CUIDADOR_PRINCIPAL_PROTEGIDO` | Ficha própria: o titular é sempre o Cuidador principal e o papel não se transfere (RF-034). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 409 | `DESTINATARIO_NAO_E_CUIDADOR` | A pessoa indicada não é Cuidador da ficha; promova-a ou convide-a como Cuidador primeiro (RF-132). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |

### 5.4 F005: Registo de medicação

#### OP-27 Registar tratamento

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/fichas/{fichaId}/tratamentos` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F005 |
| Requisitos | RF-050, RF-051, RF-052, RF-053, RF-060, RNF-006, RNF-008, RNF-014, RNF-046 |
| Casos de uso | UC-17 |

**Parâmetros e notas:** `multipart/form-data` quando há receita (câmara ou galeria no frontend mobile), com a parte `dados` e a parte `receita` (secção 3.6); caso contrário `application/json`. São obrigatórios `medicamento`, `dose`, `horarios` (uma ou mais horas fixas por dia, `HH:mm`), `dataInicio` e `grauImportancia`, que tem um só valor entre `Leve`, `Moderado` e `Grave` (RF-050, RF-051). `dataFim` é opcional e não pode ser anterior a `dataInicio` (RF-050); sem `dataFim` o tratamento é contínuo até ser terminado (OP-31) e as tomas são geradas numa janela móvel de 30 dias (RF-060). A receita é opcional e segue o RNF-008 (RF-052).

Ao registar, o servidor gera as tomas previstas, todas no estado `Pendente` (RF-060; secção 4). A operação é atómica: se a receita não puder ser guardada, o tratamento não é registado e nenhum dado é guardado (RF-053, RNF-046). Só um Cuidador regista; o Familiar recebe `403` (RF-050).

**Exemplo de pedido** (`application/json`, ou parte `dados` do pedido `multipart/form-data`):

```json
{
  "medicamento": "Metformina",
  "dose": "500 mg",
  "grauImportancia": "Moderado",
  "dataInicio": "2026-10-07",
  "dataFim": "2026-11-05",
  "horarios": [
    "08:00",
    "20:00"
  ]
}
```

**Resposta de sucesso:** `201`

```json
{
  "id": "00000000-0000-4000-8000-000000000201",
  "fichaId": "00000000-0000-4000-8000-000000000101",
  "medicamento": "Metformina",
  "dose": "500 mg",
  "grauImportancia": "Moderado",
  "dataInicio": "2026-10-07",
  "dataFim": "2026-11-05",
  "horarios": [
    "08:00",
    "20:00"
  ],
  "estado": "EmCurso",
  "receitaDocumentoId": "00000000-0000-4000-8000-000000000301"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido, por exemplo `dataFim` anterior a `dataInicio` ou grau de importância em falta ou diferente de Leve, Moderado e Grave; a resposta indica o campo em erro (RF-050, RF-051, RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-050, RF-121). |
| 413 | `FICHEIRO_DEMASIADO_GRANDE` | Receita com mais de 10 MB (RNF-008). |
| 415 | `FORMATO_NAO_SUPORTADO` | Formato diferente de PDF, JPG ou PNG (RNF-008). |
| 503 | `DOCUMENTO_NAO_GUARDADO` | A receita não pôde ser guardada; o tratamento não é registado e nada é gravado (RF-053). |

#### OP-28 Listar tratamentos da ficha

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/fichas/{fichaId}/tratamentos` |
| Autenticação e autorização | Sessão válida; Cuidador principal, Cuidador ou Familiar da ficha |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F005 |
| Requisitos | RF-054, RF-057 |
| Casos de uso | UC-18 |

**Parâmetros e notas:** Query opcional: `estado=EmCurso|Terminado`. Sem o parâmetro, devolve os tratamentos em curso e os terminados, identificados pelo `estado` (RF-054). Disponível para Cuidadores e Familiares associados. Um tratamento fica `Terminado` quando o Cuidador o termina (OP-31) ou quando passa a data de fim prevista, e mantém os seus dados (RF-056, RF-057).

**Resposta de sucesso:** `200`

```json
[
  {
    "id": "00000000-0000-4000-8000-000000000201",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "medicamento": "Metformina",
    "dose": "500 mg",
    "grauImportancia": "Moderado",
    "dataInicio": "2026-10-07",
    "dataFim": "2026-11-05",
    "horarios": [
      "08:00",
      "20:00"
    ],
    "estado": "EmCurso",
    "receitaDocumentoId": "00000000-0000-4000-8000-000000000301"
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |

#### OP-29 Consultar tratamento

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/tratamentos/{tratamentoId}` |
| Autenticação e autorização | Sessão válida; Cuidador principal, Cuidador ou Familiar da ficha |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F005 |
| Requisitos | RF-054, RF-057 |
| Casos de uso | UC-18 |

**Parâmetros e notas:** As tomas do tratamento, incluindo as de um tratamento terminado, obtêm-se em OP-32 com o parâmetro `tratamentoId` (RF-057).

**Resposta de sucesso:** `200`

```json
{
  "id": "00000000-0000-4000-8000-000000000201",
  "fichaId": "00000000-0000-4000-8000-000000000101",
  "medicamento": "Metformina",
  "dose": "500 mg",
  "grauImportancia": "Moderado",
  "dataInicio": "2026-10-07",
  "dataFim": "2026-11-05",
  "horarios": [
    "08:00",
    "20:00"
  ],
  "estado": "EmCurso",
  "receitaDocumentoId": "00000000-0000-4000-8000-000000000301"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 404 | `RECURSO_NAO_ENCONTRADO` | O recurso não existe ou pertence a uma ficha a que o Utilizador não está associado (RNF-005). |

#### OP-30 Atualizar tratamento

| Campo | Valor |
|---|---|
| Método e caminho | `PUT /api/v1/tratamentos/{tratamentoId}` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F005 |
| Requisitos | RF-051, RF-052, RF-053, RF-055, RF-060, RNF-008, RNF-046 |
| Casos de uso | UC-18 |

**Parâmetros e notas:** `multipart/form-data` quando há receita nova, com a parte `dados` e a parte `receita` (secção 3.6); caso contrário `application/json`. Os campos que não forem enviados mantêm o valor; `dataInicio` não pode ser alterada. O corpo indica os dados a alterar, por exemplo a dose, o grau de importância, a `dataFim` (não anterior a `dataInicio`), os horários e a receita (RF-055). Só se altera um tratamento `EmCurso`. Alterar os horários só afeta as tomas futuras: as tomas anteriores mantêm a hora e o estado (RF-060). Se a receita não puder ser guardada, a operação é recusada e o tratamento fica exatamente como estava (RF-053, RNF-046). Só um Cuidador altera; o Familiar recebe `403` (RF-055).

**Exemplo de pedido** (`application/json`, ou parte `dados` do pedido `multipart/form-data`):

```json
{
  "dose": "850 mg",
  "grauImportancia": "Grave",
  "dataFim": "2026-11-05",
  "horarios": [
    "08:00",
    "20:00"
  ]
}
```

**Resposta de sucesso:** `200`

```json
{
  "id": "00000000-0000-4000-8000-000000000201",
  "fichaId": "00000000-0000-4000-8000-000000000101",
  "medicamento": "Metformina",
  "dose": "850 mg",
  "grauImportancia": "Grave",
  "dataInicio": "2026-10-07",
  "dataFim": "2026-11-05",
  "horarios": [
    "08:00",
    "20:00"
  ],
  "estado": "EmCurso",
  "receitaDocumentoId": "00000000-0000-4000-8000-000000000301"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido; a resposta indica o campo e o valor ou formato esperado (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `RECURSO_NAO_ENCONTRADO` | O recurso não existe ou pertence a uma ficha a que o Utilizador não está associado (RNF-005). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |
| 409 | `TRATAMENTO_TERMINADO` | Um tratamento terminado não pode ser alterado (RF-055). |
| 413 | `FICHEIRO_DEMASIADO_GRANDE` | Receita com mais de 10 MB (RNF-008). |
| 415 | `FORMATO_NAO_SUPORTADO` | Formato diferente de PDF, JPG ou PNG (RNF-008). |
| 503 | `DOCUMENTO_NAO_GUARDADO` | A receita não pôde ser guardada; nada é alterado (RF-053). |

#### OP-31 Terminar tratamento antecipadamente

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/tratamentos/{tratamentoId}/termino` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F005 |
| Requisitos | RF-056, RF-057, RF-060, RNF-049 |
| Casos de uso | UC-19 |

**Regras:** Passa o tratamento ao estado `Terminado` e deixa de gerar tomas a partir desse momento: as tomas previstas posteriores deixam de existir e as anteriores mantêm a hora e o estado (RF-056, RF-060). Aplica-se também a um tratamento sem data de fim. Os dados do tratamento e as tomas registadas continuam consultáveis (RF-057, RNF-049).

**Resposta de sucesso:** `200`

```json
{
  "id": "00000000-0000-4000-8000-000000000201",
  "fichaId": "00000000-0000-4000-8000-000000000101",
  "medicamento": "Metformina",
  "dose": "500 mg",
  "grauImportancia": "Moderado",
  "dataInicio": "2026-10-07",
  "dataFim": "2026-11-05",
  "horarios": [
    "08:00",
    "20:00"
  ],
  "estado": "Terminado",
  "receitaDocumentoId": "00000000-0000-4000-8000-000000000301"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `RECURSO_NAO_ENCONTRADO` | O recurso não existe ou pertence a uma ficha a que o Utilizador não está associado (RNF-005). |
| 409 | `TRATAMENTO_JA_TERMINADO` | O tratamento já está terminado. |

### 5.5 F006: Horários e alarmes com confirmação de toma

#### OP-32 Listar tomas da ficha

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/fichas/{fichaId}/tomas` |
| Autenticação e autorização | Sessão válida; Cuidador principal, Cuidador ou Familiar da ficha |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F006 |
| Requisitos | RF-057, RF-063, RF-065, RF-066, RF-067, RF-068 |
| Casos de uso | UC-20, UC-22, UC-35 |

**Parâmetros e notas:** Query: `de` e `ate` (datas `AAAA-MM-DD`, obrigatórias, em hora de Lisboa; `ate` inclui o dia inteiro e o intervalo tem no máximo 31 dias, contando os dois extremos); opcionais `estado` (`Pendente`, `Confirmada`, `ConfirmadaComAtraso` ou `NaoConfirmada`) e `tratamentoId`. Disponível para Cuidadores e Familiares associados (RF-063). Cada toma indica a data e a hora previstas, o medicamento, a dose e o estado e, nas confirmadas, quem confirmou e quando (RF-063, RF-065). As tomas vêm ordenadas pela hora prevista. Os parâmetros são validados antes da autorização: `de` em falta dá erro no campo `de`; `ate` em falta, anterior a `de` ou a mais de 31 dias dá erro no campo `ate`. `confirmadaPor.nome` é o nome atual da conta de quem confirmou.

Uma toma nasce `Pendente` e mantém-se `Pendente` mesmo depois de terminada a tolerância do seu grau; passa a `NaoConfirmada` 24 horas depois da hora prevista, se continuar sem confirmação (RF-066, RF-068).

**Resposta de sucesso:** `200`

```json
[
  {
    "id": "00000000-0000-4000-8000-000000000402",
    "tratamentoId": "00000000-0000-4000-8000-000000000201",
    "medicamento": "Metformina",
    "dose": "500 mg",
    "grauImportancia": "Moderado",
    "previstaEm": "2026-10-06T20:00:00+01:00",
    "estado": "ConfirmadaComAtraso",
    "confirmadaPor": {
      "id": "00000000-0000-4000-8000-000000000011",
      "nome": "Ana Fictícia"
    },
    "confirmadaEm": "2026-10-06T21:30:00+01:00"
  },
  {
    "id": "00000000-0000-4000-8000-000000000401",
    "tratamentoId": "00000000-0000-4000-8000-000000000201",
    "medicamento": "Metformina",
    "dose": "500 mg",
    "grauImportancia": "Moderado",
    "previstaEm": "2026-10-07T08:00:00+01:00",
    "estado": "Pendente",
    "confirmadaPor": null,
    "confirmadaEm": null
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | `de` ou `ate` em falta ou com formato inválido, `ate` anterior a `de` ou intervalo com mais de 31 dias; a resposta indica o campo e a regra (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |

#### OP-33 Confirmar toma

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/tomas/{tomaId}/confirmacao` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F006 |
| Requisitos | RF-064, RF-065, RF-067, RF-068, RF-102, RF-105, RNF-011, RNF-044 |
| Casos de uso | UC-22 |

**Regras:**

- Só um Cuidador da ficha confirma; o Familiar recebe `403` e a toma mantém o estado `Pendente` (RF-064).
- Uma toma pode ser confirmada desde as 00:00 do dia previsto (hora de Lisboa) até passar ao estado `NaoConfirmada`, 24 horas depois da hora prevista. Antes das 00:00 do dia previsto a confirmação é recusada; uma toma `NaoConfirmada` é um estado final e já não pode ser confirmada (RF-064, RF-068).
- O estado resultante depende da tolerância do grau, fixa: Leve 2 horas, Moderado 1 hora, Grave 30 minutos. Confirmada até ao fim da tolerância, inclusive, fica `Confirmada`; confirmada depois, fica `ConfirmadaComAtraso` (RF-067). Uma confirmação antes da hora prevista, no mesmo dia, fica `Confirmada` (RF-064).
- O servidor regista o Cuidador que confirma e a data e hora da confirmação (RF-065).
- Uma toma já confirmada não se confirma de novo: o pedido é recusado com `409`, a resposta indica quem confirmou e quando, e o registo mantém a primeira confirmação, mesmo que dois Cuidadores confirmem em simultâneo: o servidor deteta o conflito pela versão da toma e responde `TOMA_JA_CONFIRMADA` ao segundo pedido (RF-105, RNF-044).
- Ordem das verificações: toma inexistente ou de ficha sem associação (`404`), Familiar (`403`) e, depois, as regras da janela e do estado (`409`).
- Numa ficha arquivada continuam a poder ser confirmadas as tomas que ficaram por confirmar antes do arquivo: esta operação não devolve `FICHA_ARQUIVADA`.
- É a operação usada para confirmar uma toma a partir da lista diária, sem abrir a ficha (RF-102). O resultado é devolvido em 1 segundo no máximo (RNF-011).

Exemplo de corpo da resposta `409` de confirmação repetida:

```json
{
  "codigo": "TOMA_JA_CONFIRMADA",
  "mensagem": "A toma já foi confirmada por Ana Fictícia em 07/10/2026 às 08:05.",
  "confirmacao": {
    "confirmadaPor": {
      "id": "00000000-0000-4000-8000-000000000011",
      "nome": "Ana Fictícia"
    },
    "confirmadaEm": "2026-10-07T08:05:00+01:00"
  }
}
```

**Resposta de sucesso:** `200`

```json
{
  "id": "00000000-0000-4000-8000-000000000401",
  "tratamentoId": "00000000-0000-4000-8000-000000000201",
  "medicamento": "Metformina",
  "dose": "500 mg",
  "grauImportancia": "Moderado",
  "previstaEm": "2026-10-07T08:00:00+01:00",
  "estado": "Confirmada",
  "confirmadaPor": {
    "id": "00000000-0000-4000-8000-000000000011",
    "nome": "Ana Fictícia"
  },
  "confirmadaEm": "2026-10-07T08:05:00+01:00"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `RECURSO_NAO_ENCONTRADO` | O recurso não existe ou pertence a uma ficha a que o Utilizador não está associado (RNF-005). |
| 409 | `TOMA_FORA_DA_JANELA` | A toma só pode ser confirmada a partir das 00:00 do dia previsto, hora de Lisboa (RF-064). |
| 409 | `TOMA_JA_CONFIRMADA` | A toma já foi confirmada; o pedido é recusado, o corpo inclui quem confirmou e quando (`confirmacao`) e o registo mantém a primeira confirmação (RF-105, RNF-044). |
| 409 | `TOMA_NAO_CONFIRMAVEL` | A toma está `NaoConfirmada`, estado final, e já não pode ser confirmada (RF-064, RF-068). |

#### OP-34 Sincronizar o calendário de tomas

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/calendario/tomas` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F006 |
| Requisitos | RF-061, RF-062, RF-069 |
| Casos de uso | UC-21 |

**Parâmetros e notas:** Sem parâmetros. Devolve as tomas `Pendente` previstas para as 48 horas seguintes ao instante do pedido, das fichas ativas em que o Utilizador é Cuidador (RF-061). As fichas em que é apenas Familiar nunca são incluídas (RF-061) e as fichas arquivadas também não (RF-069); se não houver tomas, devolve a lista vazia.

O frontend mobile chama esta operação sempre que a aplicação é aberta ou o ecrã é atualizado com ligação à Internet e agenda localmente, para cada toma, o aviso à hora prevista, com a ficha, o medicamento e a dose (RF-062). Sem ligação, o dispositivo mantém as tomas da última sincronização bem-sucedida. Sem notificações push, uma alteração à medicação só chega ao dispositivo na sincronização seguinte. Se a aplicação não for aberta durante mais de 48 horas, os avisos já sincronizados esgotam-se e deixam de existir até à abertura seguinte: não há sincronização em segundo plano nem renovação automática da sessão, que dura 8 horas (RF-061).

**Resposta de sucesso:** `200`

```json
[
  {
    "tomaId": "00000000-0000-4000-8000-000000000403",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "nomeFicha": "Maria Fictícia Santos",
    "medicamento": "Metformina",
    "dose": "500 mg",
    "previstaEm": "2026-10-07T20:00:00+01:00"
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |

### 5.6 F007: Tarefas de cuidados recorrentes (retirada do âmbito)

A funcionalidade F007 é retirada do âmbito pela alteração autorizada de âmbito de 2026-10-19. As operações OP-35 a OP-40 deixam de existir e os seus identificadores não são reutilizados; o código de erro `EXECUCAO_JA_CONFIRMADA` deixa de existir.

### 5.7 F008: Agenda de consultas e calendário de cuidados

#### OP-41 Registar compromisso

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/fichas/{fichaId}/compromissos` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F008 |
| Requisitos | RF-080, RF-116, RF-117 |
| Casos de uso | UC-25 |

**Parâmetros e notas:** São obrigatórios `dataHora` (ISO 8601 com o desvio; pode estar no passado e é guardada ao segundo), `local` (até 150 caracteres) e `motivo` (até 250 caracteres) (RF-080). `antecedenciaAvisoMinutos` é a antecedência da notificação e só aceita 30, 60, 120, 1440 ou 2880 minutos, isto é, 30 minutos, 1, 2, 24 ou 48 horas (RF-116); por omissão, 1440 (24 horas) (RF-080). Pelo contrato, quando faltar esse tempo para o compromisso os Cuidadores e os Familiares da ficha são notificados e, se o momento do aviso já tiver passado ao registar, a notificação é criada nesse momento (RF-117); no código confrontado esta notificação ainda não é criada (secções 4 e 8). Só um Cuidador regista; o Familiar recebe `403`. A ordem das verificações é: ficha (`404`), papel (`403`), ficha arquivada (`409`) e só depois os dados (`400`).

**Exemplo de pedido:**

```json
{
  "dataHora": "2026-10-12T10:30:00+01:00",
  "local": "Centro de Saúde Fictício",
  "motivo": "Consulta de rotina",
  "antecedenciaAvisoMinutos": 60
}
```

**Resposta de sucesso:** `201`

```json
{
  "id": "00000000-0000-4000-8000-000000000601",
  "fichaId": "00000000-0000-4000-8000-000000000101",
  "dataHora": "2026-10-12T10:30:00+01:00",
  "local": "Centro de Saúde Fictício",
  "motivo": "Consulta de rotina",
  "antecedenciaAvisoMinutos": 60
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta (por exemplo sem local) ou inválido, incluindo antecedência de aviso fora dos valores permitidos; a resposta indica o campo em erro (RF-080, RF-116, RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-080, RF-121). |

#### OP-42 Listar compromissos da ficha

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/fichas/{fichaId}/compromissos` |
| Autenticação e autorização | Sessão válida; Cuidador principal, Cuidador ou Familiar da ficha |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F008 |
| Requisitos | RF-080, RF-116 |
| Casos de uso | UC-25, UC-26 |

**Parâmetros e notas:** Query opcional: `de` e `ate` (datas `AAAA-MM-DD` em hora de Lisboa, aplicadas a `dataHora`; `ate` inclui o dia inteiro). `ate` anterior a `de` dá `400`, verificado antes da autorização. Os compromissos vêm ordenados por `dataHora`. Disponível para Cuidadores e Familiares associados, também numa ficha arquivada. Cada compromisso indica a data, a hora, o local, o motivo e a antecedência de aviso (RF-080, RF-116).

**Resposta de sucesso:** `200`

```json
[
  {
    "id": "00000000-0000-4000-8000-000000000601",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "dataHora": "2026-10-12T10:30:00+01:00",
    "local": "Centro de Saúde Fictício",
    "motivo": "Consulta de rotina",
    "antecedenciaAvisoMinutos": 60
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | `de` ou `ate` com formato inválido, ou `ate` anterior a `de`; a resposta indica o campo e a regra (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |

#### OP-43 Editar compromisso

| Campo | Valor |
|---|---|
| Método e caminho | `PUT /api/v1/compromissos/{compromissoId}` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F008 |
| Requisitos | RF-081, RF-116, RF-117 |
| Casos de uso | UC-25 |

**Parâmetros e notas:** Altera a data, a hora, o local, o motivo e a antecedência de aviso, com os valores permitidos de OP-41 (RF-081, RF-116). Todos os campos são enviados, com as regras de OP-41; sem `antecedenciaAvisoMinutos`, a antecedência volta a 1440 (24 horas). Pelo contrato, a notificação de antecedência passa a ser calculada com os novos valores (RF-117; não implementada no código confrontado, secção 8). Só um Cuidador edita; o Familiar recebe `403`. A ordem das verificações é a de OP-41: compromisso (`404`), papel (`403`), ficha arquivada (`409`) e dados (`400`).

**Exemplo de pedido:**

```json
{
  "dataHora": "2026-10-12T11:00:00+01:00",
  "local": "Centro de Saúde Fictício",
  "motivo": "Consulta de rotina",
  "antecedenciaAvisoMinutos": 120
}
```

**Resposta de sucesso:** `200`

```json
{
  "id": "00000000-0000-4000-8000-000000000601",
  "fichaId": "00000000-0000-4000-8000-000000000101",
  "dataHora": "2026-10-12T11:00:00+01:00",
  "local": "Centro de Saúde Fictício",
  "motivo": "Consulta de rotina",
  "antecedenciaAvisoMinutos": 120
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido; a resposta indica o campo e o valor ou formato esperado (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `RECURSO_NAO_ENCONTRADO` | O recurso não existe ou pertence a uma ficha a que o Utilizador não está associado (RNF-005). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |

#### OP-44 Remover compromisso

| Campo | Valor |
|---|---|
| Método e caminho | `DELETE /api/v1/compromissos/{compromissoId}` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F008 |
| Requisitos | RF-082, RF-117 |
| Casos de uso | UC-25 |

**Regras:** O compromisso é eliminado e deixa de aparecer na agenda e na lista diária; pelo contrato, a notificação de antecedência deixa de ser emitida (RF-082, RF-117). Só um Cuidador remove; o Familiar recebe `403`.

**Resposta de sucesso:** `204` (sem corpo)

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `RECURSO_NAO_ENCONTRADO` | O recurso não existe ou pertence a uma ficha a que o Utilizador não está associado (RNF-005). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |

#### OP-45 Consultar agenda

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/agenda` |
| Autenticação e autorização | Sessão válida (qualquer Utilizador) |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar, Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F008 |
| Requisitos | RF-083, RF-084, RF-085, RF-086, RF-087, RF-088, RF-089, RNF-012, RNF-047 |
| Casos de uso | UC-26 |

**Parâmetros e notas:** Query obrigatória: `vista=dia|semana|mes` (sem distinção entre maiúsculas e minúsculas) e `data` (`AAAA-MM-DD`). A vista `dia` devolve os eventos desse dia ordenados por hora (RF-084); `semana` devolve os eventos da semana de segunda-feira a domingo que contém a data (RF-085); `mes` devolve os eventos do mês que contém a data (RF-086). Os limites do dia, da semana e do mês seguem a hora de Lisboa (RNF-047).

Reúne os compromissos e as tomas previstas das fichas ativas em que o Utilizador é Cuidador ou Familiar; as fichas arquivadas não aparecem e, sem fichas associadas, a lista é vazia (RF-083). Cada evento indica o nome do utente da ficha (`nomeFicha`; RF-089). `tipo` é `Toma` ou `Compromisso`. `titulo` é «Medicamento Dose» nas tomas e o motivo nos compromissos. `estado` só existe nas tomas. `confirmavel` é `true` nas tomas `Pendente` das fichas em que o Utilizador é Cuidador e `false` nos compromissos, que nunca são confirmáveis (RF-087), e nos restantes casos; a janela de confirmação é verificada por OP-33. Os eventos vêm ordenados por `inicio`, depois por `nomeFicha`, pelo tipo (primeiro as tomas) e pelo identificador. As tomas não se alteram a partir da agenda, só no registo do tratamento (OP-30; RF-088).

Os compromissos aparecem na data registada, sem limite. As tomas só existem na janela móvel de 30 dias (tratamentos sem data de fim), por isso a agenda mostra as já geradas, até 30 dias à frente; nos períodos mais distantes só mostra compromissos (RF-083).

**Resposta de sucesso:** `200`

```json
[
  {
    "id": "00000000-0000-4000-8000-000000000401",
    "tipo": "Toma",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "nomeFicha": "Maria Fictícia Santos",
    "titulo": "Metformina 500 mg",
    "inicio": "2026-10-12T08:00:00+01:00",
    "estado": "Pendente",
    "confirmavel": true
  },
  {
    "id": "00000000-0000-4000-8000-000000000601",
    "tipo": "Compromisso",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "nomeFicha": "Maria Fictícia Santos",
    "titulo": "Consulta de rotina",
    "inicio": "2026-10-12T10:30:00+01:00",
    "confirmavel": false
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta ou inválido; a resposta indica o campo e o valor ou formato esperado (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |

### 5.8 F009: Ocorrências diárias e notas partilhadas

#### OP-46 Registar ocorrência

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/fichas/{fichaId}/ocorrencias` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F009 |
| Requisitos | RF-090, RF-092, RF-093, RF-094, RF-115, RNF-049 |
| Casos de uso | UC-27 |

**Regras:** São obrigatórios `descricao` (até 2000 caracteres) e `ocorridaEm`, a data e hora em que a ocorrência aconteceu, com o desvio (RF-090). `ocorridaEm` pode ser posterior ao momento do registo e é guardada ao segundo. A ordem das verificações é: ficha (`404`), papel (`403`), ficha arquivada (`409`) e só depois os dados (`400`). `autor.nome` é o nome atual da conta do autor. O servidor regista o autor e o momento do registo (`criadaEm`), que podem diferir de `ocorridaEm` (RF-092). Depois de registada, a ocorrência não pode ser alterada nem eliminada (RF-090, RNF-049; secção 4). Fica disponível para os restantes Cuidadores e para os Familiares da ficha quando abrem ou atualizam o ecrã (RF-093, RF-094). Notifica todos os Cuidadores e Familiares da ficha, exceto o autor, na aplicação e por e-mail, sem incluir a descrição na mensagem (RF-115; secção 4). Só um Cuidador regista; o Familiar recebe `403`.

**Exemplo de pedido:**

```json
{
  "descricao": "Queda ligeira sem lesões.",
  "ocorridaEm": "2026-10-07T15:05:00+01:00"
}
```

**Resposta de sucesso:** `201`

```json
{
  "id": "00000000-0000-4000-8000-000000000701",
  "fichaId": "00000000-0000-4000-8000-000000000101",
  "tipo": "Ocorrencia",
  "descricao": "Queda ligeira sem lesões.",
  "autor": {
    "id": "00000000-0000-4000-8000-000000000011",
    "nome": "Ana Fictícia"
  },
  "ocorridaEm": "2026-10-07T15:05:00+01:00",
  "criadaEm": "2026-10-07T15:20:00+01:00"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta (por exemplo sem descrição) ou inválido; a resposta indica o campo em erro (RF-090, RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |

#### OP-47 Registar nota partilhada

| Campo | Valor |
|---|---|
| Método e caminho | `POST /api/v1/fichas/{fichaId}/notas` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F009 |
| Requisitos | RF-091, RF-092, RF-093, RF-095 |
| Casos de uso | UC-28 |

**Regras:** É obrigatório o texto da nota, no campo `descricao`, até 2000 caracteres (RF-091). A ordem das verificações é a de OP-46. O servidor regista o autor e o momento do registo (RF-092). A nota é dirigida aos Cuidadores: fica disponível para os restantes Cuidadores da ficha quando abrem ou atualizam o ecrã (RF-093) e não é visível ao Familiar (RF-095). Depois de registada, não pode ser alterada nem eliminada (secção 4) e não gera notificações (RF-115). Só um Cuidador regista; o Familiar recebe `403` (RF-091).

**Exemplo de pedido:**

```json
{
  "descricao": "Reforçar a hidratação durante a tarde."
}
```

**Resposta de sucesso:** `201`

```json
{
  "id": "00000000-0000-4000-8000-000000000702",
  "fichaId": "00000000-0000-4000-8000-000000000101",
  "tipo": "Nota",
  "descricao": "Reforçar a hidratação durante a tarde.",
  "autor": {
    "id": "00000000-0000-4000-8000-000000000011",
    "nome": "Ana Fictícia"
  },
  "ocorridaEm": null,
  "criadaEm": "2026-10-07T15:20:00+01:00"
}
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | Campo em falta (por exemplo nota sem texto) ou inválido; a resposta indica o campo em erro (RF-091, RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |
| 409 | `FICHA_ARQUIVADA` | A ficha está arquivada; a operação só é possível depois de a reativar (RF-121). |

#### OP-48 Consultar ocorrências

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/fichas/{fichaId}/registos?tipo=Ocorrencia` |
| Autenticação e autorização | Sessão válida; Cuidador principal, Cuidador ou Familiar da ficha |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F009 |
| Requisitos | RF-092, RF-093, RF-094 |
| Casos de uso | UC-29 |

**Parâmetros e notas:** Da mais recente para a mais antiga, pelo momento do registo (`criadaEm`). Query: `tipo` obrigatório (`Ocorrencia` ou `Nota`); `de` e `ate` opcionais (datas `AAAA-MM-DD` em hora de Lisboa, aplicadas a esse momento; `ate` inclui o dia inteiro). `tipo` em falta ou inválido dá `400` antes de qualquer outra verificação; `ate` anterior a `de` dá `400` depois da autorização. `autor.nome` é o nome atual da conta do autor. Disponível para Cuidadores e Familiares associados (RF-094). Cada ocorrência indica o autor, a data e hora em que aconteceu e a data e hora do registo (RF-092).

**Resposta de sucesso:** `200`

```json
[
  {
    "id": "00000000-0000-4000-8000-000000000701",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "tipo": "Ocorrencia",
    "descricao": "Queda ligeira sem lesões.",
    "autor": {
      "id": "00000000-0000-4000-8000-000000000011",
      "nome": "Ana Fictícia"
    },
    "ocorridaEm": "2026-10-07T15:05:00+01:00",
    "criadaEm": "2026-10-07T15:20:00+01:00"
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | `tipo` em falta ou inválido, `de` ou `ate` com formato inválido, ou `ate` anterior a `de`; a resposta indica o campo e a regra (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |

#### OP-49 Consultar notas partilhadas

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/fichas/{fichaId}/registos?tipo=Nota` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F009 |
| Requisitos | RF-091, RF-092, RF-093, RF-095 |
| Casos de uso | UC-29 |

**Parâmetros e notas:** Da mais recente para a mais antiga, pelo momento do registo (`criadaEm`). Query: `tipo` obrigatório (`Ocorrencia` ou `Nota`); `de` e `ate` opcionais (datas `AAAA-MM-DD` em hora de Lisboa, aplicadas a esse momento; `ate` inclui o dia inteiro). `tipo` em falta ou inválido dá `400` antes de qualquer outra verificação; `ate` anterior a `de` dá `400` depois da autorização. `autor.nome` é o nome atual da conta do autor. As notas são dirigidas aos Cuidadores (DA, F009): um Familiar associado recebe `403` (RF-095). Os Cuidadores veem as notas dos outros Cuidadores quando abrem ou atualizam o ecrã (RF-093).

**Resposta de sucesso:** `200`

```json
[
  {
    "id": "00000000-0000-4000-8000-000000000702",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "tipo": "Nota",
    "descricao": "Reforçar a hidratação durante a tarde.",
    "autor": {
      "id": "00000000-0000-4000-8000-000000000011",
      "nome": "Ana Fictícia"
    },
    "ocorridaEm": null,
    "criadaEm": "2026-10-07T15:20:00+01:00"
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | `tipo` em falta ou inválido, `de` ou `ate` com formato inválido, ou `ate` anterior a `de`; a resposta indica o campo e a regra (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |
| 403 | `ACESSO_PROIBIDO` | O papel do Utilizador na ficha não permite a operação, por exemplo um Familiar (RNF-004). |
| 404 | `FICHA_NAO_ENCONTRADA` | A ficha não existe ou o Utilizador não está associado a ela (RNF-005). |

### 5.9 F010: Lista de tarefas diária consolidada

#### OP-50 Consultar lista de tarefas diária

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/lista-diaria` |
| Autenticação e autorização | Sessão válida; Cuidador principal ou Cuidador da ficha |
| Papéis permitidos | Cuidador principal, Cuidador (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F010 |
| Requisitos | RF-087, RF-100, RF-101, RF-102, RF-104, RNF-010, RNF-047 |
| Casos de uso | UC-30 |

**Parâmetros e notas:** Query opcional: `data` (só `AAAA-MM-DD`; por omissão, o dia de hoje na hora de Lisboa, RNF-047). Sem fichas ativas em que o Utilizador seja Cuidador, a resposta é `200` com a lista vazia. Reúne, numa só consulta, as tomas e os compromissos do dia das fichas ativas em que o Utilizador é Cuidador (RF-100). As fichas arquivadas e as fichas em que o Utilizador é apenas Familiar não aparecem. Quando a data é a de hoje, a lista inclui também as tomas do dia anterior que continuam `Pendente` (RF-100); para outras datas, não. Cada item indica o nome do utente da ficha (`nomeFicha`; RF-101).

As tomas confirmam-se diretamente a partir da lista com OP-33, sem abrir a ficha (RF-102). Uma confirmação feita por outro Cuidador aparece como realizada, com o autor e o momento, quando a lista é aberta ou atualizada (RF-104). Os compromissos são apenas de consulta: `confirmavel` é `false` e não existe forma de os confirmar (RF-087).

`titulo` é «Medicamento Dose» nas tomas e o motivo nos compromissos. Numa toma, `confirmavel` é `true` só se estiver `Pendente`, a janela de confirmação já tiver aberto (00:00 do dia previsto, hora de Lisboa) e ainda não tiverem passado 24 horas da hora prevista. `confirmadaPor` (nome atual da conta) e `confirmadaEm` só existem nas tomas `Confirmada` e `ConfirmadaComAtraso`; `estado` só existe nas tomas. Os itens vêm ordenados por `previstaEm`, depois pelo tipo (primeiro as tomas), por `nomeFicha`, por `titulo` e pelo identificador.

**Resposta de sucesso:** `200`

```json
[
  {
    "tipo": "Toma",
    "id": "00000000-0000-4000-8000-000000000409",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "nomeFicha": "Maria Fictícia Santos",
    "titulo": "Paracetamol 1 g",
    "previstaEm": "2026-10-06T23:30:00+01:00",
    "estado": "Pendente",
    "confirmavel": true
  },
  {
    "tipo": "Toma",
    "id": "00000000-0000-4000-8000-000000000401",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "nomeFicha": "Maria Fictícia Santos",
    "titulo": "Metformina 500 mg",
    "previstaEm": "2026-10-07T08:00:00+01:00",
    "estado": "Confirmada",
    "confirmadaPor": "Ana Fictícia",
    "confirmadaEm": "2026-10-07T08:05:00+01:00",
    "confirmavel": false
  },
  {
    "tipo": "Compromisso",
    "id": "00000000-0000-4000-8000-000000000601",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "nomeFicha": "Maria Fictícia Santos",
    "titulo": "Consulta de rotina",
    "previstaEm": "2026-10-07T10:30:00+01:00",
    "confirmavel": false
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | `data` com formato diferente de `AAAA-MM-DD` ou data inexistente; a resposta indica o campo e a regra (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |

### 5.10 F011: Notificações

#### OP-51 Consultar histórico de notificações

| Campo | Valor |
|---|---|
| Método e caminho | `GET /api/v1/notificacoes` |
| Autenticação e autorização | Sessão válida (qualquer Utilizador) |
| Papéis permitidos | Cuidador principal, Cuidador, Familiar, Utilizador com convite pendente (ver `m3-matriz-permissoes-ficha-v02.md`) |
| Funcionalidade | F011 |
| Requisitos | RF-041, RF-110, RF-111, RF-112, RF-113, RF-115, RF-117, RF-118, RF-119, RF-140, RF-141 |
| Casos de uso | UC-34 |

**Parâmetros e notas:** Devolve as notificações recebidas pelo Utilizador autenticado, da mais recente para a mais antiga, e nunca as destinadas a outros Utilizadores (RF-140). Query opcional: `de` e `ate` (datas `AAAA-MM-DD` em hora de Lisboa, aplicadas a `criadaEm`; `ate` inclui o dia inteiro); `ate` anterior a `de` dá `400`, verificado antes de qualquer consulta. O histórico é atualizado quando o Utilizador abre ou atualiza o ecrã, sem notificações push (RF-140).

`tipo` é `ConviteRecebido` (RF-041), `TomaEmAtraso` (RF-110, RF-111, RF-112), `TomaGraveFamiliares` (RF-113), `NovaOcorrencia` (RF-115) ou `ProximoCompromisso` (RF-117; este tipo ainda não é criado pelo código confrontado, secção 8). Cada notificação indica a ficha (`nomeFicha`); `medicamento` e `previstaEm` só têm valor nas notificações de tomas (`TomaEmAtraso` e `TomaGraveFamiliares`) e são `null` nas restantes. As notificações vêm ordenadas por `criadaEm`, da mais recente para a mais antiga. A notificação na aplicação existe sempre, mesmo que o e-mail falhe (RF-141). O campo `email` indica `Enviado`, `Pendente` (ainda não enviado ou com reenvio em curso, RF-118) ou `NaoEntregue`, quando as 3 tentativas de reenvio falharam (RF-119).

**Resposta de sucesso:** `200`

```json
[
  {
    "id": "00000000-0000-4000-8000-000000000902",
    "tipo": "TomaGraveFamiliares",
    "mensagem": "A toma de Insulina de Maria Fictícia Santos das 10:00 não foi confirmada.",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "nomeFicha": "Maria Fictícia Santos",
    "medicamento": "Insulina",
    "previstaEm": "2026-10-07T10:00:00+01:00",
    "criadaEm": "2026-10-07T11:00:00+01:00",
    "email": "NaoEntregue"
  },
  {
    "id": "00000000-0000-4000-8000-000000000901",
    "tipo": "TomaEmAtraso",
    "mensagem": "A toma de Metformina de Maria Fictícia Santos das 08:00 não foi confirmada.",
    "fichaId": "00000000-0000-4000-8000-000000000101",
    "nomeFicha": "Maria Fictícia Santos",
    "medicamento": "Metformina",
    "previstaEm": "2026-10-07T08:00:00+01:00",
    "criadaEm": "2026-10-07T09:00:00+01:00",
    "email": "Enviado"
  }
]
```

**Erros:**

| Código HTTP | Código de erro | Quando |
|---|---|---|
| 400 | `VALIDACAO_INVALIDA` | `de` ou `ate` com formato inválido, ou `ate` anterior a `de`; a resposta indica o campo e a regra (RNF-028). |
| 401 | `NAO_AUTENTICADO` | Pedido sem token, com token expirado (RNF-009), alterado ou terminado (RF-016, RF-017). |

## 6. Resumo dos endpoints

| Operação | Método e caminho | Autorização | Requisitos |
|---|---|---|---|
| OP-01 Registar conta | `POST /auth/registo` | Pública | RF-010, RF-011, RF-044, RNF-001, RNF-003 |
| OP-02 Iniciar sessão | `POST /auth/login` | Pública | RF-012, RF-017, RNF-009, RNF-054 |
| OP-03 Terminar sessão | `POST /auth/logout` | Qualquer Utilizador autenticado | RF-016, RF-017 |
| OP-04 Consultar dados da conta | `GET /conta` | Qualquer Utilizador autenticado | RF-013 |
| OP-05 Atualizar dados da conta | `PUT /conta` | Qualquer Utilizador autenticado | RF-011, RF-014 |
| OP-06 Alterar palavra-passe | `PUT /conta/palavra-passe` | Qualquer Utilizador autenticado | RF-015, RNF-001, RNF-003 |
| OP-07 Listar fichas do Utilizador | `GET /fichas` | Qualquer Utilizador autenticado | RF-028, RF-120, RF-121, RNF-005 |
| OP-08 Obter as listas de necessidades | `GET /dominios/necessidades` | Qualquer Utilizador autenticado | RF-021 |
| OP-09 Criar ficha (de terceiro ou própria) | `POST /fichas` | Qualquer Utilizador autenticado | RF-020, RF-021, RF-022, RF-023, RF-024, RF-025, RF-030, RF-031, RF-041, RNF-006, RNF-008, RNF-014, RNF-046 |
| OP-10 Consultar ficha | `GET /fichas/{fichaId}` | Cuidador principal, Cuidador, Familiar | RF-028, RNF-005 |
| OP-11 Editar ficha | `PUT /fichas/{fichaId}` | Cuidador principal, Cuidador | RF-021, RF-022, RF-023, RF-029, RNF-008, RNF-046 |
| OP-12 Arquivar ficha | `POST /fichas/{fichaId}/arquivo` | Cuidador principal, Cuidador | RF-120, RF-122, RNF-049 |
| OP-13 Reativar ficha | `DELETE /fichas/{fichaId}/arquivo` | Cuidador principal, Cuidador | RF-121, RF-122 |
| OP-14 Eliminar ficha | `DELETE /fichas/{fichaId}` | Cuidador principal, Cuidador | RF-123, RNF-046 |
| OP-15 Carregar documento na ficha | `POST /fichas/{fichaId}/documentos` | Cuidador principal, Cuidador | RF-022, RF-023, RNF-006, RNF-008, RNF-014, RNF-046 |
| OP-16 Listar documentos da ficha | `GET /fichas/{fichaId}/documentos` | Cuidador principal, Cuidador, Familiar | RF-022, RF-028, RNF-007 |
| OP-17 Descarregar documento | `GET /documentos/{documentoId}` | Cuidador principal, Cuidador, Familiar | RF-028, RF-124, RNF-007 |
| OP-18 Listar pessoas associadas à ficha | `GET /fichas/{fichaId}/pessoas` | Cuidador principal, Cuidador, Familiar | RF-025, RF-047, RF-048, RF-132 |
| OP-19 Convidar pessoa para a ficha | `POST /fichas/{fichaId}/convites` | Cuidador principal, Cuidador | RF-025, RF-027, RF-032, RF-040, RF-041, RF-043, RF-135, RF-136, RNF-018, RNF-052 |
| OP-20 Listar convites recebidos | `GET /convites` | Qualquer Utilizador autenticado | RF-042, RF-044, RF-135 |
| OP-21 Aceitar convite | `POST /convites/{conviteId}/aceitacao` | Convite pendente | RF-026, RF-045 |
| OP-22 Recusar convite | `POST /convites/{conviteId}/recusa` | Convite pendente | RF-027, RF-046 |
| OP-23 Promover Familiar a Cuidador | `PUT /fichas/{fichaId}/pessoas/{utilizadorId}/papel` | Cuidador principal, Cuidador | RF-047, RF-049 |
| OP-24 Despromover Cuidador a Familiar | `PUT /fichas/{fichaId}/pessoas/{utilizadorId}/papel` | Cuidador principal, Cuidador | RF-033, RF-048, RF-130, RF-133, RF-134 |
| OP-25 Remover acesso de uma pessoa | `DELETE /fichas/{fichaId}/pessoas/{utilizadorId}` | Cuidador principal, Cuidador | RF-033, RF-131, RF-133, RF-134 |
| OP-26 Transferir o papel de Cuidador principal | `POST /fichas/{fichaId}/cuidador-principal` | Cuidador principal, Cuidador | RF-034, RF-132, RF-133, RNF-046 |
| OP-27 Registar tratamento | `POST /fichas/{fichaId}/tratamentos` | Cuidador principal, Cuidador | RF-050, RF-051, RF-052, RF-053, RF-060, RNF-006, RNF-008, RNF-014, RNF-046 |
| OP-28 Listar tratamentos da ficha | `GET /fichas/{fichaId}/tratamentos` | Cuidador principal, Cuidador, Familiar | RF-054, RF-057 |
| OP-29 Consultar tratamento | `GET /tratamentos/{tratamentoId}` | Cuidador principal, Cuidador, Familiar | RF-054, RF-057 |
| OP-30 Atualizar tratamento | `PUT /tratamentos/{tratamentoId}` | Cuidador principal, Cuidador | RF-051, RF-052, RF-053, RF-055, RF-060, RNF-008, RNF-046 |
| OP-31 Terminar tratamento antecipadamente | `POST /tratamentos/{tratamentoId}/termino` | Cuidador principal, Cuidador | RF-056, RF-057, RF-060, RNF-049 |
| OP-32 Listar tomas da ficha | `GET /fichas/{fichaId}/tomas` | Cuidador principal, Cuidador, Familiar | RF-057, RF-063, RF-065, RF-066, RF-067, RF-068 |
| OP-33 Confirmar toma | `POST /tomas/{tomaId}/confirmacao` | Cuidador principal, Cuidador | RF-064, RF-065, RF-067, RF-068, RF-102, RF-105, RNF-011, RNF-044 |
| OP-34 Sincronizar o calendário de tomas | `GET /calendario/tomas` | Cuidador principal, Cuidador | RF-061, RF-062, RF-069 |
| OP-41 Registar compromisso | `POST /fichas/{fichaId}/compromissos` | Cuidador principal, Cuidador | RF-080, RF-116, RF-117 |
| OP-42 Listar compromissos da ficha | `GET /fichas/{fichaId}/compromissos` | Cuidador principal, Cuidador, Familiar | RF-080, RF-116 |
| OP-43 Editar compromisso | `PUT /compromissos/{compromissoId}` | Cuidador principal, Cuidador | RF-081, RF-116, RF-117 |
| OP-44 Remover compromisso | `DELETE /compromissos/{compromissoId}` | Cuidador principal, Cuidador | RF-082, RF-117 |
| OP-45 Consultar agenda | `GET /agenda` | Qualquer Utilizador autenticado | RF-083, RF-084, RF-085, RF-086, RF-087, RF-088, RF-089, RNF-012, RNF-047 |
| OP-46 Registar ocorrência | `POST /fichas/{fichaId}/ocorrencias` | Cuidador principal, Cuidador | RF-090, RF-092, RF-093, RF-094, RF-115, RNF-049 |
| OP-47 Registar nota partilhada | `POST /fichas/{fichaId}/notas` | Cuidador principal, Cuidador | RF-091, RF-092, RF-093, RF-095 |
| OP-48 Consultar ocorrências | `GET /fichas/{fichaId}/registos?tipo=Ocorrencia` | Cuidador principal, Cuidador, Familiar | RF-092, RF-093, RF-094 |
| OP-49 Consultar notas partilhadas | `GET /fichas/{fichaId}/registos?tipo=Nota` | Cuidador principal, Cuidador | RF-091, RF-092, RF-093, RF-095 |
| OP-50 Consultar lista de tarefas diária | `GET /lista-diaria` | Cuidador principal, Cuidador | RF-087, RF-100, RF-101, RF-102, RF-104, RNF-010, RNF-047 |
| OP-51 Consultar histórico de notificações | `GET /notificacoes` | Qualquer Utilizador autenticado | RF-041, RF-110, RF-111, RF-112, RF-113, RF-115, RF-117, RF-118, RF-119, RF-140, RF-141 |

Os caminhos são relativos a `/api/v1`. Fora desta tabela fica o endpoint técnico `GET /saude` (secção 3.5), sem autenticação e sem identificador OP. As rotas foram confirmadas no código confrontado: as 45 operações existem com o método e o caminho indicados.

## 7. Cobertura dos requisitos funcionais

Os requisitos RF-070 a RF-078, RF-103 e RF-114 estão descontinuados (F007 retirada do âmbito) e deixam de ter linha nesta tabela.

| Requisito | Título | Endpoints | Observação |
|---|---|---|---|
| RF-010 | Criar conta | OP-01 | Endpoint |
| RF-011 | Impedir conta com e-mail já registado | OP-01, OP-05 | Endpoint |
| RF-012 | Iniciar sessão | OP-02 | Endpoint |
| RF-013 | Consultar dados da conta | OP-04 | Endpoint |
| RF-014 | Atualizar nome e e-mail da conta | OP-05 | Endpoint |
| RF-015 | Alterar palavra-passe | OP-06 | Endpoint |
| RF-016 | Terminar sessão | OP-03 | Endpoint |
| RF-017 | Exigir sessão iniciada | OP-02, OP-03 | Transversal: sessão exigida em todos os endpoints, exceto OP-01 e OP-02 (secção 3.1) |
| RF-020 | Criar ficha de terceiro | OP-09 | Endpoint |
| RF-021 | Registar necessidades específicas | OP-08, OP-09, OP-11 | Endpoint |
| RF-022 | Carregar documentos na ficha | OP-09, OP-11, OP-15, OP-16 | Endpoint |
| RF-023 | Recusar a operação da ficha quando o documento não é guardado | OP-09, OP-11, OP-15 | Endpoint |
| RF-024 | Definir o criador como Cuidador principal | OP-09 | Endpoint |
| RF-025 | Convidar outra pessoa para Cuidador principal | OP-09, OP-18, OP-19 | Endpoint |
| RF-026 | Transferir o papel de Cuidador principal quando o convite é aceite | OP-21 | Endpoint |
| RF-027 | Manter o criador como Cuidador principal quando o convite é recusado | OP-19, OP-22 | Endpoint |
| RF-028 | Consultar ficha | OP-07, OP-10, OP-16, OP-17 | Endpoint |
| RF-029 | Editar ficha | OP-11 | Endpoint |
| RF-030 | Criar ficha própria | OP-09 | Endpoint |
| RF-031 | Limitar a uma ficha própria por Utilizador | OP-09 | Endpoint |
| RF-032 | Aplicar à ficha própria as regras das restantes fichas | OP-19 | Transversal: a ficha própria usa as mesmas operações das restantes fichas; convites em OP-19 (secção 3.2) |
| RF-033 | Proteger o titular da ficha própria | OP-24, OP-25 | Endpoint |
| RF-034 | Manter o titular como Cuidador principal da ficha própria | OP-26 | Endpoint |
| RF-040 | Convidar pessoa para uma ficha | OP-19 | Endpoint |
| RF-041 | Enviar notificação e e-mail de convite | OP-09, OP-19, OP-51 | Endpoint e servidor (secção 4) |
| RF-042 | Disponibilizar convites na área de convites | OP-20 | Endpoint |
| RF-043 | Impedir convite pendente duplicado | OP-19 | Endpoint |
| RF-044 | Apresentar convites pendentes após o registo | OP-01, OP-20 | Endpoint |
| RF-045 | Aceitar convite | OP-21 | Endpoint |
| RF-046 | Recusar convite | OP-22 | Endpoint |
| RF-047 | Conceder ao Cuidador acesso completo à ficha | OP-18, OP-23 | Transversal: permissões de escrita do Cuidador em todos os endpoints de escrita (secção 3.2) |
| RF-048 | Restringir o Familiar a consulta | OP-18, OP-24 | Transversal: o Familiar só consulta (secção 3.2) |
| RF-049 | Promover Familiar a Cuidador | OP-23 | Endpoint |
| RF-050 | Registar medicação | OP-27 | Endpoint |
| RF-051 | Atribuir grau de importância à medicação | OP-27, OP-30 | Endpoint |
| RF-052 | Carregar receita no tratamento | OP-27, OP-30 | Endpoint |
| RF-053 | Recusar a operação do tratamento quando a receita não é guardada | OP-27, OP-30 | Endpoint |
| RF-054 | Consultar medicação da ficha | OP-28, OP-29 | Endpoint |
| RF-055 | Atualizar tratamento em curso | OP-30 | Endpoint |
| RF-056 | Terminar tratamento antecipadamente | OP-31 | Endpoint |
| RF-057 | Preservar histórico de tratamentos terminados | OP-28, OP-29, OP-31, OP-32 | Endpoint |
| RF-060 | Gerar as tomas previstas de uma medicação | OP-27, OP-30, OP-31 | Endpoint e servidor (secção 4) |
| RF-061 | Sincronizar o calendário de tomas com a aplicação móvel | OP-34 | Endpoint |
| RF-062 | Emitir o aviso local da toma prevista | OP-34 | Aplicação mobile: aviso local agendado a partir dos dados de OP-34 (secção 4) |
| RF-063 | Apresentar o calendário de tomas de uma ficha | OP-32 | Endpoint |
| RF-064 | Confirmar uma toma | OP-33 | Endpoint: janela e estado resultante em OP-33 |
| RF-065 | Registar o autor e o momento da confirmação de uma toma | OP-32, OP-33 | Endpoint |
| RF-066 | Atribuir o estado "pendente" às tomas por confirmar | OP-32 | Endpoint e servidor (secção 4) |
| RF-067 | Marcar como atrasada a toma confirmada fora da tolerância | OP-32, OP-33 | Endpoint |
| RF-068 | Marcar como não confirmada a toma não confirmada em 24 horas | OP-32, OP-33 | Endpoint e servidor (secção 4) |
| RF-069 | Suspender a geração de tomas numa ficha arquivada | OP-34 | Servidor (secção 4); suspensão e retoma em OP-12 e OP-13; OP-34 não inclui fichas arquivadas |
| RF-080 | Registar um compromisso de saúde | OP-41, OP-42 | Endpoint |
| RF-081 | Editar um compromisso de saúde | OP-43 | Endpoint |
| RF-082 | Remover um compromisso de saúde | OP-44 | Endpoint |
| RF-083 | Reunir na agenda os eventos de todas as fichas do Utilizador | OP-45 | Endpoint |
| RF-084 | Apresentar a agenda em vista diária | OP-45 | Endpoint: vista `dia` de OP-45 |
| RF-085 | Apresentar a agenda em vista semanal | OP-45 | Endpoint: vista `semana` de OP-45 |
| RF-086 | Apresentar a agenda em vista mensal | OP-45 | Endpoint: vista `mes` de OP-45 |
| RF-087 | Impedir a confirmação de compromissos | OP-45, OP-50 | Endpoint e servidor (secção 4): recusa de confirmação de compromissos |
| RF-088 | Impedir a alteração de tomas a partir da agenda | OP-45 | Interface: a agenda (OP-45) é só de leitura; as tomas alteram-se em OP-30 |
| RF-089 | Identificar a ficha de cada evento da agenda | OP-45 | Endpoint: campo `nomeFicha` de OP-45 |
| RF-090 | Registar uma ocorrência | OP-46 | Endpoint; alteração e eliminação recusadas (secção 4) |
| RF-091 | Registar uma nota dirigida aos Cuidadores | OP-47, OP-49 | Endpoint; alteração e eliminação recusadas (secção 4) |
| RF-092 | Registar o autor e o momento de ocorrências e notas | OP-46, OP-47, OP-48, OP-49 | Endpoint |
| RF-093 | Disponibilizar ocorrências e notas aos Cuidadores da ficha | OP-46, OP-47, OP-48, OP-49 | Endpoint |
| RF-094 | Consultar o histórico de ocorrências de uma ficha | OP-46, OP-48 | Endpoint |
| RF-095 | Consultar as notas de uma ficha | OP-47, OP-49 | Endpoint |
| RF-100 | Apresentar a lista diária consolidada | OP-50 | Endpoint |
| RF-101 | Identificar a ficha de cada item da lista diária | OP-50 | Endpoint: campo `nomeFicha` de OP-50 |
| RF-102 | Confirmar uma toma a partir da lista diária | OP-33, OP-50 | Endpoint: OP-33, usado a partir da lista diária (OP-50) |
| RF-104 | Refletir as confirmações nas listas dos restantes Cuidadores | OP-50 | Endpoint: a lista (OP-50) mostra a confirmação de outro Cuidador quando é aberta ou atualizada; sem push |
| RF-105 | Impedir a confirmação repetida de um item | OP-33 | Endpoint: recusa com 409 em OP-33 |
| RF-110 | Notificar Cuidadores de toma Leve não confirmada | OP-51 | Servidor (secção 4); consulta em OP-51 |
| RF-111 | Notificar Cuidadores de toma Moderado não confirmada | OP-51 | Servidor (secção 4); consulta em OP-51 |
| RF-112 | Notificar Cuidadores de toma Grave não confirmada | OP-51 | Servidor (secção 4); consulta em OP-51 |
| RF-113 | Notificar Familiares de toma Grave com atraso prolongado | OP-51 | Servidor (secção 4); consulta em OP-51 |
| RF-115 | Notificar o registo de uma nova ocorrência | OP-46, OP-51 | Endpoint e servidor (secção 4) |
| RF-116 | Definir a antecedência de aviso de um compromisso | OP-41, OP-42, OP-43 | Endpoint: antecedência de aviso em OP-41 e OP-43; notificação em secção 4 |
| RF-117 | Notificar antes de um compromisso | OP-41, OP-43, OP-44, OP-51 | Endpoint e servidor (secção 4). Por cumprir no código confrontado: a notificação não é criada (secção 8) |
| RF-118 | Reenviar o e-mail de notificação após falha | OP-51 | Endpoint e servidor (secção 4) |
| RF-119 | Identificar no histórico o e-mail não entregue | OP-51 | Endpoint e servidor (secção 4) |
| RF-120 | Arquivar ficha | OP-07, OP-12 | Endpoint |
| RF-121 | Reativar ficha | OP-07, OP-13 | Endpoint |
| RF-122 | Suspender tarefas, avisos e notificações de fichas arquivadas | OP-12, OP-13 | Endpoint e servidor (secção 4) |
| RF-123 | Eliminar ficha sem registos | OP-14 | Endpoint |
| RF-124 | Informar falha ao consultar documento | OP-17 | Endpoint |
| RF-130 | Despromover Cuidador a Familiar | OP-24 | Endpoint |
| RF-131 | Remover acesso à ficha | OP-25 | Endpoint |
| RF-132 | Transferir o papel de Cuidador principal | OP-18, OP-26 | Endpoint |
| RF-133 | Exigir substituto antes de despromover ou remover o Cuidador principal | OP-24, OP-25, OP-26 | Endpoint |
| RF-134 | Garantir que a ficha tem sempre um Cuidador | OP-24, OP-25 | Endpoint |
| RF-135 | Manter o convite quando o e-mail não é entregue | OP-19, OP-20 | Endpoint e servidor (secção 4) |
| RF-136 | Reenviar e-mail de convite em caso de falha | OP-19 | Endpoint e servidor (secção 4) |
| RF-140 | Consultar o histórico de notificações recebidas (F011) | OP-51 | Endpoint |
| RF-141 | Criar a notificação na aplicação independentemente do e-mail (F011) | OP-51 | Endpoint e servidor (secção 4) |
| RF-142 | Suspender notificações de ficha arquivada (F011) | - | Servidor (secção 4); suspensão e retoma em OP-12 e OP-13 |

Os requisitos não funcionais com efeito no contrato estão refletidos como segue.

| Requisito | Título | Onde está refletido |
|---|---|---|
| RNF-001 | Proteção das palavras-passe | Secção 3.1; OP-01, OP-06 |
| RNF-002 | Comunicação cifrada | Secção 3, endereço base |
| RNF-003 | Requisitos mínimos da palavra-passe | OP-01, OP-06 |
| RNF-004 | Autorização verificada no servidor | Secção 3.2; respostas 403 de todos os endpoints de escrita |
| RNF-005 | Isolamento dos dados das fichas | Secção 3.2; respostas 404 |
| RNF-006 | Armazenamento local dos documentos | OP-09, OP-15, OP-27 |
| RNF-007 | Acesso restrito aos documentos | Secção 3.2; OP-16, OP-17 |
| RNF-008 | Formatos e tamanho dos documentos | OP-09, OP-11, OP-15, OP-27, OP-30; respostas 413 e 415 |
| RNF-009 | Validade da sessão | Secção 3.1; OP-02 |
| RNF-010 | Tempo de carregamento da lista diária | Secção 3.4; OP-50 |
| RNF-011 | Tempo de resposta da confirmação de tomas | Secção 3.4; OP-33 |
| RNF-012 | Tempo de carregamento da agenda | Secção 3.4; OP-45 |
| RNF-013 | Tempo de autenticação | Secção 3.4; OP-02 |
| RNF-014 | Tempo de carregamento de documentos | Secção 3.4; OP-09, OP-15, OP-27 |
| RNF-015 | Capacidade de utilizadores simultâneos | Secção 3.4 |
| RNF-017 | Tempo de sincronização do calendário de tomas | Secção 3.4; OP-34 |
| RNF-018 | Tempo até ao envio do e-mail de notificação | Secção 4; OP-19 |
| RNF-020 | Língua da interface | Secção 3 |
| RNF-028 | Mensagens de erro de validação | Secção 3.3; respostas 400 |
| RNF-029 | Formatos de data e hora | Secção 3 |
| RNF-031 | Tempo de resposta após inatividade | Secção 3.4 |
| RNF-040 | Precisão da deteção de atrasos e antecedências | Secção 4 |
| RNF-041 | Precisão do aviso local da toma | Secção 4; dados de OP-34 |
| RNF-042 | Recuperação de eventos após reinício do servidor | Secção 4 |
| RNF-043 | Ausência de notificações duplicadas | Secção 4 |
| RNF-044 | Ausência de confirmações duplicadas em concorrência | OP-33 |
| RNF-045 | Cópias de segurança | Secção 4 |
| RNF-046 | Atomicidade das operações de escrita | Secção 3; OP-09, OP-11, OP-14, OP-15, OP-26, OP-27, OP-30 |
| RNF-047 | Correção dos cálculos temporais em casos-limite | Secção 3; OP-45, OP-50 |
| RNF-049 | Integridade do histórico | OP-12, OP-31, OP-46; secção 4 |
| RNF-052 | Envio de e-mail por endereço dedicado | Secção 4; OP-19 |
| RNF-053 | Tecnologia do backend | Secção 3 |
| RNF-054 | Autenticação por token | Secção 3.1; OP-02 |

Os restantes requisitos não funcionais (por exemplo a usabilidade da interface, a disponibilidade da instância de demonstração, o ambiente de dados fictícios e as tecnologias dos frontends e da base de dados) dizem respeito aos clientes ou à infraestrutura e não alteram o contrato da API.

## 8. Limitações

- A API não tem paginação nem ordenação configurável nesta versão; as listas têm a dimensão do ambiente de demonstração.
- Não existem notificações push nem canais em tempo real: os clientes obtêm o estado atualizado quando abrem ou atualizam o ecrã, incluindo as ocorrências e notas de outros Cuidadores, a lista diária e o histórico de notificações (RF-093, RF-104, RF-140). Uma alteração à medicação só chega à aplicação mobile na sincronização seguinte (RF-061).
- O token JWT dura 8 horas e a API não o renova; depois de expirar, o Utilizador inicia sessão de novo (RNF-009).
- Os avisos locais de toma são agendados e emitidos pela aplicação mobile, em Android, a partir das tomas sincronizadas; a API só fornece os dados (RF-061, RF-062). Sem abrir a aplicação durante mais de 48 horas, os avisos já sincronizados esgotam-se até à abertura seguinte.
- As ocorrências e as notas não se alteram nem se eliminam, e os compromissos não se confirmam: a API não tem operações para isso e recusa estes pedidos com `405` (RF-087, RF-090, RF-091).
- As tomas só existem na janela móvel de 30 dias (tratamentos sem data de fim); a agenda (OP-45) só mostra compromissos nos períodos mais distantes (RF-083).
- O versionamento é feito no caminho (`/api/v1`); uma alteração incompatível exige uma nova versão.
- O contrato de erros usa códigos estáveis; as mensagens em texto podem ser ajustadas sem alterar o contrato.
- **Notificação de compromisso por implementar (RF-117).** No código confrontado (`main`, commit `7bf212c`) a antecedência de aviso é validada e guardada, mas nenhum passo do processo periódico cria as notificações `ProximoCompromisso`: a Issue I044 remeteu-as para a I047, que tratou só as notificações de atraso das tomas. O contrato das secções 4, 5.7 e 5.10 mantém-se, porque corresponde a um requisito em vigor; a correção é do código e precisa de uma Issue própria.

## 9. Tabela de cobertura do conteúdo mínimo

| Exigência do Regulamento de Funcionamento da UC (secção 19) | Secção deste documento |
|---|---|
| Endpoints e operações | Secções 5 e 6 |
| Autenticação | Secção 3.1 e campo «Autenticação e autorização» de cada endpoint |
| Parâmetros | Campos «Parâmetros e notas» e exemplos de pedido de cada endpoint |
| Formatos | Secção 3 |
| Respostas | «Resposta de sucesso» de cada endpoint |
| Erros | Secção 3.3 e tabela «Erros» de cada endpoint |
| Exemplos | Exemplos de pedido e de resposta de cada endpoint |

## 10. Alterações face às versões anteriores

### 10.0 Da v01 (m2) para a v02

A v02 (`m3-documentacao-api-v02.md`) só retirou a F007 do âmbito, pela alteração autorizada de âmbito de 2026-10-19 homologada pelo docente: saíram as operações OP-35 a OP-40, o código de erro `EXECUCAO_JA_CONFIRMADA`, a notificação `TarefaNaoConfirmada`, o tipo de evento `Tarefa` e as referências às tarefas nas regras de arquivo, eliminação, agenda, lista diária e notificações; as referências passaram a indicar 96 requisitos funcionais ativos, a Declaração de Âmbito v05 e as operações OP-01 a OP-34 e OP-41 a OP-51. Nenhum dos restantes endpoints mudou de caminho, de método ou de contrato. As alterações da v02 para a v03 estão nas secções 10.1 a 10.4.

### 10.1 Divergências corrigidas (v02 para v03)

As divergências D04 a D10 da Issue I054 (#68) foram classificadas como erro da documentação e atribuídas a esta atualização (Issue I042). Cada uma foi confirmada de novo no código da `main` (commit `7bf212c`) antes de ser corrigida.

| Divergência | Código | Correção nesta versão | Secção |
|---|---|---|---|
| D04 | `ArranqueApi.cs`, `VerificacaoBaseDados.cs` | Documentado o endpoint público `GET /saude`, com a verificação `baseDados`, os códigos `200` e `503` e o corpo `{estado, verificacoes}` | 1, 3.5, 6 |
| D05 | `DocumentoService.ObterAsync` | Retirada de OP-17 a linha `RECURSO_NAO_ENCONTRADO`: documento inexistente e documento de ficha sem associação dão ambos `DOCUMENTO_NAO_ENCONTRADO` | OP-17 |
| D06 | `ConviteService.AceitarAsync`, `Ficha.ExigirQueNaoEstaAssociado` | Acrescentado a OP-21 o erro `409` `PESSOA_JA_ASSOCIADA` | OP-21 |
| D07 | `Ficha.ExigirAssociacao` (via `AlterarPapel` e `RemoverAcesso`) | Acrescentado a OP-24 e OP-25 o erro `404` `PESSOA_NAO_ENCONTRADA`, que já constava de OP-23 | OP-24, OP-25 |
| D08 | `Ficha.MarcarPrincipalProvisorio` | `CONVITE_PENDENTE_EXISTE` em OP-19 também quando já existe um convite para Cuidador principal pendente na ficha, para qualquer e-mail | OP-19 |
| D09 | `FichasController` (`ParteDados`, `ParteDocumento`, `ParteCategoria`), `TratamentosController` (`ParteReceita`), `DocumentosController`, `LeitorMultipart` | Descritas as partes de cada pedido `multipart/form-data`: `dados`, `documento` e `categoria` emparelhadas pela ordem, `categoria` e `ficheiro` em OP-15, `dados` e `receita` em OP-27 e OP-30, e os nomes dos campos nos erros; retirado do exemplo de OP-09 o campo fictício `documentos` | 3, 3.6, OP-09, OP-11, OP-15 |
| D10 | `FiltroDeErros.Converter` | Acrescentado à tabela 3.3 o código `500` `ERRO_INTERNO`, com o corpo; documentados também o corpo ilegível (`400` com o campo `corpo`) e a ordem das verificações | 3.3 |

### 10.2 Decisões do Sprint 05 integradas

| Origem | Decisão registada no Pull Request e confirmada no código | Secção |
|---|---|---|
| I038 (#61) | OP-32: `de` e `ate` obrigatórios, validados antes da autorização, com o erro no campo em falta; intervalo de 31 dias contando os dois extremos | OP-32, 3.3 |
| I038 (#61) | OP-33: a confirmação concorrente é detetada pela versão da toma e o segundo pedido recebe `TOMA_JA_CONFIRMADA` | OP-33 |
| I044 (#74) | F008: verificação da ficha e do papel antes da validação; `local` até 150 e `motivo` até 250 caracteres; `dataHora` obrigatória, sem exigir data futura, guardada ao segundo; antecedência por omissão de 1440 minutos, também em OP-43 | OP-41, OP-43, 3.3 |
| I044 (#74) | OP-42: `de` e `ate` em dias de Lisboa, `ate` inclusive, `ate` anterior a `de` dá `400`; ordenação por `dataHora`; disponível em fichas arquivadas | OP-42 |
| I044 (#74) | OP-45: `vista` sem distinção de maiúsculas, `titulo` «Medicamento Dose», regra de `confirmavel`, ordenação e lista vazia sem fichas | OP-45 |
| I045 (#67) | F009: verificação da ficha e do papel antes da validação; `descricao` até 2000 caracteres; `ocorridaEm` pode ser futura; `de` e `ate` em dias de Lisboa aplicados a `criadaEm`; nome do autor atual; mensagem da notificação sem a descrição | OP-46 a OP-49, 4 |
| I046 (#73) | OP-50: lista vazia sem fichas; `confirmavel` só com toma `Pendente`, janela aberta e antes das 24 horas; `data` só `AAAA-MM-DD`; tomas pendentes do dia anterior só quando a data é hoje; campos de confirmação só nas tomas confirmadas; ordenação | OP-50 |
| I047 (#75) | Notificações de atraso: a toma continua `Pendente` com a marca de notificação; Familiares a T mais 60 minutos só no grau Grave; ficha sem Familiares só marcada; mensagem com o medicamento sem a dose; e-mail `Pendente` enquanto não é enviado; `de` e `ate` opcionais em OP-51; notificações em falta antes da passagem a `NaoConfirmada` | 4, OP-51 |

### 10.3 Divergências da I054 tratadas noutros artefactos

| Divergências | Tratamento |
|---|---|
| D01, D02 | Erros do código, corrigidos no Pull Request #64 (passos 4 e 7 do processo periódico) |
| D03, D11 a D23, C1, C2 | Modelo de classes do backend, `m3-modelo-classes-backend-v03.md` (Issue I055) |
| D24, D25 | Modelo de dados, `m3-modelo-de-dados-dosecerta-v03.md` (Issue I055) |

### 10.4 Lacuna do código identificada nesta revisão

A confrontação mostrou que as notificações `ProximoCompromisso` (RF-117) não são criadas pelo código da `main`: `Compromisso.AvisoDevido` e `Compromisso.MarcarAvisoEnviado` existem, mas nenhum serviço nem passo do processo periódico os usa. Não é um erro desta documentação, que mantém o contrato e assinala o estado nas secções 4, 5.7, 7 e 8. A correção é do código e deve ser tratada numa Issue própria.

### 10.5 Correspondência entre os endpoints implementados e as secções

Cada endpoint implementado no código confrontado tem a sua secção neste documento, e cada secção corresponde a uma ação implementada.

| Operação | Método e caminho | Ação no código | Secção |
|---|---|---|---|
| OP-01 Registar conta | `POST /api/v1/auth/registo` | `AuthController.RegistarConta` | 5.1 |
| OP-02 Iniciar sessão | `POST /api/v1/auth/login` | `AuthController.IniciarSessao` | 5.1 |
| OP-03 Terminar sessão | `POST /api/v1/auth/logout` | `AuthController.TerminarSessao` | 5.1 |
| OP-04 Consultar dados da conta | `GET /api/v1/conta` | `ContaController.ConsultarDadosDaConta` | 5.1 |
| OP-05 Atualizar dados da conta | `PUT /api/v1/conta` | `ContaController.AtualizarDadosDaConta` | 5.1 |
| OP-06 Alterar palavra-passe | `PUT /api/v1/conta/palavra-passe` | `ContaController.AlterarPalavraPasse` | 5.1 |
| OP-07 Listar fichas do Utilizador | `GET /api/v1/fichas` | `FichasController.ListarFichasDoUtilizador` | 5.2 |
| OP-08 Obter as listas de necessidades | `GET /api/v1/dominios/necessidades` | `DominiosController.ObterAsListasDeNecessidades` | 5.2 |
| OP-09 Criar ficha (de terceiro ou própria) | `POST /api/v1/fichas` | `FichasController.CriarFicha`, `FichasController.CriarFichaComDocumentos` | 5.2 |
| OP-10 Consultar ficha | `GET /api/v1/fichas/{fichaId}` | `FichasController.ConsultarFicha` | 5.2 |
| OP-11 Editar ficha | `PUT /api/v1/fichas/{fichaId}` | `FichasController.EditarFicha`, `FichasController.EditarFichaComDocumentos` | 5.2 |
| OP-12 Arquivar ficha | `POST /api/v1/fichas/{fichaId}/arquivo` | `FichasController.ArquivarFicha` | 5.2 |
| OP-13 Reativar ficha | `DELETE /api/v1/fichas/{fichaId}/arquivo` | `FichasController.ReativarFicha` | 5.2 |
| OP-14 Eliminar ficha | `DELETE /api/v1/fichas/{fichaId}` | `FichasController.EliminarFicha` | 5.2 |
| OP-15 Carregar documento na ficha | `POST /api/v1/fichas/{fichaId}/documentos` | `DocumentosController.CarregarDocumentoNaFicha` | 5.2 |
| OP-16 Listar documentos da ficha | `GET /api/v1/fichas/{fichaId}/documentos` | `DocumentosController.ListarDocumentosDaFicha` | 5.2 |
| OP-17 Descarregar documento | `GET /api/v1/documentos/{documentoId}` | `DocumentosController.DescarregarDocumento` | 5.2 |
| OP-18 Listar pessoas associadas à ficha | `GET /api/v1/fichas/{fichaId}/pessoas` | `PessoasController.ListarPessoasAssociadasAFicha` | 5.3 |
| OP-19 Convidar pessoa para a ficha | `POST /api/v1/fichas/{fichaId}/convites` | `ConvitesController.ConvidarPessoaParaAFicha` | 5.3 |
| OP-20 Listar convites recebidos | `GET /api/v1/convites` | `ConvitesController.ListarConvitesRecebidos` | 5.3 |
| OP-21 Aceitar convite | `POST /api/v1/convites/{conviteId}/aceitacao` | `ConvitesController.AceitarConvite` | 5.3 |
| OP-22 Recusar convite | `POST /api/v1/convites/{conviteId}/recusa` | `ConvitesController.RecusarConvite` | 5.3 |
| OP-23 Promover Familiar a Cuidador | `PUT /api/v1/fichas/{fichaId}/pessoas/{utilizadorId}/papel` | `PessoasController.AlterarPapelDeUmaPessoa` | 5.3 |
| OP-24 Despromover Cuidador a Familiar | `PUT /api/v1/fichas/{fichaId}/pessoas/{utilizadorId}/papel` | `PessoasController.AlterarPapelDeUmaPessoa` | 5.3 |
| OP-25 Remover acesso de uma pessoa | `DELETE /api/v1/fichas/{fichaId}/pessoas/{utilizadorId}` | `PessoasController.RemoverAcessoDeUmaPessoa` | 5.3 |
| OP-26 Transferir o papel de Cuidador principal | `POST /api/v1/fichas/{fichaId}/cuidador-principal` | `PessoasController.TransferirOPapelDeCuidadorPrincipal` | 5.3 |
| OP-27 Registar tratamento | `POST /api/v1/fichas/{fichaId}/tratamentos` | `TratamentosController.RegistarTratamento`, `TratamentosController.RegistarTratamentoComReceita` | 5.4 |
| OP-28 Listar tratamentos da ficha | `GET /api/v1/fichas/{fichaId}/tratamentos` | `TratamentosController.ListarTratamentosDaFicha` | 5.4 |
| OP-29 Consultar tratamento | `GET /api/v1/tratamentos/{tratamentoId}` | `TratamentosController.ConsultarTratamento` | 5.4 |
| OP-30 Atualizar tratamento | `PUT /api/v1/tratamentos/{tratamentoId}` | `TratamentosController.AtualizarTratamento`, `TratamentosController.AtualizarTratamentoComReceita` | 5.4 |
| OP-31 Terminar tratamento antecipadamente | `POST /api/v1/tratamentos/{tratamentoId}/termino` | `TratamentosController.TerminarTratamentoAntecipadamente` | 5.4 |
| OP-32 Listar tomas da ficha | `GET /api/v1/fichas/{fichaId}/tomas` | `TomasController.ListarTomasDaFicha` | 5.5 |
| OP-33 Confirmar toma | `POST /api/v1/tomas/{tomaId}/confirmacao` | `TomasController.ConfirmarToma` | 5.5 |
| OP-34 Sincronizar o calendário de tomas | `GET /api/v1/calendario/tomas` | `TomasController.SincronizarOCalendarioDeTomas` | 5.5 |
| OP-41 Registar compromisso | `POST /api/v1/fichas/{fichaId}/compromissos` | `CompromissosController.RegistarCompromisso` | 5.7 |
| OP-42 Listar compromissos da ficha | `GET /api/v1/fichas/{fichaId}/compromissos` | `CompromissosController.ListarCompromissosDaFicha` | 5.7 |
| OP-43 Editar compromisso | `PUT /api/v1/compromissos/{compromissoId}` | `CompromissosController.EditarCompromisso` | 5.7 |
| OP-44 Remover compromisso | `DELETE /api/v1/compromissos/{compromissoId}` | `CompromissosController.RemoverCompromisso` | 5.7 |
| OP-45 Consultar agenda | `GET /api/v1/agenda` | `AgendaController.ConsultarAgenda` | 5.7 |
| OP-46 Registar ocorrência | `POST /api/v1/fichas/{fichaId}/ocorrencias` | `RegistosController.RegistarOcorrencia` | 5.8 |
| OP-47 Registar nota partilhada | `POST /api/v1/fichas/{fichaId}/notas` | `RegistosController.RegistarNotaPartilhada` | 5.8 |
| OP-48 Consultar ocorrências | `GET /api/v1/fichas/{fichaId}/registos?tipo=Ocorrencia` | `RegistosController.ConsultarRegistos` | 5.8 |
| OP-49 Consultar notas partilhadas | `GET /api/v1/fichas/{fichaId}/registos?tipo=Nota` | `RegistosController.ConsultarRegistos` | 5.8 |
| OP-50 Consultar lista de tarefas diária | `GET /api/v1/lista-diaria` | `ListaDiariaController.ConsultarListaDeTarefasDiaria` | 5.9 |
| OP-51 Consultar histórico de notificações | `GET /api/v1/notificacoes` | `NotificacoesController.ConsultarHistoricoDeNotificacoes` | 5.10 |
| Verificação do estado do servidor | `GET /saude` | `ArranqueApi.ConfigurarPipeline` (`MapHealthChecks`) e `VerificacaoBaseDados` | 3.5 |

### 10.6 Verificação desta versão

| Verificação | Resultado |
|---|---|
| Rotas | As 45 operações têm no código uma ação com o mesmo método e o mesmo caminho. Há 43 rotas distintas, porque OP-23 e OP-24 partilham `PUT /api/v1/fichas/{fichaId}/pessoas/{utilizadorId}/papel` (o corpo indica o papel) e OP-48 e OP-49 partilham `GET /api/v1/fichas/{fichaId}/registos` (o parâmetro `tipo` distingue-as). As 4 rotas que só respondem `405` estão na secção 4. Não há no código rotas da API sem documentação; `GET /saude` está na secção 3.5 |
| Códigos de erro | Os 35 códigos de `CodigosErro` aparecem neste documento e todos têm código HTTP em `FiltroDeErros`; os 178 pares (código HTTP, código de erro) das tabelas coincidem com esse mapeamento e não há no documento códigos que não existam no código. A presença de cada código na tabela da operação certa foi revista à mão em OP-32 a OP-51 e nas operações alteradas nesta versão; em OP-01 a OP-31 mantém-se a v02, confrontada com o código pela I054 (Pull Request #64) |
| Corpos de resposta | As chaves de todos os exemplos JSON coincidem com os campos dos contratos de pedido e de resposta do código (`DoseCerta.Api.Contratos` e `ProblemaApi`). As únicas exceções são intencionais: o exemplo de OP-15 mostra as partes do pedido `multipart/form-data` e o de OP-30 envia só os campos a alterar |
