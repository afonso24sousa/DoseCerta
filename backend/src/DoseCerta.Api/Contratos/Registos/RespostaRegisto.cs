// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Api.Contratos.Registos;

/// <summary>
/// Resposta de OP-46 a OP-49 com uma ocorrência ou uma nota.
/// </summary>
/// <param name="Id">Identificador do registo.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="Tipo">Tipo do registo: <c>Ocorrencia</c> ou <c>Nota</c>.</param>
/// <param name="Descricao">Descrição da ocorrência ou texto da nota.</param>
/// <param name="Autor">Cuidador que fez o registo.</param>
/// <param name="OcorridaEm">Instante em que a ocorrência aconteceu, com o desvio de Lisboa; <see langword="null"/> nas notas.</param>
/// <param name="CriadaEm">Instante do registo, com o desvio de Lisboa.</param>
public sealed record RespostaRegisto(
    Guid Id,
    Guid FichaId,
    TipoRegisto Tipo,
    string Descricao,
    RespostaAutor Autor,
    DateTimeOffset? OcorridaEm,
    DateTimeOffset CriadaEm);
