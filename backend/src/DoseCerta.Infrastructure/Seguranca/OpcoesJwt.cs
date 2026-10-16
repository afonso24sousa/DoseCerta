// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Infrastructure.Seguranca;

/// <summary>
/// Configuração dos tokens JWT (secção <c>Jwt</c> da configuração; AD-02, RNF-054).
/// A chave de assinatura é um segredo: nunca é versionada (Regulamento Interno do Grupo, secção 14.2).
/// </summary>
public sealed class OpcoesJwt
{
    /// <summary>
    /// Nome da secção de configuração.
    /// </summary>
    public const string Seccao = "Jwt";

    /// <summary>
    /// Número mínimo de carateres da chave de assinatura (256 bits para HMAC-SHA256).
    /// </summary>
    public const int TamanhoMinimoChave = 32;

    /// <summary>
    /// Gets or sets o emissor dos tokens.
    /// </summary>
    public string Emissor { get; set; } = "DoseCerta";

    /// <summary>
    /// Gets or sets o público dos tokens.
    /// </summary>
    public string Publico { get; set; } = "DoseCerta";

    /// <summary>
    /// Gets or sets a chave simétrica de assinatura (segredo com pelo menos 32 carateres).
    /// </summary>
    public string Chave { get; set; } = string.Empty;

    /// <summary>
    /// Valida a configuração e lança um erro claro se a chave não estiver definida.
    /// </summary>
    /// <exception cref="InvalidOperationException">A chave está em falta ou é demasiado curta.</exception>
    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(this.Chave) || this.Chave.Length < TamanhoMinimoChave)
        {
            throw new InvalidOperationException(
                "A chave JWT (Jwt:Chave) não está configurada ou tem menos de 32 carateres. "
                + "Defina-a com 'dotnet user-secrets' ou com a variável de ambiente Jwt__Chave.");
        }
    }
}
