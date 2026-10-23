-- =====================================================================
-- DoseCerta (G07) - Base de dados e utilizador da API no ambiente de demonstração
-- Issue I040
--
-- Idempotente: corre em cada publicação, antes de arrancar a API, com o sa e o
-- sqlcmd dentro do contentor do SQL Server (ver publicar.sh). Cria a base de dados
-- DoseCerta, o login e o utilizador dosecerta_app, se ainda não existirem, e
-- acerta a palavra-passe do login com o valor atual do segredo (rotação).
-- As tabelas são criadas depois pelas migrações do EF Core, no arranque da API.
--
-- A palavra-passe chega na variável de scripting $(DOSECERTA_APP_PASSWORD), lida
-- pelo sqlcmd da variável de ambiente com o mesmo nome (nunca versionada).
-- =====================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID(N'DoseCerta') IS NULL
  CREATE DATABASE DoseCerta;
GO

IF SUSER_ID(N'dosecerta_app') IS NULL
  CREATE LOGIN dosecerta_app WITH PASSWORD = N'$(DOSECERTA_APP_PASSWORD)', CHECK_POLICY = ON;
ELSE
  ALTER LOGIN dosecerta_app WITH PASSWORD = N'$(DOSECERTA_APP_PASSWORD)';
GO

USE DoseCerta;
GO

IF DATABASE_PRINCIPAL_ID(N'dosecerta_app') IS NULL
  CREATE USER dosecerta_app FOR LOGIN dosecerta_app;
ELSE
  ALTER USER dosecerta_app WITH LOGIN = dosecerta_app;   -- volta a ligar o utilizador ao login, se tiver ficado órfão

-- db_owner porque a API aplica as migrações ao arrancar; não tem poderes sobre o servidor.
IF IS_ROLEMEMBER(N'db_owner', N'dosecerta_app') = 0
  ALTER ROLE db_owner ADD MEMBER dosecerta_app;
GO
