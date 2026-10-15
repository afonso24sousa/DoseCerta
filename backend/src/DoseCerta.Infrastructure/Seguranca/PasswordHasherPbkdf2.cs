// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DoseCerta.Application.Abstracoes;

namespace DoseCerta.Infrastructure.Seguranca;

/// <summary>
/// Hash das palavras-passe com PBKDF2-HMAC-SHA256 e salt aleatório de 16 bytes (RNF-001).
/// Formato guardado: <c>PBKDF2-SHA256$&lt;iterações&gt;$&lt;salt base64&gt;$&lt;hash base64&gt;</c>.
/// As 100 000 iterações mantêm o início de sessão abaixo dos 2 segundos (RNF-013).
/// </summary>
public sealed class PasswordHasherPbkdf2 : IPasswordHasher
{
    /// <summary>
    /// Identificador do algoritmo no hash guardado.
    /// </summary>
    public const string Algoritmo = "PBKDF2-SHA256";

    /// <summary>
    /// Número de iterações usado nos hashes novos.
    /// </summary>
    public const int Iteracoes = 100_000;

    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;

    /// <inheritdoc/>
    public string Hash(string palavraPasse)
    {
        ArgumentNullException.ThrowIfNull(palavraPasse);
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(palavraPasse), salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return string.Join(
            '$',
            Algoritmo,
            Iteracoes.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    /// <inheritdoc/>
    public bool Verificar(string hash, string palavraPasse)
    {
        ArgumentNullException.ThrowIfNull(hash);
        ArgumentNullException.ThrowIfNull(palavraPasse);

        var partes = hash.Split('$');
        if (partes.Length != 4
            || !string.Equals(partes[0], Algoritmo, StringComparison.Ordinal)
            || !int.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iteracoes)
            || iteracoes <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(palavraPasse),
                salt,
                iteracoes,
                HashAlgorithmName.SHA256,
                esperado.Length);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
