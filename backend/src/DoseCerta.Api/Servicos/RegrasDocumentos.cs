// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Servicos;

/// <summary>
/// Limites aplicados aos documentos das fichas.
/// </summary>
public static class RegrasDocumentos
{
    /// <summary>
    /// Tamanho máximo de cada ficheiro (10 MB).
    /// </summary>
    public const long TamanhoMaximoBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Número máximo de documentos por pedido.
    /// </summary>
    public const int MaximoPorPedido = 10;

    /// <summary>
    /// Gets as extensões admitidas e o tipo MIME correspondente.
    /// </summary>
    public static IReadOnlyDictionary<string, string> TiposAdmitidos { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
        };
}
