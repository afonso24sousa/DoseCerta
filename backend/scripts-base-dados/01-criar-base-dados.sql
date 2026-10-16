-- =====================================================================
-- DoseCerta (G07) - Script de criação da base de dados (SQL Server 2022)
-- Issue I030 | Modelo de dados m2-modelo-de-dados-dosecerta-v01.md, secção 13.2
--
-- Cria as 23 tabelas, as restrições, os índices e os 4 gatilhos do modelo de dados
-- e os valores predefinidos das listas fechadas (secção 8). Executar numa base de
-- dados vazia, por exemplo:
--   sqlcmd -S localhost,1433 -U sa -C -I -d DoseCerta -i 01-criar-base-dados.sql
-- Uma base de dados criada por este script é usada com a API com
-- BaseDados:AplicarMigracoesNoArranque = false (as migrações do EF Core são o
-- caminho alternativo e criam o mesmo esquema; ver README.md).
-- =====================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
-- Obrigatório para os índices filtrados (o sqlcmd usa QUOTED_IDENTIFIER OFF por omissão).
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE dbo.Utilizador (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_Utilizador_Id DEFAULT NEWSEQUENTIALID(),
  Nome nvarchar(120) NOT NULL,
  Email nvarchar(254) COLLATE Latin1_General_100_CI_AI NOT NULL,
  PalavraPasseHash nvarchar(255) NOT NULL,
  CriadoEmUtc datetime2(0) NOT NULL CONSTRAINT DF_Utilizador_CriadoEmUtc DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_Utilizador PRIMARY KEY (Id),
  CONSTRAINT UQ_Utilizador_Email UNIQUE (Email)
);

CREATE TABLE dbo.SessaoRevogada (
  Jti uniqueidentifier NOT NULL,
  UtilizadorId uniqueidentifier NOT NULL,
  RevogadaEmUtc datetime2(0) NOT NULL CONSTRAINT DF_SessaoRevogada_RevogadaEmUtc DEFAULT SYSUTCDATETIME(),
  ExpiraEmUtc datetime2(0) NOT NULL,
  CONSTRAINT PK_SessaoRevogada PRIMARY KEY (Jti),
  CONSTRAINT FK_SessaoRevogada_UtilizadorId FOREIGN KEY (UtilizadorId) REFERENCES dbo.Utilizador (Id) ON DELETE CASCADE,
  CONSTRAINT CK_SessaoRevogada_Validade CHECK (ExpiraEmUtc <= DATEADD(HOUR, 8, RevogadaEmUtc))
);
CREATE INDEX IX_SessaoRevogada_ExpiraEmUtc ON dbo.SessaoRevogada (ExpiraEmUtc);

CREATE TABLE dbo.CondicaoSaude (
  Id smallint NOT NULL,
  Designacao nvarchar(80) NOT NULL,
  CONSTRAINT PK_CondicaoSaude PRIMARY KEY (Id),
  CONSTRAINT UQ_CondicaoSaude_Designacao UNIQUE (Designacao)
);

CREATE TABLE dbo.Alergia (
  Id smallint NOT NULL,
  Designacao nvarchar(80) NOT NULL,
  CONSTRAINT PK_Alergia PRIMARY KEY (Id),
  CONSTRAINT UQ_Alergia_Designacao UNIQUE (Designacao)
);

CREATE TABLE dbo.GrauAutonomia (
  Id smallint NOT NULL,
  Designacao nvarchar(80) NOT NULL,
  Ordem tinyint NOT NULL,
  CONSTRAINT PK_GrauAutonomia PRIMARY KEY (Id),
  CONSTRAINT UQ_GrauAutonomia_Designacao UNIQUE (Designacao)
);

