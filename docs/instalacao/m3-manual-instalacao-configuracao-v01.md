# MANUAL DE INSTALAÇÃO E CONFIGURAÇÃO DO BACKEND

| Campo | Valor |
|---|---|
| Grupo | G07 |
| Projeto | DoseCerta |
| Milestone de Entrega | m3 |
| Versão | v01 |
| Issue | I051 (Sprint 06) |
| Versão do backend | v0.3.0-m3 (`m3-notas-release-backend-v01.md`) |
| Data | 2026-11-03 |

| Versão | Alterações |
|---|---|
| v01 | Primeira versão (Issue I051): pré-requisitos, obtenção do código, SQL Server 2022, configuração dos segredos, base de dados e dados fictícios, execução, verificação final, testes, collection Postman e resolução de problemas do backend de m3 |

## 1. Finalidade e âmbito

Este manual permite a um avaliador instalar, configurar, executar e verificar o backend do DoseCerta (API REST em ASP.NET Core 8 com SQL Server 2022) num computador limpo, com Windows ou Linux, sem ajuda do grupo. Cobre a versão v0.3.0-m3, com as funcionalidades F001 a F011, sem a F007 (notas de release, `docs/releases/m3-notas-release-backend-v01.md`). Os frontends web e mobile são acrescentados em m4, numa nova versão deste manual.

O backend também está publicado num ambiente de demonstração, em https://dosecerta.spaincentral.cloudapp.azure.com. Essa publicação é automática e está descrita na descrição da pipeline CI/CD (`docs/ci-cd/m3-descricao-pipeline-ci-cd-v03.md`, secção 10), que este manual não repete. Para usar só o ambiente de demonstração, basta a secção 10.2 deste manual (collection Postman contra o URL público).

Convenções:

- Os comandos estão indicados para **PowerShell** (Windows) e para **bash** (Linux; também Git Bash no Windows). Quando o comando é igual nos dois, aparece uma só vez.
- Os valores entre `<...>` são marcadores que o avaliador substitui por valores escolhidos por si. Nenhum segredo real está neste manual nem no repositório.
- Os comandos foram executados num computador com Windows 11 (secção 13). Os comandos marcados **(Linux, não verificado neste ambiente)** seguem a documentação oficial das ferramentas, mas não foram executados pelo grupo neste manual; a verificação num ambiente limpo é feita pelo Revisor (secção 14).
- Salvo indicação em contrário, os comandos correm na pasta `backend/` do repositório.

## 2. Pré-requisitos

| Ferramenta | Versão | Para quê | Obter |
|---|---|---|---|
| Git | Qualquer versão atual | Obter o código | https://git-scm.com/downloads |
| .NET 8 SDK | 8.0.100 ou superior da linha 8.0 (o `backend/global.json` fixa `8.0.100` com `rollForward: latestFeature`) | Compilar, executar e testar | https://dotnet.microsoft.com/download/dotnet/8.0 |
| Ferramenta `dotnet-ef` | 8.0.31 (a mesma versão do EF Core em `backend/Directory.Packages.props`) | Aplicar as migrações sem arrancar a API (opcional) | `dotnet tool install --global dotnet-ef --version 8.0.31` |
| SQL Server 2022 Developer | 2022 (16.x) | Base de dados da API e dos testes de integração | Contentor Docker (secção 4.1) ou instalação local (secção 4.2) |
| Docker | Docker Desktop (Windows) ou Docker Engine (Linux) | Correr o SQL Server em contentor | https://docs.docker.com/get-docker/ |
| `sqlcmd` | Ferramenta de linha de comandos do SQL Server (versão ODBC 18 ou `go-sqlcmd`), com a opção `-C` | Executar os scripts SQL dos dados fictícios | https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility |
| `curl` | Qualquer versão atual (incluído no Windows 10 e 11 como `curl.exe`) | Verificação final | Incluído no sistema |
| Postman ou Node.js | Postman atual, ou Node.js 18 ou superior para o `newman` | Correr a collection da API (secção 10) | https://www.postman.com/downloads/ e https://nodejs.org |

