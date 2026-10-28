// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Compromissos;

/// <summary>
/// Dados de um compromisso submetidos ao registar (OP-41) ou ao editar (OP-43).
/// </summary>
/// <param name="DataHora">Data e hora, com o desvio; obrigatória.</param>
/// <param name="Local">Local; obrigatório.</param>
/// <param name="Motivo">Motivo; obrigatório.</param>
/// <param name="AntecedenciaAvisoMinutos">Antecedência do aviso, em minutos; por omissão 1440 (24 horas).</param>
public sealed record DadosCompromisso(
    DateTimeOffset? DataHora,
    string? Local,
    string? Motivo,
    int? AntecedenciaAvisoMinutos);
