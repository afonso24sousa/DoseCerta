**I057 - Criar a estrutura base do frontend web e implementar a autenticação e a gestão de conta (F001)**

Tipo: Desenvolvimento · Prioridade: 5 - Muito baixa · Milestone: m4 · Sprint: S07 · Orçamento: 50 (Executor 30 / Revisor 15 / Auditor 5)
Executor: Ricardo André Santos Ribeiro · Revisor: _(Sprint Planning)_ · Auditor: _(Sprint Planning)_ · Prazo do Executor: _(Sprint Planning)_

**User Stories**
- US-I057-01 - Como Utilizador, quero criar conta, iniciar sessão e terminar sessão no frontend web, para aceder às fichas a que estou associado.
- US-I057-02 - Como Utilizador, quero editar os dados da minha conta no frontend web, para os manter atualizados.
- US-I057-03 - Como programador do frontend web, quero uma estrutura base com a configuração do endereço da API, o serviço de autenticação, o interceptor do token e a navegação protegida, para implementar as restantes funcionalidades sobre ela.

**Descrição técnica**
Criar o projeto Angular 18 em frontend-web/, com ESLint (angular-eslint) e Prettier configurados conforme o Regulamento Interno (secções 11.2 a 11.4); criar o ApiConfig com o endereço base HTTPS, o AuthService, o interceptor que envia o token JWT e trata a resposta 401, a guarda das rotas autenticadas, o layout e a navegação; implementar os ecrãs de registo, início de sessão e edição da conta (E01 a E03) segundo o protótipo de baixa fidelidade v02 e o modelo de classes do frontend web v02.

**Objetivo**
Disponibilizar a base do frontend web e o acesso autenticado ao DoseCerta, sobre o qual assentam as restantes funcionalidades da m4.

**Resultado esperado**
Projeto frontend-web/ integrado na branch main, com a estrutura base e a F001 a funcionar contra o backend publicado.

**Critérios de aceitação**
- [ ] Os ecrãs de registo, início de sessão e edição da conta seguem o protótipo de baixa fidelidade v02.
- [ ] A validação prévia da palavra-passe cumpre a RNF-003 e o servidor continua a validar.
- [ ] Os erros de validação indicam o campo e a regra e mantêm os dados preenchidos (RNF-028).
- [ ] Perante uma resposta 401 ou a sessão expirada o Utilizador volta ao início de sessão.
- [ ] Nenhuma rota autenticada abre sem sessão.
- [ ] Todos os pedidos usam o endereço base HTTPS (RNF-002).
- [ ] O terminar sessão apaga o token guardado.
- [ ] ng build, ESLint e Prettier terminam sem erros.
- [ ] Todas as User Stories da Descrição (US-I057-01 a US-I057-03) estão satisfeitas.

**Evidências do Executor**
Ligação do Pull Request integrado na branch main; identificação da execução da pipeline do componente concluída com sucesso (ou, antes da I061, logs locais da build e do linter); capturas dos ecrãs a funcionar contra o backend publicado, com dados fictícios.

**Dependências**
I032 - Autenticação e gestão de conta no backend (F001); I040 - URL público; I025 - Modelo de classes do frontend web; I020 - Protótipo de baixa fidelidade.

**DoD aplicável**
DoD geral do grupo (m1-definition-of-done-v02.pdf); código integrado na branch main através de Pull Request revisto e auditado; frontend-web-ci concluída com sucesso (QG-08 e QG-09; QG-06 a partir do primeiro cenário); comportamento coerente com a documentação da API e com a matriz de permissões; apenas dados fictícios; relatório de revisão e checklist de auditoria arquivados; aceitação e confirmação final registadas na Issue.

**Normas, modelos e referências**
Regulamento de Funcionamento da UC (secções 16, 18 e 19); Regulamento Interno do Grupo (secções 10 e 11.2 a 11.6); m3-especificacao-requisitos-v02.md; m3-especificacoes-casos-uso-v02.md; m3-modelo-classes-frontend-web-v02.md; m3-prototipo-baixa-fidelidade-v02.pdf; m3-matriz-permissoes-ficha-v02.md; m3-documentacao-api-v03.md (I042).

**Pasta de destino**
/projeto/04-artefactos-tecnicos/04.14-frontend-web/m4-v01