Confirmar o SDK e a ferramenta do EF Core:

```bash
dotnet --list-sdks          # tem de mostrar uma versão 8.0.x
dotnet tool install --global dotnet-ef --version 8.0.31
dotnet ef --version         # Entity Framework Core .NET Command-line Tools, seguido de 8.0.31
```

Os pacotes NuGet não se instalam à mão: as versões estão fixadas em `backend/Directory.Packages.props` (por exemplo, EF Core 8.0.31, JwtBearer 8.0.31, Swashbuckle 6.9.0, xUnit 2.9.3) e são obtidas pelo `dotnet restore`.

## 3. Obter o código

```bash
git clone https://github.com/afonso24sousa/DoseCerta.git
cd DoseCerta/backend
dotnet restore DoseCerta.sln
dotnet build DoseCerta.sln --configuration Release
```

A compilação trata os avisos como erros (Regulamento Interno do Grupo, secção 11.4): termina com `0 Warning(s)` e `0 Error(s)`.

## 4. SQL Server 2022 Developer

A API e os testes de integração precisam de um SQL Server 2022 acessível por TCP, com autenticação SQL Server e um utilizador que possa criar bases de dados (o `sa` serve). A palavra-passe do `sa` é escolhida pelo avaliador e tem de cumprir a política do SQL Server: pelo menos 8 carateres, com maiúsculas, minúsculas, algarismos e símbolos. Para não ter de a escapar nos comandos, não deve ter aspas, `$`, `;` nem espaços.

### 4.1 Em contentor Docker (recomendado)

PowerShell:

```powershell
docker run -d --name dosecerta-sql -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<palavra-passe-sa>" -e "MSSQL_PID=Developer" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

bash **(Linux, não verificado neste ambiente)**:

```bash
docker run -d --name dosecerta-sql -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<palavra-passe-sa>" -e "MSSQL_PID=Developer" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

O SQL Server demora alguns segundos a arrancar. Fica pronto quando o log mostra `SQL Server is now ready for client connections`:

```bash
docker logs dosecerta-sql
```

Se a porta 1433 já estiver ocupada (por exemplo, por um SQL Server instalado no computador), use outra porta no anfitrião (`-p 14333:1433`) e substitua `localhost,1433` por `localhost,14333` em todo o manual.

### 4.2 Instalação local

