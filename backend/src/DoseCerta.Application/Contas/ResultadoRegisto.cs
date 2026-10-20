// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Contas;

/// <summary>
/// Resultado da criação de uma conta (OP-01).
/// </summary>
/// <param name="Utilizador">Conta criada.</param>
/// <param name="ConvitesPendentes">Número de convites pendentes dirigidos ao e-mail da conta (RF-044).</param>
public sealed record ResultadoRegisto(Utilizador Utilizador, int ConvitesPendentes);
