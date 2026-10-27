// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Ocorrencias;

/// <summary>
/// Ocorrência ou nota com o nome do autor, como é devolvida no registo e na consulta (OP-46 a OP-49, RF-092).
/// </summary>
/// <param name="Id">Identificador do registo.</param>
/// <param name="FichaId">Identificador da ficha.</param>
/// <param name="Tipo">Tipo do registo: ocorrência ou nota.</param>
/// <param name="Descricao">Descrição da ocorrência ou texto da nota.</param>
/// <param name="AutorId">Cuidador que fez o registo.</param>
/// <param name="NomeAutor">Nome da conta do autor.</param>
/// <param name="OcorreuEmUtc">Instante em que a ocorrência aconteceu, em UTC; <see langword="null"/> nas notas.</param>
/// <param name="RegistadaEmUtc">Instante do registo, em UTC.</param>
public sealed record RegistoPartilhado(
    Guid Id,
    Guid FichaId,
    TipoRegisto Tipo,
    string Descricao,
    Guid AutorId,
    string NomeAutor,
    DateTime? OcorreuEmUtc,
    DateTime RegistadaEmUtc);