CREATE TABLE dbo.Ficha (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_Ficha_Id DEFAULT NEWSEQUENTIALID(),
  NomeCompleto nvarchar(150) NOT NULL,
  DataNascimento date NOT NULL,
  ContactoEmergencia nvarchar(30) NOT NULL,
  GrauAutonomiaId smallint NULL,
  Tipo nvarchar(10) NOT NULL,
  UtilizadorProprioId uniqueidentifier NULL,
  Estado nvarchar(10) NOT NULL CONSTRAINT DF_Ficha_Estado DEFAULT 'Ativa',
  CriadaPorId uniqueidentifier NOT NULL,
  CriadaEmUtc datetime2(0) NOT NULL CONSTRAINT DF_Ficha_CriadaEmUtc DEFAULT SYSUTCDATETIME(),
  AtivaDesdeUtc datetime2(0) NOT NULL CONSTRAINT DF_Ficha_AtivaDesdeUtc DEFAULT SYSUTCDATETIME(),
  ArquivadaEmUtc datetime2(0) NULL,
  CONSTRAINT PK_Ficha PRIMARY KEY (Id),
  CONSTRAINT FK_Ficha_GrauAutonomiaId FOREIGN KEY (GrauAutonomiaId) REFERENCES dbo.GrauAutonomia (Id),
  CONSTRAINT FK_Ficha_UtilizadorProprioId FOREIGN KEY (UtilizadorProprioId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT FK_Ficha_CriadaPorId FOREIGN KEY (CriadaPorId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT CK_Ficha_Estado CHECK (Estado IN ('Ativa','Arquivada')),
  CONSTRAINT CK_Ficha_Tipo CHECK ((Tipo = 'Propria' AND UtilizadorProprioId IS NOT NULL) OR (Tipo = 'Terceira' AND UtilizadorProprioId IS NULL)),
  CONSTRAINT CK_Ficha_Nascimento CHECK (DataNascimento >= '1900-01-01'),
  CONSTRAINT CK_Ficha_Arquivo CHECK ((Estado = 'Arquivada' AND ArquivadaEmUtc IS NOT NULL AND ArquivadaEmUtc >= AtivaDesdeUtc) OR (Estado = 'Ativa' AND ArquivadaEmUtc IS NULL))
);
CREATE UNIQUE INDEX UQ_Ficha_UtilizadorProprio ON dbo.Ficha (UtilizadorProprioId) WHERE UtilizadorProprioId IS NOT NULL;
CREATE INDEX IX_Ficha_Estado ON dbo.Ficha (Estado);

CREATE TABLE dbo.FichaCondicaoSaude (
  FichaId uniqueidentifier NOT NULL,
  CondicaoSaudeId smallint NOT NULL,
  CONSTRAINT PK_FichaCondicaoSaude PRIMARY KEY (FichaId, CondicaoSaudeId),
  CONSTRAINT FK_FichaCondicaoSaude_FichaId FOREIGN KEY (FichaId) REFERENCES dbo.Ficha (Id) ON DELETE CASCADE,
  CONSTRAINT FK_FichaCondicaoSaude_CondicaoSaudeId FOREIGN KEY (CondicaoSaudeId) REFERENCES dbo.CondicaoSaude (Id)
);

CREATE TABLE dbo.FichaAlergia (
  FichaId uniqueidentifier NOT NULL,
  AlergiaId smallint NOT NULL,
  CONSTRAINT PK_FichaAlergia PRIMARY KEY (FichaId, AlergiaId),
  CONSTRAINT FK_FichaAlergia_FichaId FOREIGN KEY (FichaId) REFERENCES dbo.Ficha (Id) ON DELETE CASCADE,
  CONSTRAINT FK_FichaAlergia_AlergiaId FOREIGN KEY (AlergiaId) REFERENCES dbo.Alergia (Id)
);

CREATE TABLE dbo.Associacao (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_Associacao_Id DEFAULT NEWSEQUENTIALID(),
  FichaId uniqueidentifier NOT NULL,
  UtilizadorId uniqueidentifier NOT NULL,
  Papel nvarchar(10) NOT NULL,
  EPrincipal bit NOT NULL CONSTRAINT DF_Associacao_EPrincipal DEFAULT 0,
  EProvisorio bit NOT NULL CONSTRAINT DF_Associacao_EProvisorio DEFAULT 0,
  CriadaEmUtc datetime2(0) NOT NULL CONSTRAINT DF_Associacao_CriadaEmUtc DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_Associacao PRIMARY KEY (Id),
  CONSTRAINT FK_Associacao_FichaId FOREIGN KEY (FichaId) REFERENCES dbo.Ficha (Id) ON DELETE CASCADE,
  CONSTRAINT FK_Associacao_UtilizadorId FOREIGN KEY (UtilizadorId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT CK_Associacao_Papel CHECK (Papel IN ('Cuidador','Familiar')),
  CONSTRAINT CK_Associacao_Principal CHECK (EPrincipal = 0 OR Papel = 'Cuidador'),
  CONSTRAINT CK_Associacao_Provisorio CHECK (EProvisorio = 0 OR EPrincipal = 1),
  CONSTRAINT UQ_Associacao_FichaUtilizador UNIQUE (FichaId, UtilizadorId)
);
CREATE UNIQUE INDEX UQ_Associacao_Principal ON dbo.Associacao (FichaId) WHERE EPrincipal = 1;
CREATE INDEX IX_Associacao_Utilizador ON dbo.Associacao (UtilizadorId, FichaId) INCLUDE (Papel);

CREATE TABLE dbo.Convite (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_Convite_Id DEFAULT NEWSEQUENTIALID(),
  FichaId uniqueidentifier NOT NULL,
  EmailConvidado nvarchar(254) COLLATE Latin1_General_100_CI_AI NOT NULL,
  PapelOferecido nvarchar(10) NOT NULL,
  ParaPrincipal bit NOT NULL CONSTRAINT DF_Convite_ParaPrincipal DEFAULT 0,
  ConvidadoPorId uniqueidentifier NOT NULL,
  Estado nvarchar(10) NOT NULL CONSTRAINT DF_Convite_Estado DEFAULT 'Pendente',
  UtilizadorConvidadoId uniqueidentifier NULL,
  CriadoEmUtc datetime2(0) NOT NULL CONSTRAINT DF_Convite_CriadoEmUtc DEFAULT SYSUTCDATETIME(),
  RespondidoEmUtc datetime2(0) NULL,
  CONSTRAINT PK_Convite PRIMARY KEY (Id),
  CONSTRAINT FK_Convite_FichaId FOREIGN KEY (FichaId) REFERENCES dbo.Ficha (Id) ON DELETE CASCADE,
  CONSTRAINT FK_Convite_ConvidadoPorId FOREIGN KEY (ConvidadoPorId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT FK_Convite_UtilizadorConvidadoId FOREIGN KEY (UtilizadorConvidadoId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT CK_Convite_Papel CHECK (PapelOferecido IN ('Cuidador','Familiar')),
  CONSTRAINT CK_Convite_Principal CHECK (ParaPrincipal = 0 OR PapelOferecido = 'Cuidador'),
  CONSTRAINT CK_Convite_Estado CHECK (Estado IN ('Pendente','Aceite','Recusado')),
  CONSTRAINT CK_Convite_Resposta CHECK ((Estado = 'Pendente' AND RespondidoEmUtc IS NULL AND UtilizadorConvidadoId IS NULL) OR (Estado <> 'Pendente' AND RespondidoEmUtc IS NOT NULL AND UtilizadorConvidadoId IS NOT NULL))
);
CREATE UNIQUE INDEX UQ_Convite_Pendente ON dbo.Convite (FichaId, EmailConvidado) WHERE Estado = 'Pendente';
CREATE INDEX IX_Convite_EmailEstado ON dbo.Convite (EmailConvidado, Estado);

CREATE TABLE dbo.Tratamento (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_Tratamento_Id DEFAULT NEWSEQUENTIALID(),
  FichaId uniqueidentifier NOT NULL,
  Medicamento nvarchar(120) NOT NULL,
  Dose nvarchar(60) NOT NULL,
  GrauImportancia nvarchar(10) NOT NULL,
  DataInicio date NOT NULL,
  DataFim date NULL,
  Estado nvarchar(10) NOT NULL CONSTRAINT DF_Tratamento_Estado DEFAULT 'EmCurso',
  TerminadoEmUtc datetime2(0) NULL,
  CriadoPorId uniqueidentifier NOT NULL,
  CriadoEmUtc datetime2(0) NOT NULL CONSTRAINT DF_Tratamento_CriadoEmUtc DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_Tratamento PRIMARY KEY (Id),
  CONSTRAINT FK_Tratamento_FichaId FOREIGN KEY (FichaId) REFERENCES dbo.Ficha (Id),
  CONSTRAINT FK_Tratamento_CriadoPorId FOREIGN KEY (CriadoPorId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT CK_Tratamento_Grau CHECK (GrauImportancia IN ('Leve','Moderado','Grave')),
  CONSTRAINT CK_Tratamento_Datas CHECK (DataFim IS NULL OR DataFim >= DataInicio),
  CONSTRAINT CK_Tratamento_Estado CHECK (Estado IN ('EmCurso','Terminado')),
  CONSTRAINT CK_Tratamento_Termino CHECK ((Estado = 'Terminado' AND TerminadoEmUtc IS NOT NULL) OR (Estado = 'EmCurso' AND TerminadoEmUtc IS NULL))
);
CREATE INDEX IX_Tratamento_FichaEstado ON dbo.Tratamento (FichaId, Estado);

CREATE TABLE dbo.HorarioToma (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_HorarioToma_Id DEFAULT NEWSEQUENTIALID(),
  TratamentoId uniqueidentifier NOT NULL,
  HoraLocal time(0) NOT NULL,
  CONSTRAINT PK_HorarioToma PRIMARY KEY (Id),
  CONSTRAINT FK_HorarioToma_TratamentoId FOREIGN KEY (TratamentoId) REFERENCES dbo.Tratamento (Id) ON DELETE CASCADE,
  CONSTRAINT UQ_HorarioToma_TratamentoHora UNIQUE (TratamentoId, HoraLocal)
);

CREATE TABLE dbo.Documento (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_Documento_Id DEFAULT NEWSEQUENTIALID(),
  FichaId uniqueidentifier NOT NULL,
  TratamentoId uniqueidentifier NULL,
  Categoria nvarchar(25) NOT NULL,
  NomeOriginal nvarchar(255) NOT NULL,
  CaminhoRelativo nvarchar(400) NOT NULL,
  Formato nvarchar(3) NOT NULL,
  TamanhoBytes int NOT NULL,
  CarregadoPorId uniqueidentifier NOT NULL,
  CarregadoEmUtc datetime2(0) NOT NULL CONSTRAINT DF_Documento_CarregadoEmUtc DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_Documento PRIMARY KEY (Id),
  CONSTRAINT FK_Documento_FichaId FOREIGN KEY (FichaId) REFERENCES dbo.Ficha (Id) ON DELETE CASCADE,
  CONSTRAINT FK_Documento_TratamentoId FOREIGN KEY (TratamentoId) REFERENCES dbo.Tratamento (Id),
  CONSTRAINT FK_Documento_CarregadoPorId FOREIGN KEY (CarregadoPorId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT CK_Documento_Categoria CHECK (Categoria IN ('ReceitaMedica','RelatorioClinico','DocumentoIdentificacao')),
  CONSTRAINT CK_Documento_Formato CHECK (Formato IN ('PDF','JPG','PNG')),
  CONSTRAINT CK_Documento_Tamanho CHECK (TamanhoBytes BETWEEN 1 AND 10485760),
  CONSTRAINT CK_Documento_Receita CHECK (TratamentoId IS NULL OR Categoria = 'ReceitaMedica'),
  CONSTRAINT UQ_Documento_Caminho UNIQUE (CaminhoRelativo)
);
CREATE INDEX IX_Documento_Ficha ON dbo.Documento (FichaId, Categoria);

CREATE TABLE dbo.Toma (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_Toma_Id DEFAULT NEWSEQUENTIALID(),
  TratamentoId uniqueidentifier NOT NULL,
  PrevistaEmUtc datetime2(0) NOT NULL,
  Medicamento nvarchar(120) NOT NULL,
  Dose nvarchar(60) NOT NULL,
  GrauImportancia nvarchar(10) NOT NULL,
  Estado nvarchar(20) NOT NULL CONSTRAINT DF_Toma_Estado DEFAULT 'Pendente',
  ConfirmadaPorId uniqueidentifier NULL,
  ConfirmadaEmUtc datetime2(0) NULL,
  NaoConfirmadaDetetadaEmUtc datetime2(0) NULL,
  CuidadoresNotificadosEmUtc datetime2(0) NULL,
  FamiliaresNotificadosEmUtc datetime2(0) NULL,
  Versao rowversion NOT NULL,
  CONSTRAINT PK_Toma PRIMARY KEY (Id),
  CONSTRAINT FK_Toma_TratamentoId FOREIGN KEY (TratamentoId) REFERENCES dbo.Tratamento (Id),
  CONSTRAINT FK_Toma_ConfirmadaPorId FOREIGN KEY (ConfirmadaPorId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT CK_Toma_Estado CHECK (Estado IN ('Pendente','Confirmada','ConfirmadaComAtraso','NaoConfirmada')),
  CONSTRAINT CK_Toma_Grau CHECK (GrauImportancia IN ('Leve','Moderado','Grave')),
  CONSTRAINT CK_Toma_Confirmacao CHECK ((Estado IN ('Confirmada','ConfirmadaComAtraso') AND ConfirmadaPorId IS NOT NULL AND ConfirmadaEmUtc IS NOT NULL AND NaoConfirmadaDetetadaEmUtc IS NULL) OR (Estado = 'Pendente' AND ConfirmadaPorId IS NULL AND ConfirmadaEmUtc IS NULL AND NaoConfirmadaDetetadaEmUtc IS NULL) OR (Estado = 'NaoConfirmada' AND ConfirmadaPorId IS NULL AND ConfirmadaEmUtc IS NULL AND NaoConfirmadaDetetadaEmUtc IS NOT NULL)),
  CONSTRAINT CK_Toma_Limite24h CHECK ((ConfirmadaEmUtc IS NULL OR ConfirmadaEmUtc < DATEADD(HOUR, 24, PrevistaEmUtc)) AND (NaoConfirmadaDetetadaEmUtc IS NULL OR NaoConfirmadaDetetadaEmUtc >= DATEADD(HOUR, 24, PrevistaEmUtc))),
  CONSTRAINT CK_Toma_Tolerancia CHECK ((Estado <> 'Confirmada' OR ConfirmadaEmUtc <= DATEADD(MINUTE, CASE GrauImportancia WHEN 'Leve' THEN 120 WHEN 'Moderado' THEN 60 ELSE 30 END, PrevistaEmUtc)) AND (Estado <> 'ConfirmadaComAtraso' OR ConfirmadaEmUtc > DATEADD(MINUTE, CASE GrauImportancia WHEN 'Leve' THEN 120 WHEN 'Moderado' THEN 60 ELSE 30 END, PrevistaEmUtc))),
  CONSTRAINT CK_Toma_Familiares CHECK (FamiliaresNotificadosEmUtc IS NULL OR GrauImportancia = 'Grave'),
  CONSTRAINT UQ_Toma_TratamentoInstante UNIQUE (TratamentoId, PrevistaEmUtc)
);
CREATE INDEX IX_Toma_PendentePrevista ON dbo.Toma (PrevistaEmUtc) WHERE Estado = 'Pendente';

CREATE TABLE dbo.TarefaRecorrente (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_TarefaRecorrente_Id DEFAULT NEWSEQUENTIALID(),
  FichaId uniqueidentifier NOT NULL,
  Designacao nvarchar(200) NOT NULL,
  PeriodicidadeTipo nvarchar(10) NOT NULL,
  IntervaloDias tinyint NULL,
  DataInicio date NOT NULL,
  ToleranciaMinutos smallint NOT NULL,
  Ativa bit NOT NULL CONSTRAINT DF_TarefaRecorrente_Ativa DEFAULT 1,
  CriadaPorId uniqueidentifier NOT NULL,
  CriadaEmUtc datetime2(0) NOT NULL CONSTRAINT DF_TarefaRecorrente_CriadaEmUtc DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_TarefaRecorrente PRIMARY KEY (Id),
  CONSTRAINT FK_TarefaRecorrente_FichaId FOREIGN KEY (FichaId) REFERENCES dbo.Ficha (Id),
  CONSTRAINT FK_TarefaRecorrente_CriadaPorId FOREIGN KEY (CriadaPorId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT CK_Tarefa_Periodicidade CHECK (PeriodicidadeTipo IN ('Diaria','DiasSemana','CadaNDias')),
  CONSTRAINT CK_Tarefa_Intervalo CHECK ((PeriodicidadeTipo = 'CadaNDias' AND IntervaloDias BETWEEN 2 AND 30) OR (PeriodicidadeTipo <> 'CadaNDias' AND IntervaloDias IS NULL)),
  CONSTRAINT CK_Tarefa_Tolerancia CHECK (ToleranciaMinutos BETWEEN 15 AND 1440)
);
CREATE INDEX IX_Tarefa_FichaAtiva ON dbo.TarefaRecorrente (FichaId, Ativa);

CREATE TABLE dbo.HorarioTarefa (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_HorarioTarefa_Id DEFAULT NEWSEQUENTIALID(),
  TarefaId uniqueidentifier NOT NULL,
  HoraLocal time(0) NOT NULL,
  CONSTRAINT PK_HorarioTarefa PRIMARY KEY (Id),
  CONSTRAINT FK_HorarioTarefa_TarefaId FOREIGN KEY (TarefaId) REFERENCES dbo.TarefaRecorrente (Id) ON DELETE CASCADE,
  CONSTRAINT UQ_HorarioTarefa_TarefaHora UNIQUE (TarefaId, HoraLocal)
);

CREATE TABLE dbo.TarefaDiaSemana (
  TarefaId uniqueidentifier NOT NULL,
  DiaSemana tinyint NOT NULL,
  CONSTRAINT PK_TarefaDiaSemana PRIMARY KEY (TarefaId, DiaSemana),
  CONSTRAINT FK_TarefaDiaSemana_TarefaId FOREIGN KEY (TarefaId) REFERENCES dbo.TarefaRecorrente (Id) ON DELETE CASCADE,
  CONSTRAINT CK_TarefaDiaSemana_Dia CHECK (DiaSemana BETWEEN 1 AND 7)
);

CREATE TABLE dbo.ExecucaoTarefa (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_ExecucaoTarefa_Id DEFAULT NEWSEQUENTIALID(),
  TarefaId uniqueidentifier NOT NULL,
  PrevistaEmUtc datetime2(0) NOT NULL,
  Designacao nvarchar(200) NOT NULL,
  ToleranciaMinutos smallint NOT NULL,
  Estado nvarchar(12) NOT NULL CONSTRAINT DF_ExecucaoTarefa_Estado DEFAULT 'PorConfirmar',
  ConfirmadaPorId uniqueidentifier NULL,
  ConfirmadaEmUtc datetime2(0) NULL,
  CuidadoresNotificadosEmUtc datetime2(0) NULL,
  Versao rowversion NOT NULL,
  CONSTRAINT PK_ExecucaoTarefa PRIMARY KEY (Id),
  CONSTRAINT FK_ExecucaoTarefa_TarefaId FOREIGN KEY (TarefaId) REFERENCES dbo.TarefaRecorrente (Id),
  CONSTRAINT FK_ExecucaoTarefa_ConfirmadaPorId FOREIGN KEY (ConfirmadaPorId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT CK_ExecucaoTarefa_Estado CHECK (Estado IN ('PorConfirmar','Realizada')),
  CONSTRAINT CK_ExecucaoTarefa_Confirmacao CHECK ((Estado = 'Realizada' AND ConfirmadaPorId IS NOT NULL AND ConfirmadaEmUtc IS NOT NULL) OR (Estado = 'PorConfirmar' AND ConfirmadaPorId IS NULL AND ConfirmadaEmUtc IS NULL)),
  CONSTRAINT CK_ExecucaoTarefa_Tolerancia CHECK (ToleranciaMinutos BETWEEN 15 AND 1440),
  CONSTRAINT UQ_ExecucaoTarefa_TarefaInstante UNIQUE (TarefaId, PrevistaEmUtc)
);
CREATE INDEX IX_ExecucaoTarefa_PorConfirmar ON dbo.ExecucaoTarefa (PrevistaEmUtc) WHERE Estado = 'PorConfirmar' AND CuidadoresNotificadosEmUtc IS NULL;

CREATE TABLE dbo.Compromisso (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_Compromisso_Id DEFAULT NEWSEQUENTIALID(),
  FichaId uniqueidentifier NOT NULL,
  DataHoraUtc datetime2(0) NOT NULL,
  Local nvarchar(150) NOT NULL,
  Motivo nvarchar(250) NOT NULL,
  AntecedenciaAvisoMin int NOT NULL CONSTRAINT DF_Compromisso_AntecedenciaAvisoMin DEFAULT 1440,
  AvisoEnviadoEmUtc datetime2(0) NULL,
  CriadoPorId uniqueidentifier NOT NULL,
  CriadoEmUtc datetime2(0) NOT NULL CONSTRAINT DF_Compromisso_CriadoEmUtc DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_Compromisso PRIMARY KEY (Id),
  CONSTRAINT FK_Compromisso_FichaId FOREIGN KEY (FichaId) REFERENCES dbo.Ficha (Id) ON DELETE CASCADE,
  CONSTRAINT FK_Compromisso_CriadoPorId FOREIGN KEY (CriadoPorId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT CK_Compromisso_Antecedencia CHECK (AntecedenciaAvisoMin IN (30, 60, 120, 1440, 2880))
);
CREATE INDEX IX_Compromisso_FichaData ON dbo.Compromisso (FichaId, DataHoraUtc);
CREATE INDEX IX_Compromisso_AvisoPendente ON dbo.Compromisso (DataHoraUtc) WHERE AvisoEnviadoEmUtc IS NULL;

CREATE TABLE dbo.Ocorrencia (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_Ocorrencia_Id DEFAULT NEWSEQUENTIALID(),
  FichaId uniqueidentifier NOT NULL,
  Descricao nvarchar(2000) NOT NULL,
  OcorreuEmUtc datetime2(0) NOT NULL,
  AutorId uniqueidentifier NOT NULL,
  RegistadaEmUtc datetime2(0) NOT NULL CONSTRAINT DF_Ocorrencia_RegistadaEmUtc DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_Ocorrencia PRIMARY KEY (Id),
  CONSTRAINT FK_Ocorrencia_FichaId FOREIGN KEY (FichaId) REFERENCES dbo.Ficha (Id),
  CONSTRAINT FK_Ocorrencia_AutorId FOREIGN KEY (AutorId) REFERENCES dbo.Utilizador (Id)
);
CREATE INDEX IX_Ocorrencia_FichaRegisto ON dbo.Ocorrencia (FichaId, RegistadaEmUtc DESC);

CREATE TABLE dbo.Nota (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_Nota_Id DEFAULT NEWSEQUENTIALID(),
  FichaId uniqueidentifier NOT NULL,
  Texto nvarchar(2000) NOT NULL,
  AutorId uniqueidentifier NOT NULL,
  RegistadaEmUtc datetime2(0) NOT NULL CONSTRAINT DF_Nota_RegistadaEmUtc DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_Nota PRIMARY KEY (Id),
  CONSTRAINT FK_Nota_FichaId FOREIGN KEY (FichaId) REFERENCES dbo.Ficha (Id),
  CONSTRAINT FK_Nota_AutorId FOREIGN KEY (AutorId) REFERENCES dbo.Utilizador (Id)
);
CREATE INDEX IX_Nota_FichaRegisto ON dbo.Nota (FichaId, RegistadaEmUtc DESC);

CREATE TABLE dbo.Notificacao (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_Notificacao_Id DEFAULT NEWSEQUENTIALID(),
  DestinatarioId uniqueidentifier NOT NULL,
  FichaId uniqueidentifier NOT NULL,
  Tipo nvarchar(25) NOT NULL,
  Mensagem nvarchar(400) NOT NULL,
  ReferenciaTipo nvarchar(20) NOT NULL,
  ReferenciaId uniqueidentifier NOT NULL,
  InstanteLimiteUtc datetime2(0) NOT NULL,
  CriadaEmUtc datetime2(0) NOT NULL CONSTRAINT DF_Notificacao_CriadaEmUtc DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_Notificacao PRIMARY KEY (Id),
  CONSTRAINT FK_Notificacao_DestinatarioId FOREIGN KEY (DestinatarioId) REFERENCES dbo.Utilizador (Id),
  CONSTRAINT FK_Notificacao_FichaId FOREIGN KEY (FichaId) REFERENCES dbo.Ficha (Id),
  CONSTRAINT CK_Notificacao_Tipo CHECK (Tipo IN ('TomaEmAtraso','TomaGraveFamiliares','TarefaNaoConfirmada','NovaOcorrencia','ProximoCompromisso','ConviteRecebido')),
  CONSTRAINT CK_Notificacao_Referencia CHECK ((Tipo IN ('TomaEmAtraso','TomaGraveFamiliares') AND ReferenciaTipo = 'Toma') OR (Tipo = 'TarefaNaoConfirmada' AND ReferenciaTipo = 'ExecucaoTarefa') OR (Tipo = 'NovaOcorrencia' AND ReferenciaTipo = 'Ocorrencia') OR (Tipo = 'ProximoCompromisso' AND ReferenciaTipo = 'Compromisso') OR (Tipo = 'ConviteRecebido' AND ReferenciaTipo = 'Convite')),
  CONSTRAINT UQ_Notificacao_Origem UNIQUE (DestinatarioId, Tipo, ReferenciaId, InstanteLimiteUtc)
);
CREATE INDEX IX_Notificacao_DestinatarioData ON dbo.Notificacao (DestinatarioId, CriadaEmUtc DESC);

CREATE TABLE dbo.EnvioEmail (
  Id uniqueidentifier NOT NULL CONSTRAINT DF_EnvioEmail_Id DEFAULT NEWSEQUENTIALID(),
  NotificacaoId uniqueidentifier NULL,
  ConviteId uniqueidentifier NULL,
  EnderecoDestino nvarchar(254) NOT NULL,
  Assunto nvarchar(150) NOT NULL,
  Estado nvarchar(12) NOT NULL CONSTRAINT DF_EnvioEmail_Estado DEFAULT 'Pendente',
  Reenvios tinyint NOT NULL CONSTRAINT DF_EnvioEmail_Reenvios DEFAULT 0,
  CriadoEmUtc datetime2(0) NOT NULL CONSTRAINT DF_EnvioEmail_CriadoEmUtc DEFAULT SYSUTCDATETIME(),
  PrimeiraTentativaEmUtc datetime2(0) NULL,
  UltimaTentativaEmUtc datetime2(0) NULL,
  ProximaTentativaEmUtc datetime2(0) NULL,
  CONSTRAINT PK_EnvioEmail PRIMARY KEY (Id),
  CONSTRAINT FK_EnvioEmail_NotificacaoId FOREIGN KEY (NotificacaoId) REFERENCES dbo.Notificacao (Id) ON DELETE CASCADE,
  CONSTRAINT FK_EnvioEmail_ConviteId FOREIGN KEY (ConviteId) REFERENCES dbo.Convite (Id) ON DELETE CASCADE,
  CONSTRAINT CK_EnvioEmail_Estado CHECK (Estado IN ('Pendente','Enviado','NaoEntregue')),
  CONSTRAINT CK_EnvioEmail_Reenvios CHECK (Reenvios BETWEEN 0 AND 3),
  CONSTRAINT CK_EnvioEmail_Origem CHECK (NotificacaoId IS NOT NULL OR ConviteId IS NOT NULL),
  CONSTRAINT CK_EnvioEmail_Coerencia CHECK ((Estado = 'Pendente' AND ProximaTentativaEmUtc IS NOT NULL) OR (Estado = 'Enviado' AND ProximaTentativaEmUtc IS NULL AND UltimaTentativaEmUtc IS NOT NULL) OR (Estado = 'NaoEntregue' AND ProximaTentativaEmUtc IS NULL AND Reenvios = 3))
);
CREATE UNIQUE INDEX UQ_EnvioEmail_Notificacao ON dbo.EnvioEmail (NotificacaoId) WHERE NotificacaoId IS NOT NULL;
CREATE UNIQUE INDEX UQ_EnvioEmail_Convite ON dbo.EnvioEmail (ConviteId) WHERE ConviteId IS NOT NULL;
CREATE INDEX IX_EnvioEmail_PendenteProxima ON dbo.EnvioEmail (ProximaTentativaEmUtc) WHERE Estado = 'Pendente';
GO

CREATE TRIGGER dbo.TR_Toma_Historico ON dbo.Toma
AFTER UPDATE, DELETE
AS
BEGIN
  SET NOCOUNT ON;
  IF EXISTS (SELECT 1 FROM deleted WHERE Estado <> 'Pendente')
  BEGIN
    ROLLBACK TRANSACTION;
    THROW 51001, N'Uma toma confirmada ou não confirmada faz parte do histórico e não pode ser alterada nem eliminada.', 1;
  END;
END;
GO

CREATE TRIGGER dbo.TR_ExecucaoTarefa_Historico ON dbo.ExecucaoTarefa
AFTER UPDATE, DELETE
AS
BEGIN
  SET NOCOUNT ON;
  IF EXISTS (SELECT 1 FROM deleted WHERE Estado <> 'PorConfirmar')
  BEGIN
    ROLLBACK TRANSACTION;
    THROW 51002, N'Uma execução realizada faz parte do histórico e não pode ser alterada nem eliminada.', 1;
  END;
END;
GO

CREATE TRIGGER dbo.TR_Ocorrencia_Imutavel ON dbo.Ocorrencia
INSTEAD OF UPDATE, DELETE
AS
BEGIN
  SET NOCOUNT ON;
  THROW 51003, N'As ocorrências não podem ser alteradas nem eliminadas.', 1;
END;
GO

CREATE TRIGGER dbo.TR_Nota_Imutavel ON dbo.Nota
INSTEAD OF UPDATE, DELETE
AS
BEGIN
  SET NOCOUNT ON;
  THROW 51004, N'As notas não podem ser alteradas nem eliminadas.', 1;
END;
GO

-- ---------------------------------------------------------------------
-- Valores predefinidos das listas fechadas (modelo de dados, secção 8)
-- ---------------------------------------------------------------------
INSERT INTO dbo.CondicaoSaude (Id, Designacao) VALUES
  (1, N'Diabetes'),
  (2, N'Hipertensão arterial'),
  (3, N'Doença cardíaca'),
  (4, N'Doença respiratória crónica'),
  (5, N'Demência ou doença de Alzheimer'),
  (6, N'Doença de Parkinson'),
  (7, N'Epilepsia'),
  (8, N'Insuficiência renal'),
  (9, N'Outra condição relevante');

INSERT INTO dbo.Alergia (Id, Designacao) VALUES
  (1, N'Medicamentos'),
  (2, N'Alimentos'),
  (3, N'Látex'),
  (4, N'Picadas de insetos'),
  (5, N'Pólen ou ácaros'),
  (6, N'Outra alergia');

INSERT INTO dbo.GrauAutonomia (Id, Designacao, Ordem) VALUES
  (1, N'Autónomo', 1),
  (2, N'Autonomia parcial', 2),
  (3, N'Dependente', 3),
  (4, N'Acamado', 4);
GO
