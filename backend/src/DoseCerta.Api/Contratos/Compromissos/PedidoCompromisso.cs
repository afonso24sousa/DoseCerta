// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Compromissos;

/// <summary>
/// Corpo de OP-41 (registar compromisso) e de OP-43 (editar compromisso).
/// </summary>
/// <param name="DataHora">Data e hora, em ISO 8601 com o desvio, por exemplo <c>2026-10-12T10:30:00+01:00</c>.</param>
/// <param name="Local">Local.</param>
/// <param name="Motivo">Motivo.</param>
/// <param name="AntecedenciaAvisoMinutos">Antecedência do aviso: 30, 60, 120, 1440 ou 2880 minutos; por omissão 1440.</param>
public sealed record PedidoCompromisso(
    DateTimeOffset? DataHora,
    string? Local,
    string? Motivo,
    int? AntecedenciaAvisoMinutos);
