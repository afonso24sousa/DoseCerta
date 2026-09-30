# DoseCerta — Backend

API em ASP.NET Core 8 + Entity Framework Core 8 sobre SQL Server 2022.

## Estrutura

| Caminho | Conteúdo |
|---|---|
| `src/DoseCerta.Api/Dominio/` | Entidades e regras de negócio (`Ficha`, `AssociacaoFicha`, categorias fechadas) |
| `src/DoseCerta.Api/Dados/` | `DoseCertaDbContext`, mapeamentos, valores das categorias fechadas e migrações |
| `src/DoseCerta.Api/Servicos/` | Casos de uso (`ServicoFichas`) e armazenamento de documentos |
| `src/DoseCerta.Api/Controllers/` | Endpoints REST |
| `tests/DoseCerta.Api.Tests/` | Testes xUnit (serviço sobre SQLite em memória e API via `WebApplicationFactory`) |
| `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `stylecop.json` | Configuração partilhada (Regulamento Interno, secção 11) |

## Pré-requisitos

- .NET 8 SDK
- SQL Server 2022 (Developer Edition) local, com autenticação Windows
- Ferramenta do EF Core: `dotnet tool install --global dotnet-ef --version "8.*"`

## Executar localmente

1. Confirmar a connection string `DoseCerta` em `src/DoseCerta.Api/appsettings.json`. Por omissão aponta para
   `Server=localhost` com autenticação Windows. Para outra instância (por exemplo `localhost\SQLEXPRESS`),
   criar `src/DoseCerta.Api/appsettings.Development.json`, que não é versionado, com a connection string correta.
2. Criar a base de dados:

   ```powershell
   dotnet ef database update --project src/DoseCerta.Api
   ```

   Em ambiente `Development`, a API também aplica as migrações pendentes ao arrancar e cria o Utilizador fictício
   de teste `11111111-1111-1111-1111-111111111111`.
3. Gerar um token de desenvolvimento para esse Utilizador. Até a I001 disponibilizar o login, usa-se `dotnet user-jwts`:

   ```powershell
   dotnet user-jwts create --project src/DoseCerta.Api --name 11111111-1111-1111-1111-111111111111
   ```

4. Arrancar a API e abrir o Swagger em `http://localhost:5209/swagger`. Usar o token no botão **Authorize**.

   ```powershell
   dotnet run --project src/DoseCerta.Api --launch-profile http
   ```

## Testes e qualidade

```powershell
dotnet build                                          # QG-01 e QG-05 (analisadores e StyleCop, avisos = erros)
dotnet test --settings coverage.runsettings --collect "XPlat Code Coverage"   # QG-02, QG-03 e QG-04
dotnet format DoseCerta.sln --verify-no-changes       # QG-05 (formatação)
```

Os testes locais usam SQLite em memória, que respeita os índices únicos, as restrições CHECK e as transações
do modelo. A validação contra SQL Server 2022 é feita na pipeline.

## Ficha própria (I003 / F003)

- `POST /api/fichas` (`multipart/form-data`) é o endpoint único de criação, partilhado com a F002.
  Com `paraOUtilizadorAutenticado=true` cria a ficha própria, e o Utilizador fica imediatamente Cuidador principal.
  Com `false` devolve `501` até a I002 implementar a ficha de terceiro.
- Uma ficha é própria quando `Fichas.UtilizadorProprietarioId` está preenchido. O índice único filtrado
  `UX_Fichas_UtilizadorProprietarioId` garante uma única ficha própria por Utilizador, mesmo com pedidos em simultâneo.
- O dono da ficha própria não pode ser despromovido nem removido. A regra é aplicada em `Ficha`
  (`AlterarPapel` e `RemoverAssociacao`) e verificada de novo em `DoseCertaDbContext.SaveChanges`, para que
  nenhum outro código a contorne.
- A criação é transacional. Cada documento só é associado depois de o ficheiro ser guardado. Se algum ficheiro
  falhar, a transação é revertida e os ficheiros já guardados são removidos.
- A eliminação consulta `IVerificadorDependenciasFicha`. Cada Issue que introduzir medicação, tarefas ou
  histórico deve acrescentar lá a sua verificação.

## Contrato com outras Issues

- **I001 (Utilizador e autenticação):** a entidade `Utilizador` tem, no mínimo, `Id` (`Guid`), `Nome` e `Email`
  (único). O token JWT transporta o `Utilizador.Id` na claim `sub`.
- **I002 (ficha de terceiro):** reutiliza a entidade `Ficha`, o endpoint `POST /api/fichas`, a validação e a
  gravação transacional com documentos (`ServicoFichas`). Falta acrescentar a fábrica `Ficha.CriarDeTerceiro`
  e o ramo `paraOUtilizadorAutenticado=false`.
- **F004 (convites e papéis):** associa pessoas através de `Ficha.AdicionarAssociacao` e altera ou remove
  associações só através de `Ficha.AlterarPapel` e `Ficha.RemoverAssociacao`.
