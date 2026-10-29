// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Notificacoes;

/// <summary>
/// Resposta de OP-51 com uma notificação do histórico do Utilizador.
/// </summary>
/// <param name="Id">Identificador da notificação.</param>
/// <param name="Tipo">Tipo da notificação.</param>
/// <param name="Mensagem">Mensagem em português europeu.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome completo do utente da ficha.</param>
/// <param name="Medicamento">Medicamento da toma; <see langword="null"/> se a notificação não for de uma toma.</param>
/// <param name="PrevistaEm">Hora prevista da toma, com o desvio de Lisboa; <see langword="null"/> se a notificação não for de uma toma.</param>
/// <param name="CriadaEm">Instante da notificação, com o desvio de Lisboa.</param>
/// <param name="Email">Estado do e-mail: <c>Enviado</c>, <c>Pendente</c> ou <c>NaoEntregue</c> (RF-118, RF-119).</param>
public sealed record RespostaNotificacao(
    Guid Id,
    TipoNotificacao Tipo,
    string Mensagem,
    Guid FichaId,
    string NomeFicha,
    string? Medicamento,
    DateTimeOffset? PrevistaEm,
    DateTimeOffset CriadaEm,
    EstadoEnvioEmail Email);
