// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Compromissos;

/// <summary>
/// Resposta de OP-41 a OP-43 com os dados de um compromisso.
/// </summary>
/// <param name="Id">Identificador do compromisso.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="DataHora">Data e hora, com o desvio de Lisboa.</param>
/// <param name="Local">Local.</param>
/// <param name="Motivo">Motivo.</param>
/// <param name="AntecedenciaAvisoMinutos">Antecedência do aviso, em minutos.</param>
public sealed record RespostaCompromisso(
    Guid Id,
    Guid FichaId,
    DateTimeOffset DataHora,
    string Local,
    string Motivo,
    int AntecedenciaAvisoMinutos);
