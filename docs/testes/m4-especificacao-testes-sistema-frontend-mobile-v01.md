# ESPECIFICAÇÃO DOS TESTES DE SISTEMA DO FRONTEND MOBILE

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m4 |
| Versão | v01 |
| Issue | I063 (Sprint 07) |
| Artefacto da Checklist de Controlo de Artefactos | OF-M4-018 Especificação dos testes de sistema do frontend mobile |
| Pasta oficial | `03-artefactos-de-qualidade-de-produto/03.03-frontend-mobile/03.03.02-testes-de-sistema` |
| Plano de referência | `m4-plano-testes-frontend-mobile-v01.md` (I062) |
| Código de referência | Aplicação mobile da I058 (`feature/i058-mobile-base-f001`); pipeline da I061 (`feature/i061-pipeline-frontends`) |

| Versão | Alterações |
|---|---|
| v01 | Primeira versão: cenários de sistema da F001 (RF-010 a RF-017), automatizados com `integration_test` em emulador Android |

## 1. Âmbito

Esta especificação descreve os cenários de sistema da aplicação mobile, segundo o plano de testes (secções 4.1 e 5). A v01 cobre a F001 Autenticação e gestão de conta, nos ecrãs Registo, Início de sessão e Conta. As funcionalidades F002 a F011 entram em versões seguintes. Os cenários são os mesmos do frontend web (`m4-especificacao-testes-sistema-frontend-web-v01.md`), adaptados à aplicação.

## 2. Execução

| Elemento | Definição |
|---|---|
| Ferramenta | `integration_test` (SDK do Flutter) |
| Código dos testes | `frontend-mobile/integration_test/f001_test.dart` |
| Endereço da API | `--dart-define=URL_API=<raiz do servidor>`, o nome que a aplicação lê em `ConfiguracaoApi`; na frontend-mobile-ci, `http://10.0.2.2:<porta>` (só aceite em builds debug no emulador) |
| Emulador | Android API 34, x86_64, `google_apis`, animações desativadas (frontend-mobile-ci, QG-07) |
| Backend | API real (`DoseCerta.Api`) e SQL Server 2022 criados e destruídos em cada execução da frontend-mobile-ci |
| Localização dos elementos | Rótulo do campo (`InputDecoration.labelText`), texto e tipo do botão, dica da entrada da barra inferior; nunca a posição |
| Esperas | Espera ativa pelo elemento (`esperarPor`, no máximo 30 segundos); sem esperas fixas |
| Independência | Cada cenário apaga a sessão guardada (`FlutterSecureStorage.deleteAll`) e arranca a aplicação de novo |
| Comando local | `flutter test integration_test --dart-define=URL_API=http://10.0.2.2:5180`, com emulador e API ativos |
| Quality gate | QG-07: 100% dos cenários passam. A falha de um cenário termina a pipeline sem sucesso |

**Desvio ao plano (secção 5.3 do plano mobile):** o plano prevê, na pipeline, um cliente HTTP falso. A frontend-mobile-ci da I061 já arranca a API real e o emulador chega-lhe por `10.0.2.2`, que a aplicação aceita em debug. Por isso estes cenários correm contra o backend real, o que já cumpre o «subconjunto contra o backend» do plano para a F001. O cliente falso fica para os cenários de falhas difíceis de provocar (falta de ligação, erro 500), em versões seguintes.

## 3. Dados de teste

- Só dados fictícios: nomes «Ana Fictícia», «Bruno Fictício» e «Carla Fictícia»; e-mails `<prefixo>.<microssegundos>@exemplo.pt`, únicos por execução.
- As contas das precondições são criadas pela API (`POST /api/v1/auth/registo`) no início de cada cenário.
- Palavras-passe fictícias que cumprem RNF-003 (`Teste2026a1`, `Nova2026b2`), válidas só na base de dados efémera da pipeline.

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

## 5. Rastreabilidade

| Requisito | Cenários |
|---|---|
| RF-010 | TS-M-F001-01 |
| RF-011 | TS-M-F001-02 |
| RF-012 | TS-M-F001-03, TS-M-F001-04 |
| RF-013 | TS-M-F001-05 |
| RF-014 | TS-M-F001-05 |
| RF-015 | TS-M-F001-06 |
| RF-016 | TS-M-F001-07 |
| RF-017 | TS-M-F001-07 |

Os oito requisitos funcionais da F001 (RF-010 a RF-017) têm pelo menos um cenário (critério CS-M04 do plano, para a F001).
