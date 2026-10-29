// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Emails;

/// <summary>
/// Notificação que ainda não tem e-mail registado, com o endereço atual do destinatário.
/// </summary>
/// <param name="Notificacao">Notificação criada.</param>
/// <param name="EnderecoDestino">E-mail da conta do destinatário.</param>
public sealed record NotificacaoSemEmail(Notificacao Notificacao, string EnderecoDestino);
