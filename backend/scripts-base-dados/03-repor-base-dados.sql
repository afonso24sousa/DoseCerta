-- =====================================================================
-- DoseCerta (G07) - Script de reposição da base de dados (SQL Server 2022)
-- Issue I030
--
-- Remove os gatilhos e todas as tabelas do DoseCerta (incluindo o histórico de
-- migrações do EF Core, se existir), deixando a base de dados vazia. Para repor o
-- estado inicial de demonstração, executar a seguir 01-criar-base-dados.sql e
-- 02-dados-iniciais-ficticios.sql (o script repor-base-dados.sh faz os três passos).
-- ATENÇÃO: apaga todos os dados da base de dados indicada. Usar só em ambientes locais.
-- =====================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DROP TRIGGER IF EXISTS dbo.TR_Toma_Historico;
DROP TRIGGER IF EXISTS dbo.TR_ExecucaoTarefa_Historico;
DROP TRIGGER IF EXISTS dbo.TR_Ocorrencia_Imutavel;
DROP TRIGGER IF EXISTS dbo.TR_Nota_Imutavel;
GO

-- Ordem inversa das dependências (chaves estrangeiras).
DROP TABLE IF EXISTS dbo.EnvioEmail;
DROP TABLE IF EXISTS dbo.Notificacao;
DROP TABLE IF EXISTS dbo.Nota;
DROP TABLE IF EXISTS dbo.Ocorrencia;
DROP TABLE IF EXISTS dbo.Compromisso;
DROP TABLE IF EXISTS dbo.ExecucaoTarefa;
DROP TABLE IF EXISTS dbo.TarefaDiaSemana;
DROP TABLE IF EXISTS dbo.HorarioTarefa;
DROP TABLE IF EXISTS dbo.TarefaRecorrente;
DROP TABLE IF EXISTS dbo.Toma;
DROP TABLE IF EXISTS dbo.Documento;
DROP TABLE IF EXISTS dbo.HorarioToma;
DROP TABLE IF EXISTS dbo.Tratamento;
DROP TABLE IF EXISTS dbo.Convite;
DROP TABLE IF EXISTS dbo.Associacao;
DROP TABLE IF EXISTS dbo.FichaAlergia;
DROP TABLE IF EXISTS dbo.FichaCondicaoSaude;
DROP TABLE IF EXISTS dbo.Ficha;
DROP TABLE IF EXISTS dbo.GrauAutonomia;
DROP TABLE IF EXISTS dbo.Alergia;
DROP TABLE IF EXISTS dbo.CondicaoSaude;
DROP TABLE IF EXISTS dbo.SessaoRevogada;
DROP TABLE IF EXISTS dbo.Utilizador;
DROP TABLE IF EXISTS dbo.__EFMigrationsHistory;
GO
