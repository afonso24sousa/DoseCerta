-- =====================================================================
-- DoseCerta (G07) - Dados iniciais fictícios (RNF-050; Regulamento Interno do Grupo, secção 14.3)
-- Issue I030 | Executar depois de 01-criar-base-dados.sql (ou das migrações do EF Core).
--
-- Todos os nomes, e-mails, datas e contactos são inventados. Os identificadores
-- coincidem com os exemplos da Documentação da API (m2-documentacao-api-v01.md).
-- Contas de demonstração local: palavra-passe "Demo2026x" (fictícia, só para o ambiente
-- local de cada aluno). As credenciais da instância de avaliação são outras e ficam
-- no ficheiro de credenciais de avaliação de m3.
-- O hash segue o formato do PasswordHasherPbkdf2 (Issue I032): PBKDF2-SHA256$iterações$salt$hash (base64).
-- =====================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
-- Obrigatório para os índices filtrados (o sqlcmd usa QUOTED_IDENTIFIER OFF por omissão).
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

-- Contas (F001)
INSERT INTO dbo.Utilizador (Id, Nome, Email, PalavraPasseHash, CriadoEmUtc) VALUES
  ('00000000-0000-4000-8000-000000000011', N'Ana Fictícia', N'ana@exemplo.pt', N'PBKDF2-SHA256$100000$Ea0oZw/bbnMr2HlANteVpw==$HAPgoUz0LOBxLJfHew2dG1zurKaZvCIgIlhk28N7yBI=', '2026-10-01T09:00:00'),
  ('00000000-0000-4000-8000-000000000012', N'Rui Fictício', N'rui@exemplo.pt', N'PBKDF2-SHA256$100000$BOtztfYZ11XFIUXswigbdw==$v2UKD7TtsOQNgZSc1WAQDQGv2mgvshDMN9YG3NOHOwo=', '2026-10-01T09:00:00'),
  ('00000000-0000-4000-8000-000000000013', N'Joana Fictícia', N'joana@exemplo.pt', N'PBKDF2-SHA256$100000$DnZw8j/e5wS+r2me274KqQ==$g78pF13Nt8IIIkQg0ZcaAlmoyQuRNzd+aKxjN9ubumw=', '2026-10-01T09:00:00'),
  ('00000000-0000-4000-8000-000000000014', N'Carlos Fictício', N'carlos@exemplo.pt', N'PBKDF2-SHA256$100000$VRT9p7AuWGnwMSplcV3PSw==$eiyKcG1KjR16gHqt8wEPPx6ullYwwVUzlbzOQfLqkeQ=', '2026-10-01T09:00:00');

-- Fichas (F002 e F003)
-- 0101: ficha de terceiro criada pela Ana, que é a Cuidadora principal; o Rui é Familiar.
-- 0102: ficha própria da Ana.
-- 0103: ficha de terceiro criada pela Joana, com convite pendente para Cuidador principal (Joana provisória).
INSERT INTO dbo.Ficha (Id, NomeCompleto, DataNascimento, ContactoEmergencia, GrauAutonomiaId, Tipo, UtilizadorProprioId, Estado, CriadaPorId, CriadaEmUtc, AtivaDesdeUtc) VALUES
  ('00000000-0000-4000-8000-000000000101', N'Maria Fictícia Santos', '1941-05-17', N'+351 910 000 001', 2, 'Terceira', NULL, 'Ativa', '00000000-0000-4000-8000-000000000011', '2026-10-01T09:10:00', '2026-10-01T09:10:00'),
  ('00000000-0000-4000-8000-000000000102', N'Ana Fictícia', '1985-03-02', N'+351 910 000 002', 1, 'Propria', '00000000-0000-4000-8000-000000000011', 'Ativa', '00000000-0000-4000-8000-000000000011', '2026-10-01T09:20:00', '2026-10-01T09:20:00'),
  ('00000000-0000-4000-8000-000000000103', N'Alberto Fictício Costa', '1938-11-23', N'+351 910 000 003', 3, 'Terceira', NULL, 'Ativa', '00000000-0000-4000-8000-000000000013', '2026-10-01T09:30:00', '2026-10-01T09:30:00');

INSERT INTO dbo.FichaCondicaoSaude (FichaId, CondicaoSaudeId) VALUES
  ('00000000-0000-4000-8000-000000000101', 1),
  ('00000000-0000-4000-8000-000000000101', 2),
  ('00000000-0000-4000-8000-000000000103', 5);

INSERT INTO dbo.FichaAlergia (FichaId, AlergiaId) VALUES
  ('00000000-0000-4000-8000-000000000101', 1),
  ('00000000-0000-4000-8000-000000000103', 3);

-- Associações (F004)
INSERT INTO dbo.Associacao (Id, FichaId, UtilizadorId, Papel, EPrincipal, EProvisorio, CriadaEmUtc) VALUES
  ('00000000-0000-4000-8000-000000000201', '00000000-0000-4000-8000-000000000101', '00000000-0000-4000-8000-000000000011', 'Cuidador', 1, 0, '2026-10-01T09:10:00'),
  ('00000000-0000-4000-8000-000000000202', '00000000-0000-4000-8000-000000000101', '00000000-0000-4000-8000-000000000012', 'Familiar', 0, 0, '2026-10-01T10:00:00'),
  ('00000000-0000-4000-8000-000000000203', '00000000-0000-4000-8000-000000000102', '00000000-0000-4000-8000-000000000011', 'Cuidador', 1, 0, '2026-10-01T09:20:00'),
  ('00000000-0000-4000-8000-000000000204', '00000000-0000-4000-8000-000000000103', '00000000-0000-4000-8000-000000000013', 'Cuidador', 1, 1, '2026-10-01T09:30:00');

-- Convites (F004)
-- 0801: convite pendente da Joana para o Carlos ser Cuidador principal da ficha 0103.
-- 0802: convite pendente para um e-mail ainda sem conta (RF-044).
-- 0803: convite já aceite pelo Rui (histórico da associação 0202).
INSERT INTO dbo.Convite (Id, FichaId, EmailConvidado, PapelOferecido, ParaPrincipal, ConvidadoPorId, Estado, UtilizadorConvidadoId, CriadoEmUtc, RespondidoEmUtc) VALUES
  ('00000000-0000-4000-8000-000000000801', '00000000-0000-4000-8000-000000000103', N'carlos@exemplo.pt', 'Cuidador', 1, '00000000-0000-4000-8000-000000000013', 'Pendente', NULL, '2026-10-01T09:30:00', NULL),
  ('00000000-0000-4000-8000-000000000802', '00000000-0000-4000-8000-000000000101', N'joao@exemplo.pt', 'Familiar', 0, '00000000-0000-4000-8000-000000000011', 'Pendente', NULL, '2026-10-01T09:40:00', NULL),
  ('00000000-0000-4000-8000-000000000803', '00000000-0000-4000-8000-000000000101', N'rui@exemplo.pt', 'Familiar', 0, '00000000-0000-4000-8000-000000000011', 'Aceite', '00000000-0000-4000-8000-000000000012', '2026-10-01T09:45:00', '2026-10-01T10:00:00');

COMMIT TRANSACTION;
GO