- Windows: instalar o SQL Server 2022 Developer (https://www.microsoft.com/sql-server/sql-server-downloads), com autenticação mista (SQL Server e Windows). Depois, no *SQL Server Configuration Manager*, ativar o protocolo TCP/IP da instância e reiniciar o serviço; a instância predefinida usa a porta 1433.
- Linux **(não verificado neste ambiente)**: seguir o guia oficial (https://learn.microsoft.com/sql/linux/sql-server-linux-setup) e escolher a edição Developer no `mssql-conf setup`.

## 5. Configuração dos segredos

O `src/DoseCerta.Api/appsettings.json` versionado tem a connection string, a chave JWT e o SMTP vazios. Os valores locais nunca são versionados (Regulamento Interno do Grupo, secção 14.2) e definem-se de uma de três formas:

1. **`dotnet user-secrets` (recomendado).** Ficam no perfil do utilizador, fora do repositório (`UserSecretsId` `dosecerta-api-g07`), e só são lidos no ambiente `Development`, o do perfil `https` usado na secção 7.
2. **Variáveis de ambiente**, com `__` como separador (por exemplo, `ConnectionStrings__DoseCerta`, `Jwt__Chave`).
3. **Ficheiro `src/DoseCerta.Api/appsettings.Development.json`**, criado a partir de `appsettings.Development.exemplo.json`. O `.gitignore` do repositório ignora-o.

Com `dotnet user-secrets`, a partir da pasta `backend/`:

```bash
cd src/DoseCerta.Api
dotnet user-secrets set "ConnectionStrings:DoseCerta" "Server=localhost,1433;Database=DoseCerta;User Id=sa;Password=<palavra-passe-sa>;TrustServerCertificate=true;Encrypt=true"
dotnet user-secrets set "Jwt:Chave" "<chave-jwt-com-pelo-menos-32-carateres>"
dotnet user-secrets set "BaseDados:AplicarMigracoesNoArranque" "true"
dotnet user-secrets list
cd ../..
```

| Chave | Obrigatória | Valor |
|---|---|---|
| `ConnectionStrings:DoseCerta` | Sim | Ligação ao SQL Server da secção 4. A base de dados `DoseCerta` é criada pelas migrações (secção 6.1) ou pelos scripts (secção 6.2) |
| `Jwt:Chave` | Sim | Chave de assinatura dos tokens (HMAC-SHA256), com pelo menos 32 carateres. Sem ela, a API recusa os pedidos com uma mensagem a indicar como a definir |
| `BaseDados:AplicarMigracoesNoArranque` | Não | `false` por omissão. Com `true`, a API aplica as migrações em falta ao arrancar (caminho A da secção 6) |
| `Jwt:Emissor`, `Jwt:Publico` | Não | `DoseCerta` por omissão |
| `Email:Servidor`, `Email:Remetente`, `Email:Porta`, `Email:UsarTls`, `Email:Utilizador`, `Email:PalavraPasse`, `Email:NomeRemetente` | Não | Servidor SMTP. Sem `Email:Servidor` e `Email:Remetente`, a API não tenta enviar e-mails e regista um aviso no log; os convites e as notificações são criados na mesma. Por omissão, porta 587 com TLS |
| `Documentos:PastaRaiz` | Não | Pasta dos documentos carregados. Vazia: `%LOCALAPPDATA%\DoseCerta\documentos` no Windows e `~/.local/share/DoseCerta/documentos` no Linux |

O avaliador não precisa de configurar o SMTP para verificar o backend. Se o quiser fazer, pode usar uma caixa de teste (por exemplo, o Mailtrap Sandbox) e enviar uma mensagem de teste, sem arrancar o servidor web:

```bash
dotnet run --project src/DoseCerta.Api --launch-profile https -- --enviar-email-teste <endereco-de-destino>
```

## 6. Base de dados e dados fictícios

Há dois caminhos para criar o esquema, que produzem o mesmo resultado: 19 tabelas, 3 gatilhos e os valores das listas fechadas (`backend/scripts-base-dados/README.md`). Os dados de demonstração vêm do `02-dados-iniciais-ficticios.sql`, só com dados fictícios (RNF-050).

| Caminho | Quando usar | `BaseDados:AplicarMigracoesNoArranque` |
|---|---|---|
| A: migrações do EF Core e, depois, o script 02 | Primeira instalação (recomendado). É o caminho da API, dos testes de integração e do ambiente de demonstração | `true` |
| B: scripts 01 e 02, ou `repor-base-dados.sh` | Repor a base de dados no estado inicial de demonstração, ou criá-la sem o .NET | `false` (o histórico das migrações não existe) |

Os scripts executam-se com o `sqlcmd`, sempre com `-I` (os índices filtrados exigem `QUOTED_IDENTIFIER ON`) e, no Windows, com `-f 65001` (os scripts estão em UTF-8). A palavra-passe passa pela variável `SQLCMDPASSWORD`, para não ficar no histórico do terminal:

```powershell
# PowerShell
$env:SQLCMDPASSWORD = "<palavra-passe-sa>"
```

```bash
# bash
export SQLCMDPASSWORD='<palavra-passe-sa>'
```

### 6.1 Caminho A: migrações e dados fictícios

1. Com `BaseDados:AplicarMigracoesNoArranque` a `true` (secção 5), arrancar a API (secção 7). Ao arrancar, a API cria a base de dados `DoseCerta` e aplica as migrações `Inicial` e `RetirarTarefasRecorrentes`.
   Em alternativa, sem arrancar a API, aplicar as migrações com o `dotnet-ef`, que lê a ligação da variável `ConnectionStrings__DoseCerta`:

   ```powershell
   # PowerShell
   $env:ConnectionStrings__DoseCerta = "Server=localhost,1433;Database=DoseCerta;User Id=sa;Password=<palavra-passe-sa>;TrustServerCertificate=true;Encrypt=true"
   dotnet ef database update --project src/DoseCerta.Infrastructure --startup-project src/DoseCerta.Infrastructure
   ```

   ```bash
   # bash
   export ConnectionStrings__DoseCerta="Server=localhost,1433;Database=DoseCerta;User Id=sa;Password=<palavra-passe-sa>;TrustServerCertificate=true;Encrypt=true"
   dotnet ef database update --project src/DoseCerta.Infrastructure --startup-project src/DoseCerta.Infrastructure
   ```

2. Aplicar os dados fictícios, uma só vez (o script só tem `INSERT` com identificadores fixos e falha numa segunda execução, sem deixar dados parciais):

   ```bash
   sqlcmd -S localhost,1433 -U sa -C -I -b -f 65001 -d DoseCerta -i scripts-base-dados/02-dados-iniciais-ficticios.sql
   ```

### 6.2 Caminho B: repor a base de dados com os scripts

O `repor-base-dados.sh` cria a base de dados se não existir e executa, por esta ordem, o `03-repor-base-dados.sql` (remove os gatilhos e todas as tabelas, incluindo `__EFMigrationsHistory`), o `01-criar-base-dados.sql` e o `02-dados-iniciais-ficticios.sql`. **Apaga todos os dados da base indicada.** Corre em bash ou no Git Bash:

```bash
./scripts-base-dados/repor-base-dados.sh localhost,1433 DoseCerta sa
```

Depois, a API usa-se com `BaseDados:AplicarMigracoesNoArranque` a `false`:

```bash
cd src/DoseCerta.Api
dotnet user-secrets set "BaseDados:AplicarMigracoesNoArranque" "false"
cd ../..
```

### 6.3 Contas fictícias

O script 02 cria 4 contas, 3 fichas, associações e convites, todos inventados. A palavra-passe é a mesma nas 4 contas e é fictícia: `Demo2026x`.

| Conta | E-mail | Situação inicial |
|---|---|---|
| Ana Fictícia | `ana@exemplo.pt` | Cuidadora principal da ficha de Maria Fictícia Santos e titular da sua ficha própria |
| Rui Fictício | `rui@exemplo.pt` | Familiar na ficha de Maria Fictícia Santos |
| Joana Fictícia | `joana@exemplo.pt` | Cuidadora principal provisória da ficha de Alberto Fictício Costa |
| Carlos Fictício | `carlos@exemplo.pt` | Com um convite pendente para Cuidador principal da ficha de Alberto Fictício Costa |

## 7. Executar a API

Num computador limpo, confiar primeiro no certificado HTTPS de desenvolvimento do .NET (uma vez por computador; no Windows, aceitar o aviso de segurança):

```bash
dotnet dev-certs https --trust
```

No Linux **(não verificado neste ambiente)**, `--trust` só é suportado em algumas distribuições. Sempre que o certificado não for de confiança, acrescente `-k` aos comandos `curl` (sem ele, o `curl` termina com o código 60), use `--insecure` no `newman` e, no Postman, desative a verificação do certificado (secção 10). No Git Bash (Windows), o `curl` pode não usar o repositório de certificados do Windows: use `-k`.

Arrancar a API com o perfil `https` (ambiente `Development`):

```bash
dotnet run --project src/DoseCerta.Api --launch-profile https
```

| Elemento | Valor |
|---|---|
| Projeto de arranque | `src/DoseCerta.Api` |
| URL HTTPS | `https://localhost:7180` |
| URL HTTP | `http://localhost:5180` (redirecionado para HTTPS com `307 Temporary Redirect`) |
| Prefixo da API | `/api/v1` |
| Verificação do estado | `GET /saude`, anónimo |
| Documentação interativa | `https://localhost:7180/swagger` (só no ambiente `Development`) |

A API está pronta quando o terminal mostra `Now listening on: https://localhost:7180` (com o caminho A, depois de `Applying migration '20261014232229_Inicial'` e `Applying migration '20261020175938_RetirarTarefasRecorrentes'` na primeira execução) e fica a correr até se carregar em `Ctrl+C`. O processo periódico (tomas em atraso, envio de e-mails) corre de 30 em 30 segundos enquanto a API está ativa.

## 8. Verificação final

Com a API a correr, noutro terminal:

1. Estado da API e da base de dados:

   ```bash
   curl https://localhost:7180/saude
   ```

   Resposta esperada (HTTP 200):

   ```json
   {"estado":"Saudavel","verificacoes":{"baseDados":"Saudavel"}}
   ```

2. Início de sessão com uma conta fictícia (secção 6.3). A resposta 200 tem o campo `token`.

   PowerShell (o `Invoke-RestMethod` do Windows PowerShell 5.1 exige o certificado de confiança da secção 7):

   ```powershell
   $sessao = Invoke-RestMethod -Method Post -Uri https://localhost:7180/api/v1/auth/login -ContentType "application/json" -Body '{"email":"ana@exemplo.pt","palavraPasse":"Demo2026x"}'
   Invoke-RestMethod -Uri https://localhost:7180/api/v1/fichas -Headers @{ Authorization = "Bearer $($sessao.token)" }
   ```

   bash:

   ```bash
   TOKEN=$(curl -s -X POST https://localhost:7180/api/v1/auth/login -H "Content-Type: application/json" -d '{"email":"ana@exemplo.pt","palavraPasse":"Demo2026x"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
   curl -s https://localhost:7180/api/v1/fichas -H "Authorization: Bearer $TOKEN"
   ```

   A lista de fichas da Ana tem 2 fichas: a sua ficha própria (`"nomeCompleto":"Ana Fictícia"`, `"eProprio":true`) e a de Maria Fictícia Santos.

A instalação está concluída quando o `/saude` responde `Saudavel` e o início de sessão devolve um token que dá acesso às fichas.

## 9. Testes

Os testes unitários (QG-02) não precisam de base de dados:

```bash
dotnet test tests/DoseCerta.UnitTests/DoseCerta.UnitTests.csproj --configuration Release
```

Os testes de integração (QG-03) arrancam a API em memória contra o SQL Server da secção 4. Cada execução cria uma base de dados própria (`DoseCerta_Testes_<guid>`), aplica-lhe as migrações e apaga-a no fim. A instância indica-se na variável `DOSECERTA_TESTES_SQLSERVER`, com uma connection string **sem** o nome da base de dados:

```powershell
# PowerShell
$env:DOSECERTA_TESTES_SQLSERVER = "Server=localhost,1433;User Id=sa;Password=<palavra-passe-sa>;TrustServerCertificate=true"
dotnet test tests/DoseCerta.IntegrationTests/DoseCerta.IntegrationTests.csproj --configuration Release
```

```bash
# bash
export DOSECERTA_TESTES_SQLSERVER="Server=localhost,1433;User Id=sa;Password=<palavra-passe-sa>;TrustServerCertificate=true"
dotnet test tests/DoseCerta.IntegrationTests/DoseCerta.IntegrationTests.csproj --configuration Release
```

Os dois conjuntos passam a 100%. As verificações completas de cada Pull Request (análise estática, formatação e cobertura mínima de 70%) estão no `backend/README.md`, secção 7, e na descrição da pipeline CI/CD, secção 4.

## 10. Collection Postman

A collection e os ambientes estão em `docs/postman/` (pasta oficial `03.01.05-postman`):

| Ficheiro | Conteúdo |
|---|---|
| `m3-collection-postman-v02.json` | Pedidos de F001 a F011, sem a F007, organizados por funcionalidade, com verificações |
| `m3-ambiente-local-postman-v02.json` | `baseUrl` `https://localhost:7180/api/v1` e credenciais fictícias |
| `m3-ambiente-publico-postman-v01.json` | `baseUrl` do ambiente de demonstração e credenciais fictícias |

A collection regista contas novas em cada execução (e-mails `postman.<ambiente>.<instante>.<conta>@exemplo.pt`), obtém o token sozinha e deve correr do princípio ao fim, pela ordem das pastas. Não deve ser executada nos últimos minutos do dia (hora de Lisboa), porque algumas tomas usam as 23:57 a 23:59.

### 10.1 Contra a API local

- Postman: *Import* dos dois ficheiros (collection e ambiente local); escolher o ambiente `m3-ambiente-local-postman-v02`; se o certificado da secção 7 não for de confiança, desativar *Settings > General > SSL certificate verification*; correr a collection com o *Collection Runner*.
- newman, a partir de `docs/postman/`:

  ```bash
  npx -y -p newman@6.2.1 newman run m3-collection-postman-v02.json -e m3-ambiente-local-postman-v02.json --insecure
  ```

### 10.2 Contra o ambiente de demonstração

```bash
npx -y -p newman@6.2.1 newman run m3-collection-postman-v02.json -e m3-ambiente-publico-postman-v01.json
```

A última execução contra o URL público, com 0 falhas, está no relatório de evidências da pipeline (`docs/ci-cd/m3-relatorio-evidencias-pipeline-ci-cd-v01.md`, secção 7.6).

## 11. Resolução de problemas

| Sintoma | Causa provável | Solução |
|---|---|---|
| `dotnet` diz que o SDK `8.0.100` não foi encontrado | Não está instalado nenhum SDK 8.0.x | Instalar o .NET 8 SDK (secção 2); o `global.json` aceita qualquer 8.0.x igual ou superior |
| `dotnet ef` não é reconhecido | A ferramenta não está instalada, ou a pasta das ferramentas globais não está no `PATH` | `dotnet tool install --global dotnet-ef --version 8.0.31` e reabrir o terminal |
| O contentor `dosecerta-sql` para logo depois de arrancar | Palavra-passe do `sa` fora da política do SQL Server | `docker logs dosecerta-sql`; recriar o contentor com uma palavra-passe mais forte |
| `docker run` falha com a porta 1433 ocupada | Outro SQL Server no computador | Usar `-p 14333:1433` e `localhost,14333` (secção 4.1) |
| `/saude` responde 503 com `"estado":"Indisponivel"` | SQL Server parado, base de dados inexistente ou credenciais erradas | Confirmar o contentor (`docker ps`), a connection string (`dotnet user-secrets list`) e `BaseDados:AplicarMigracoesNoArranque` a `true` (caminho A) |
| Erro `A chave JWT (Jwt:Chave) não está configurada` | Falta a chave, ou tem menos de 32 carateres | Definir `Jwt:Chave` (secção 5) |
| Os user-secrets não têm efeito | A API não está no ambiente `Development` | Arrancar com `--launch-profile https`, ou usar variáveis de ambiente |
| `curl` falha com um erro de certificado | Certificado de desenvolvimento não confiado | `dotnet dev-certs https --trust` (secção 7) ou `curl -k` |
| O login responde 401 com uma conta fictícia | Os dados do script 02 não foram aplicados | Executar o script 02 (secção 6.1) |
| O script 02 falha com `Violation of PRIMARY KEY constraint 'PK_Utilizador'` | Já tinha sido aplicado | Não é preciso aplicá-lo de novo; para voltar ao estado inicial, usar o caminho B |
| `sqlcmd` falha com `SET options have incorrect settings: 'QUOTED_IDENTIFIER'` | Falta a opção `-I` | Executar com `-I`, como na secção 6 |
| Acentos errados nas listas fechadas depois dos scripts, no Windows | O `sqlcmd` leu os scripts com outra página de código | Acrescentar `-f 65001` |
| `sqlcmd` falha com um erro de certificado do servidor | Falta a opção `-C` (confiar no certificado do servidor) | Executar com `-C` |
| A API termina ao arrancar com `There is already an object named 'Alergia' in the database` | `BaseDados:AplicarMigracoesNoArranque` a `true` numa base criada pelos scripts (caminho B) | Pôr a chave a `false` (secção 6.2) |
| Testes de integração falham com `Defina a variável de ambiente DOSECERTA_TESTES_SQLSERVER` | A variável não está definida no terminal do `dotnet test` | Defini-la no mesmo terminal (secção 9) |
| O newman local falha com `self signed certificate` | Certificado de desenvolvimento | Acrescentar `--insecure` (secção 10.1) |

## 12. Publicação no ambiente de demonstração

A publicação automática no Azure (workflow `backend-cd`, máquina virtual, Docker Compose, Caddy, segredos do environment `demonstracao`, base de dados e administração) está descrita na descrição da pipeline CI/CD (`docs/ci-cd/m3-descricao-pipeline-ci-cd-v03.md`, secções 8 e 10) e não é repetida aqui. As evidências da última publicação estão no relatório de evidências da pipeline (`docs/ci-cd/m3-relatorio-evidencias-pipeline-ci-cd-v01.md`, secção 7).

## 13. Verificação dos comandos pelo grupo

Em 2026-11-03, entre as 18:50 e as 19:00 (UTC), o Executor seguiu este manual em Windows 11, com o .NET SDK 8.0.426, o Docker 29.8.2 e o `sqlcmd` ODBC 18. Usou uma cópia nova do repositório (`git clone` da `main`, commit `7bf212c`) e um contentor novo do SQL Server 2022 na porta 14333 do anfitrião (secção 4.1). Os user-secrets foram gravados num perfil temporário, sem nenhuma configuração anterior. No fim, o contentor e a cópia foram apagados. Os comandos de PowerShell e de bash (Git Bash) foram executados; os comandos marcados para Linux não foram executados neste ambiente.

| Secção | Verificação | Resultado |
|---|---|---|
| 2 | `dotnet --list-sdks`; `dotnet tool install --global dotnet-ef --version 8.0.31`; `dotnet ef --version` | SDK 8.0.426; `dotnet-ef` 8.0.31 |
| 3 | `git clone`, `dotnet restore` e `dotnet build --configuration Release` | Restauro sem erros; `Build succeeded`, 0 avisos e 0 erros |
| 4.1 | `docker run` com `-p 14333:1433` e `docker logs` | `SQL Server is now ready for client connections` |
| 5 | 3 comandos `dotnet user-secrets set` e `dotnet user-secrets list` | As 3 chaves gravadas |
| 6.1 | Arranque da API com a flag a `true`; script 02 com o `sqlcmd` | Migrações `Inicial` e `RetirarTarefasRecorrentes` aplicadas; 19 tabelas, 3 gatilhos e 4 contas |
| 6.1 | `dotnet ef database update` noutra base de dados, com `ConnectionStrings__DoseCerta` | As 2 migrações aplicadas (`Done.`); 19 tabelas e 3 gatilhos |
| 6.1 | Script 02 executado uma segunda vez | Falha com `Violation of PRIMARY KEY constraint 'PK_Utilizador'` (secção 11) |
| 6.2 | `repor-base-dados.sh localhost,14333 DoseCerta sa` no Git Bash | 03, 01 e 02 executados; 19 tabelas, 3 gatilhos, 4 contas e sem `__EFMigrationsHistory` |
| 6.2 | API sobre a base do caminho B, com a flag a `true` e depois a `false` | Com `true`, termina com `There is already an object named 'Alergia'` (secção 11); com `false`, arranca e o `/saude` responde `Saudavel` |
| 7 | `dotnet run --project src/DoseCerta.Api --launch-profile https` | `Now listening on: https://localhost:7180` e `http://localhost:5180`; o HTTP responde `307` para HTTPS |
| 7 | `dotnet dev-certs https --trust` | **Não executado**: abre um diálogo do Windows e altera os certificados de raiz do computador. Por isso, os pedidos HTTPS foram feitos com `curl -k` e, no PowerShell, com a verificação do certificado desligada só na sessão. Sem `-k`, o `curl` termina com o código 60, como diz a secção 7 |
| 8 | `curl -k https://localhost:7180/saude` | HTTP 200, `{"estado":"Saudavel","verificacoes":{"baseDados":"Saudavel"}}` |
| 8 | Login com `ana@exemplo.pt` e listagem das fichas, em PowerShell e em bash | Token devolvido; 2 fichas (Ana Fictícia, própria, e Maria Fictícia Santos) |
| 9 | Testes unitários e de integração, com `DOSECERTA_TESTES_SQLSERVER` | 454 unitários e 177 de integração, todos a passar |
| 10.1 | `newman run ... -e m3-ambiente-local-postman-v02.json --insecure` | 130 pedidos e 277 asserções, 0 falhas |
| 10.2 | `newman run ... -e m3-ambiente-publico-postman-v01.json` | Não repetido nesta verificação; última execução, com 0 falhas, no relatório de evidências da pipeline (secção 7.6) |

## 14. Lista de verificação para o Revisor

O Revisor segue este manual num computador limpo, sem ajuda do Executor (Definition of Done), e regista o resultado de cada passo no relatório de revisão.

- [ ] 1. Os pré-requisitos instalam-se pelas ligações da secção 2 e `dotnet --list-sdks` e `dotnet ef --version` mostram as versões indicadas.
- [ ] 2. O `dotnet build` da secção 3 termina com 0 avisos e 0 erros.
- [ ] 3. O SQL Server 2022 arranca (secção 4.1 ou 4.2).
- [ ] 4. Os segredos ficam definidos só com marcadores substituídos pelo Revisor, sem alterar nenhum ficheiro versionado (`git status` sem alterações).
- [ ] 5. O caminho A cria a base de dados e o script 02 aplica os dados fictícios (secção 6.1).
- [ ] 6. A API arranca com o perfil `https` (secção 7).
- [ ] 7. `/saude` responde `Saudavel` e o login com `ana@exemplo.pt` devolve um token que lista as fichas (secção 8).
- [ ] 8. Os testes unitários e de integração passam a 100% (secção 9).
- [ ] 9. A collection Postman corre contra a API local com 0 falhas (secção 10.1).
- [ ] 10. O caminho B repõe a base de dados e a API arranca com `BaseDados:AplicarMigracoesNoArranque` a `false` (secção 6.2).
- [ ] 11. Os problemas encontrados estão na secção 11 ou ficam registados como defeito do manual.
- [ ] 12. Opcional, em Linux: os comandos marcados como não verificados funcionam.

## 15. Cobertura

| Exigência | Origem | Onde é cumprida |
|---|---|---|
| CA1: o manual permite instalar e executar o backend num computador limpo, seguindo apenas as instruções | Issue I051 | Secções 2 a 8; verificação na secção 13 e lista do Revisor na secção 14 |
| CA2: indica as versões das ferramentas | Issue I051 | Secção 2 |
| CA2: indica a configuração dos segredos sem os expor | Issue I051 | Secção 5 (marcadores `<...>`, user-secrets, variáveis de ambiente e ficheiro ignorado pelo Git) |
| CA2: indica a verificação final | Issue I051 | Secção 8 |
| CA4: US-I051-01 (instalar, configurar e executar o backend num computador limpo, sem ajuda do grupo) | Issue I051 | Secções 1 a 11 e 14 |
| Pré-requisitos | Regulamento da UC, secção 19 (04.17) | Secção 2 |
| Instalação | Regulamento da UC, secção 19 (04.17) | Secções 3 e 4 |
| Configuração | Regulamento da UC, secção 19 (04.17) | Secções 5 e 6 |
| Inicialização | Regulamento da UC, secção 19 (04.17) | Secção 7 |
| Verificação | Regulamento da UC, secção 19 (04.17) | Secções 8 a 10 |
| Resolução de problemas | Regulamento da UC, secção 19 (04.17) | Secção 11 |

O CA3 (notas de release) e a US-I051-02 são cumpridos em `docs/releases/m3-notas-release-backend-v01.md`.
