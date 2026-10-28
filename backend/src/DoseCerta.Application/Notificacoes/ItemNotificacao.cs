// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Notificacoes;

/// <summary>
/// Notificação do histórico de um Utilizador (OP-51), com a ficha e, nas notificações de toma, o medicamento e a hora prevista.
/// </summary>
/// <param name="Id">Identificador da notificação.</param>
/// <param name="Tipo">Tipo da notificação.</param>
/// <param name="Mensagem">Mensagem guardada na criação.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="NomeFicha">Nome completo do utente da ficha.</param>
/// <param name="Medicamento">Medicamento da toma; <see langword="null"/> se a notificação não for de uma toma.</param>
/// <param name="PrevistaEmUtc">Instante previsto da toma, em UTC; <see langword="null"/> se a notificação não for de uma toma.</param>
/// <param name="CriadaEmUtc">Instante de criação, em UTC.</param>
/// <param name="Email">Estado do e-mail: <c>Pendente</c> enquanto não foi enviado, incluindo antes de ser registado (RF-118, RF-119).</param>
public sealed record ItemNotificacao(
    Guid Id,
    TipoNotificacao Tipo,
    string Mensagem,
    Guid FichaId,
    string NomeFicha,
    string? Medicamento,
    DateTime? PrevistaEmUtc,
    DateTime CriadaEmUtc,
    EstadoEnvioEmail Email);
