**I056 - Implementar o aviso antes de um compromisso (ProximoCompromisso, RF-117)**

Tipo: Defeito · Prioridade: 1 - Crítica · Milestone: m3 · Sprint: S07 · Orçamento: 50 (Executor 30 / Revisor 15 / Auditor 5)
Executor: Ricardo André Santos Ribeiro · Revisor: _(Sprint Planning)_ · Auditor: _(Sprint Planning)_ · Prazo do Executor: _(Sprint Planning)_

**User Stories**
- US-I056-01 - Como Cuidador ou Familiar de uma ficha, quero receber uma notificação na aplicação e por e-mail quando faltar o tempo de antecedência definido para um compromisso, para não faltar à consulta.
- US-I056-02 - Como Cuidador, quero que editar ou remover um compromisso atualize ou cancele o aviso, para não receber avisos errados.

**Descrição técnica**
Implementar a criação das notificações ProximoCompromisso, que hoje não existe no código: Compromisso.InstanteAviso, AvisoDevido e MarcarAvisoEnviado existem, mas nenhum serviço nem passo do processo periódico os usa. A I044 (#74) remeteu este aviso para a I047, que só tratou os atrasos das tomas. A tarefa acrescenta um passo novo ao ProcessoPeriodico; a operação de serviço que cria as notificações para os Cuidadores e Familiares da ficha; o texto em TextosNotificacao; o registo do e-mail pelo fluxo existente do EnvioEmailService; e os testes. Atualiza também a documentação da API e o modelo de classes, retirando a limitação registada nas v03.

**Objetivo**
Cumprir o RF-117 e fechar a lacuna de F011 identificada na I042 e na I055.

**Resultado esperado**
Aviso de compromisso implementado e integrado na main através de Pull Request revisto e auditado, com a pipeline CI concluída com sucesso, e documentação atualizada.

**Critérios de aceitação**
- [ ] O processo periódico tem um passo que, para cada compromisso de ficha ativa cujo instante de aviso já chegou e que ainda não foi avisado, cria uma notificação ProximoCompromisso para cada Cuidador e Familiar da ficha e marca o aviso como enviado na mesma transação.
- [ ] Se o instante de aviso já tiver passado quando o compromisso é registado, a notificação é criada no ciclo seguinte do processo periódico (no máximo 1 minuto depois, RNF-040).
- [ ] Não há notificações duplicadas: no máximo uma por compromisso e por destinatário, mesmo com reinícios ou execuções concorrentes (RNF-043).
- [ ] Editar a data ou a antecedência de um compromisso repõe o aviso e remover o compromisso cancela-o.
- [ ] Fichas arquivadas não geram avisos e, ao reativar uma ficha, não são criados avisos do período em que esteve arquivada (RF-122, RF-142).
- [ ] O e-mail de cada notificação é registado e enviado pelo fluxo existente, com reenvios (RF-118, RF-119), e o assunto não tem dados de saúde.
- [ ] A notificação aparece em OP-51 com o tipo ProximoCompromisso e a mensagem com o motivo, o nome do utente, a data e a hora do compromisso.
- [ ] Fica registada no Pull Request a decisão sobre compromissos cuja data já passou (se são avisados ou não).
- [ ] Os testes unitários e de integração do passo novo estão implementados e a passar, com cobertura de linhas do backend igual ou superior a 70%.
- [ ] A documentação da API (v04) e o modelo de classes do backend (v04) deixam de registar a limitação do RF-117 e descrevem o passo novo.
- [ ] Todas as User Stories da Descrição (US-I056-01 a US-I056-02) estão satisfeitas.

**Evidências do Executor**
Ligação do Pull Request integrado na main; identificação da execução da pipeline CI concluída com sucesso; relatório de testes (TRX) e relatório de cobertura; pedidos Postman ou capturas que mostrem a notificação ProximoCompromisso em OP-51.

**Dependências**
I044 (#74) - Compromissos; I047 (#75) - Notificações e envio de e-mail; I042 (#78) - Documentação da API v03; I055 (#79) - Modelo de classes v03.

**DoD aplicável**
DoD geral do grupo (m1-definition-of-done-v02.pdf); código integrado na branch main através de Pull Request revisto e auditado; pipeline CI concluída com sucesso, com os quality gates QG-01 a QG-05 cumpridos; testes a passar; cobertura de linhas do backend igual ou superior a 70%; relatório de revisão e checklist de auditoria arquivados; aceitação e confirmação final registadas na Issue.

**Normas, modelos e referências**
Regulamento de Funcionamento da UC; Regulamento Interno do Grupo; m3-especificacao-requisitos-v02.md (RF-116, RF-117); m3-documentacao-api-v03.md (secções 4, 8 e 10.4); m3-modelo-classes-backend-v03.md (secções 8.1 e 14.3); m3-modelo-de-dados-dosecerta-v03.md.

**Pasta de destino**
/projeto/04-artefactos-tecnicos/04.12-backend
