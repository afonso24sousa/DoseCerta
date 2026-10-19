# DoseCerta: backend

API REST do DoseCerta em ASP.NET Core 8 (C# 12), com Entity Framework Core 8 sobre SQL Server 2022 e autenticação JWT.
Este ficheiro contém as instruções técnicas para clonar, configurar, compilar, executar e testar o backend num computador limpo
(ambiente individual de desenvolvimento e de defesa, Regulamento Interno do Grupo, secção 18.3).

Esta versão corresponde à configuração da solução (Issue I029): camadas, autenticação JWT, ligação ao SQL Server,
verificação do estado da aplicação, serviço de e-mail e testes; e ao modelo de dados (Issue I030): entidades, mapeamento
EF Core, migração inicial e scripts SQL. Ainda não existem endpoints de negócio.

## 1. Estrutura da solução

A solução segue a arquitetura em camadas da documentação de arquitetura (`m2-documentacao-arquitetura-v01.md`, secção 5)
e os pacotes do modelo de classes do backend (`m2-modelo-classes-backend-v01.md`, secção 3). As dependências só vão da
apresentação para a persistência.

| Projeto | Camada | Conteúdo nesta versão |
|---|---|---|
| `src/DoseCerta.Api` | API REST e raiz de composição | arranque (`Arranque/`), erros (`Erros/`), `GET /saude` (`Saude/`), validação JWT (`Seguranca/`) |
| `src/DoseCerta.Application` | Serviços de aplicação | abstrações `IEmailSender`, `IRelogio`, `IUnidadeDeTrabalho` e o registo da camada |
| `src/DoseCerta.Domain` | Domínio | entidades do modelo de dados (`Entidades/`), listas de valores (`Enumerados/`), exceções e códigos de erro (`Comum/`), calendário de Lisboa (`Tempo/`) |
| `src/DoseCerta.Infrastructure` | Infraestrutura | `DoseCertaDbContext` com as 23 tabelas, mapeamento (`Persistence/Configuracoes/`), gatilhos (`Persistence/GatilhosSql.cs`), migrações (`Persistence/Migrations/`), unidade de trabalho, e-mail SMTP, opções JWT, relógio |
| `scripts-base-dados` | Base de dados | scripts SQL de criação, dados fictícios e reposição (ver secção 9) |
| `tests/DoseCerta.UnitTests` | Testes unitários (QG-02) | sem base de dados nem rede |
| `tests/DoseCerta.IntegrationTests` | Testes de integração (QG-03) | API em memória contra um SQL Server real |

Ficheiros de configuração da qualidade (Regulamento Interno do Grupo, secção 11): `global.json` (SDK .NET 8),
`Directory.Build.props` (AnalysisMode Recommended, StyleCop, avisos como erros), `Directory.Packages.props` (versões fixadas),
`.editorconfig`, `stylecop.json` e `coverage.runsettings` (cobertura de linhas, exclui `Migrations/` e `Program.cs`).

## 2. Pré-requisitos

- Git.
- .NET 8 SDK (8.0.100 ou superior da linha 8.0): `dotnet --list-sdks` tem de mostrar uma versão 8.0.x.
- SQL Server 2022 Developer Edition local, ou o contentor `mcr.microsoft.com/mssql/server:2022-latest`.
- Opcional, para o relatório de cobertura em HTML: `dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.5.11`.

Exemplo de SQL Server em contentor (palavra-passe escolhida por cada aluno, nunca versionada):

```bash
docker run -d --name dosecerta-sql -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=<palavra-passe-local>" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

## 3. Obter o código

```bash
git clone <url-do-repositorio> DoseCerta
cd DoseCerta/backend
dotnet restore DoseCerta.sln
```

## 4. Configuração local (segredos fora do repositório)

Os segredos nunca são versionados (Regulamento Interno do Grupo, secção 14.2): o `appsettings.json` versionado tem a
connection string, a chave JWT e as credenciais SMTP vazias. Cada aluno define-as com `dotnet user-secrets` (recomendado),
com variáveis de ambiente ou com um `appsettings.Development.json` não versionado, criado a partir de
`src/DoseCerta.Api/appsettings.Development.exemplo.json` (o `.gitignore` do repositório ignora esse ficheiro).

```bash
cd src/DoseCerta.Api
dotnet user-secrets set "ConnectionStrings:DoseCerta" "Server=localhost,1433;Database=DoseCerta;User Id=sa;Password=<palavra-passe-local>;TrustServerCertificate=true;Encrypt=true"
dotnet user-secrets set "Jwt:Chave" "<segredo-local-com-pelo-menos-32-carateres>"
dotnet user-secrets set "BaseDados:AplicarMigracoesNoArranque" "true"
# Serviço de e-mail transacional (endereço dedicado ao projeto, RNF-052)
dotnet user-secrets set "Email:Servidor" "<servidor-smtp>"
dotnet user-secrets set "Email:Porta" "587"
dotnet user-secrets set "Email:Utilizador" "<utilizador-smtp>"
dotnet user-secrets set "Email:PalavraPasse" "<chave-smtp>"
dotnet user-secrets set "Email:Remetente" "<endereco-dedicado-do-projeto>"
cd ../..
```

Também são aceites variáveis de ambiente com `__` como separador, por exemplo `Jwt__Chave`, `ConnectionStrings__DoseCerta`
e `Email__Servidor`. Os user-secrets só são lidos no ambiente `Development` (o perfil `https` do `launchSettings.json`).

| Chave | Obrigatória | Descrição |
|---|---|---|
| `ConnectionStrings:DoseCerta` | sim | Ligação ao SQL Server. A base de dados tem de existir (ou ser criada pelas migrações) para o `/saude` responder `Saudavel`. |
| `BaseDados:AplicarMigracoesNoArranque` | não | `false` no `appsettings.json`; com `true`, a API aplica as migrações em falta ao arrancar. |
| `Jwt:Chave` | sim | Chave HMAC-SHA256 com pelo menos 32 carateres. Sem ela, a API recusa os pedidos com uma mensagem a indicar como a definir. |
| `Jwt:Emissor`, `Jwt:Publico` | não | Por omissão `DoseCerta`. |
| `Email:Servidor`, `Email:Remetente` | para enviar e-mail | Sem estes valores o envio não é tentado e fica registado um aviso no log. |
| `Email:Porta`, `Email:UsarTls`, `Email:Utilizador`, `Email:PalavraPasse`, `Email:NomeRemetente` | não | Por omissão 587, TLS ativo e nome `DoseCerta`. |

Com `BaseDados:AplicarMigracoesNoArranque` a `true`, a API aplica as migrações do EF Core ao arrancar e cria a base de dados
`DoseCerta` se ainda não existir, com as 23 tabelas, os 4 gatilhos e os valores predefinidos das listas fechadas (secção 8).
O utilizador da connection string precisa de permissão para criar bases de dados.

## 5. Compilar e executar

```bash
dotnet build DoseCerta.sln --configuration Release
dotnet run --project src/DoseCerta.Api --launch-profile https
```

Verificação do estado da aplicação (endpoint anónimo, sem sessão), que confirma a ligação ao SQL Server:

```bash
curl -k https://localhost:7180/saude
# {"estado":"Saudavel","verificacoes":{"baseDados":"Saudavel"}}
```

Se a base de dados não estiver acessível, a resposta é 503 com `"estado":"Indisponivel"`.
Todos os outros endpoints exigem um token JWT válido no cabeçalho `Authorization: Bearer <token>`.
Em desenvolvimento, a documentação interativa da API fica em `https://localhost:7180/swagger`.

## 6. Enviar uma mensagem de teste pelo serviço de e-mail

Depois de configurar a secção `Email`, o comando seguinte envia uma mensagem de teste e termina, sem arrancar o servidor web
(código de saída 0 se o serviço SMTP aceitou a mensagem; 1 caso contrário, com o motivo no log):

```bash
dotnet run --project src/DoseCerta.Api --launch-profile https -- --enviar-email-teste <endereco-de-destino>
```

## 7. Testes, cobertura e análise estática

Os testes de integração arrancam a API em memória e criam, aplicando as migrações, uma base de dados exclusiva
(`DoseCerta_Testes_<guid>`), eliminada no fim. A instância do SQL Server indica-se na variável `DOSECERTA_TESTES_SQLSERVER`
(connection string sem o nome da base de dados; o utilizador precisa de permissão para criar bases de dados):

```bash
# bash
export DOSECERTA_TESTES_SQLSERVER="Server=localhost,1433;User Id=sa;Password=<palavra-passe-local>;TrustServerCertificate=true"
# PowerShell
$env:DOSECERTA_TESTES_SQLSERVER = "Server=localhost,1433;User Id=sa;Password=<palavra-passe-local>;TrustServerCertificate=true"
```

Verificações exigidas antes de abrir um pull request (Quality Gates QG-02 a QG-05):

```bash
dotnet build DoseCerta.sln --configuration Release -warnaserror
dotnet format DoseCerta.sln --verify-no-changes
dotnet test DoseCerta.sln --configuration Release --settings coverage.runsettings --collect "XPlat Code Coverage" --logger trx --results-directory artefactos/testes
reportgenerator -reports:"artefactos/testes/**/coverage.cobertura.xml" -targetdir:artefactos/cobertura -reporttypes:"Html;Cobertura;JsonSummary"
```

A compilação falha com qualquer aviso dos Roslyn Analyzers ou do StyleCop, e `dotnet format --verify-no-changes`
termina sem sucesso se existirem diferenças de formatação. A pasta `artefactos/` não é versionada.

## 8. Migrações do EF Core

As migrações ficam em `src/DoseCerta.Infrastructure/Persistence/Migrations` (código gerado, excluído da análise estática e
da cobertura). A migração inicial (`Inicial`) cria as 23 tabelas, as chaves, as restrições `CHECK`, os índices, os valores
predefinidos das listas fechadas e, no fim do `Up`, os 4 gatilhos (`migrationBuilder.CriarGatilhosDoseCerta()`; o `Down`
começa por `migrationBuilder.RemoverGatilhosDoseCerta()`), porque o EF Core não gera gatilhos.

Aplicar as migrações sem arrancar a API (com a variável `ConnectionStrings__DoseCerta` definida):

```bash
dotnet ef database update --project src/DoseCerta.Infrastructure --startup-project src/DoseCerta.Infrastructure
```

Gerar uma nova migração depois de alterar o mapeamento:

```bash
dotnet ef migrations add <Nome> --project src/DoseCerta.Infrastructure --startup-project src/DoseCerta.Infrastructure --output-dir Persistence/Migrations
```

O mapeamento segue o script `scripts-base-dados/01-criar-base-dados.sql` (modelo de dados, secção 13.2). Uma base de dados
criada pela migração e outra criada pelo `01` têm o mesmo esquema; as únicas diferenças são formais e estão descritas em
`scripts-base-dados/README.md`.

## 9. Scripts SQL e dados fictícios

A pasta `scripts-base-dados/` tem o caminho alternativo às migrações e os dados de demonstração (só dados fictícios, RNF-050):

| Script | Finalidade |
|---|---|
| `01-criar-base-dados.sql` | Cria o esquema completo numa base de dados vazia |
| `02-dados-iniciais-ficticios.sql` | Insere contas, fichas, associações e convites fictícios (depois do `01` ou das migrações) |
| `03-repor-base-dados.sql` | Remove os gatilhos e todas as tabelas, incluindo `__EFMigrationsHistory` |
| `repor-base-dados.sh` | Executa `03`, `01` e `02` com o `sqlcmd` (bash ou Git Bash) |

```bash
export SQLCMDPASSWORD='<palavra-passe-local>'   # nunca versionar
./scripts-base-dados/repor-base-dados.sh localhost,1433 DoseCerta sa
```

Uma base de dados criada pelos scripts usa-se com `BaseDados:AplicarMigracoesNoArranque` a `false`. Os pormenores estão em
`scripts-base-dados/README.md`.

## 10. Resolução de problemas

- `/saude` responde 503: confirme que o SQL Server está a correr, que a base de dados da connection string existe
  (ou que `BaseDados:AplicarMigracoesNoArranque` está a `true`) e que o utilizador tem acesso.
- Erro `A chave JWT (Jwt:Chave) não está configurada`: defina a chave com `dotnet user-secrets` ou `Jwt__Chave`.
- Testes de integração falham com `Defina a variável de ambiente DOSECERTA_TESTES_SQLSERVER`: defina a variável no terminal
  onde corre `dotnet test` (ou nas variáveis de ambiente do utilizador e reabra o terminal).
- `sqlcmd` falha com `SET options have incorrect settings: 'QUOTED_IDENTIFIER'`: execute os scripts com `-I` (como faz o
  `repor-base-dados.sh`); os índices filtrados exigem `QUOTED_IDENTIFIER ON`.
- Acentos errados nas listas fechadas depois de executar os scripts com o `sqlcmd` no Windows: acrescente `-f 65001`
  (os scripts estão em UTF-8).
