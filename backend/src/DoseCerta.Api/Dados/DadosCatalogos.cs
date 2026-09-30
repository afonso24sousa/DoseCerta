// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;

namespace DoseCerta.Api.Dados;

/// <summary>
/// Valores das categorias fechadas, carregados pela migração.
/// </summary>
/// <remarks>
/// Lista proposta na I003, a confirmar com a I002 (F002). Acrescentar valores exige uma nova migração;
/// os identificadores existentes não devem ser alterados.
/// </remarks>
public static class DadosCatalogos
{
    /// <summary>
    /// Gets as condições de saúde relevantes.
    /// </summary>
    public static IReadOnlyList<CondicaoSaude> CondicoesSaude { get; } = new[]
    {
        new CondicaoSaude { Id = 1, Nome = "Sem condições de saúde relevantes", EhAusencia = true },
        new CondicaoSaude { Id = 2, Nome = "Hipertensão arterial" },
        new CondicaoSaude { Id = 3, Nome = "Diabetes mellitus tipo 1" },
        new CondicaoSaude { Id = 4, Nome = "Diabetes mellitus tipo 2" },
        new CondicaoSaude { Id = 5, Nome = "Dislipidemia" },
        new CondicaoSaude { Id = 6, Nome = "Insuficiência cardíaca" },
        new CondicaoSaude { Id = 7, Nome = "Fibrilhação auricular" },
        new CondicaoSaude { Id = 8, Nome = "Asma" },
        new CondicaoSaude { Id = 9, Nome = "Doença pulmonar obstrutiva crónica (DPOC)" },
        new CondicaoSaude { Id = 10, Nome = "Doença renal crónica" },
        new CondicaoSaude { Id = 11, Nome = "Demência" },
        new CondicaoSaude { Id = 12, Nome = "Doença de Parkinson" },
        new CondicaoSaude { Id = 13, Nome = "Epilepsia" },
        new CondicaoSaude { Id = 14, Nome = "Depressão" },
        new CondicaoSaude { Id = 15, Nome = "Osteoporose" },
    };

    /// <summary>
    /// Gets as alergias.
    /// </summary>
    public static IReadOnlyList<Alergia> Alergias { get; } = new[]
    {
        new Alergia { Id = 1, Nome = "Sem alergias conhecidas", EhAusencia = true },
        new Alergia { Id = 2, Nome = "Penicilina e derivados" },
        new Alergia { Id = 3, Nome = "Sulfonamidas" },
        new Alergia { Id = 4, Nome = "Anti-inflamatórios não esteroides (AINE)" },
        new Alergia { Id = 5, Nome = "Ácido acetilsalicílico" },
        new Alergia { Id = 6, Nome = "Iodo e meios de contraste" },
        new Alergia { Id = 7, Nome = "Látex" },
        new Alergia { Id = 8, Nome = "Marisco" },
        new Alergia { Id = 9, Nome = "Frutos de casca rija" },
        new Alergia { Id = 10, Nome = "Ovo" },
        new Alergia { Id = 11, Nome = "Proteína do leite" },
        new Alergia { Id = 12, Nome = "Pólenes" },
        new Alergia { Id = 13, Nome = "Ácaros" },
        new Alergia { Id = 14, Nome = "Picada de abelha ou vespa" },
    };

    /// <summary>
    /// Gets os graus de autonomia e mobilidade.
    /// </summary>
    public static IReadOnlyList<GrauAutonomia> GrausAutonomia { get; } = new[]
    {
        new GrauAutonomia { Id = 1, Nome = "Autónomo" },
        new GrauAutonomia { Id = 2, Nome = "Parcialmente dependente" },
        new GrauAutonomia { Id = 3, Nome = "Dependente" },
        new GrauAutonomia { Id = 4, Nome = "Totalmente dependente (acamado)" },
    };
}
