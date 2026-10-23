# DoseCerta: scripts da base de dados (SQL Server 2022)

Issues I030 e I037. Modelo de dados: `m3-modelo-de-dados-dosecerta-v02.md` (secções 6 a 8, 13 e 15). A F007 (tarefas de
cuidados recorrentes) foi retirada do âmbito: os scripts já não criam as suas quatro tabelas nem o seu gatilho.

| Script | Finalidade |
|---|---|
| `01-criar-base-dados.sql` | Cria as 19 tabelas, as restrições, os índices, os 3 gatilhos e os valores predefinidos das listas fechadas (script T-SQL da secção 13.2 do modelo de dados) |
| `02-dados-iniciais-ficticios.sql` | Insere dados exclusivamente fictícios para desenvolvimento e demonstração (RNF-050): 4 contas, 3 fichas (uma própria), associações e convites |
| `03-repor-base-dados.sql` | Remove os gatilhos e todas as tabelas, incluindo o histórico de migrações do EF Core, deixando a base de dados vazia |
| `repor-base-dados.sh` | Executa 03, 01 e 02 por esta ordem com o `sqlcmd` |

## Dois caminhos para criar a base de dados

1. **Migrações do EF Core (caminho usado pela API e pelos testes).** A API aplica as migrações ao arrancar
   (`BaseDados:AplicarMigracoesNoArranque = true`) ou com `dotnet ef database update`. Depois, para ter dados de
   demonstração, executar só o `02-dados-iniciais-ficticios.sql`.
2. **Scripts SQL.** Numa base de dados vazia, executar `01` e `02` (ou `repor-base-dados.sh`). A API usa-se então com
   `BaseDados:AplicarMigracoesNoArranque = false`, porque o histórico de migrações não existe.

Os dois caminhos produzem o mesmo esquema. Diferenças apenas formais: as restrições `UQ_*` são criadas pelo EF Core
como índices únicos com o mesmo nome e as restrições de valor predefinido criadas pelo EF Core não têm nome explícito.

## Reposição local

```bash
export SQLCMDPASSWORD='<palavra-passe-local>'   # nunca versionar
./repor-base-dados.sh localhost,1433 DoseCerta sa
```

Para executar um script isolado com o `sqlcmd`, usar `-I` (os índices filtrados exigem `QUOTED_IDENTIFIER ON`, que o `sqlcmd`
não liga por omissão) e, no Windows, `-f 65001` (os scripts estão em UTF-8):

```bash
sqlcmd -S localhost,1433 -U sa -C -I -b -f 65001 -d DoseCerta -i 01-criar-base-dados.sql
```

## Contas de demonstração local

As 4 contas do `02` usam a palavra-passe fictícia indicada no cabeçalho do script, apenas para o ambiente local de cada
aluno. Os hashes seguem o formato do hasher de palavras-passe do backend (`PasswordHasherPbkdf2`, Issue I032):
`PBKDF2-SHA256$<iterações>$<salt>$<hash>` (base64).

## Migração inicial

A migração inicial (`src/DoseCerta.Infrastructure/Persistence/Migrations/<data>_Inicial.cs`) é código gerado pela
ferramenta do EF Core a partir do mapeamento. Foi gerada com:

```bash
cd backend
dotnet ef migrations add Inicial --project src/DoseCerta.Infrastructure --startup-project src/DoseCerta.Infrastructure --output-dir Persistence/Migrations
```

O EF Core não gera gatilhos. No ficheiro gerado foi acrescentado (e tem de ser acrescentado de novo se a migração inicial
for regenerada):

- no fim do método `Up`: `migrationBuilder.CriarGatilhosDoseCerta();`
- no início do método `Down`: `migrationBuilder.RemoverGatilhosDoseCerta();`

(com `using DoseCerta.Infrastructure.Persistence;`).

## Migração `RetirarTarefasRecorrentes` (I037)

Retira a F007 de uma base de dados já criada pela migração inicial, sem a alterar (modelo de dados v02, secção 15): remove
o gatilho `TR_ExecucaoTarefa_Historico` (acrescentado à mão no início do `Up`, porque o EF Core não gera gatilhos), as
tabelas `ExecucaoTarefa`, `TarefaDiaSemana`, `HorarioTarefa` e `TarefaRecorrente`, e recria `CK_Notificacao_Tipo` e
`CK_Notificacao_Referencia` sem `TarefaNaoConfirmada` e `ExecucaoTarefa`. O `Down` volta a criar as tabelas e, no fim, o
gatilho. Depois das duas migrações, a base de dados tem as 19 tabelas e os 3 gatilhos do `01`; os testes de integração
`MigracoesDeixamAsDezanoveTabelas` e `MigracoesDeixamOsTresGatilhos` verificam-no. Para obter o SQL gerado pelas migrações e compará-lo com o `01`:

```bash
dotnet ef migrations script --idempotent --project src/DoseCerta.Infrastructure --startup-project src/DoseCerta.Infrastructure --output artefactos/esquema-migracoes.sql
```
