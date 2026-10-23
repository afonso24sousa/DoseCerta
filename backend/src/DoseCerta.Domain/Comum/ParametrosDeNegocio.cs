// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Domain.Comum;

/// <summary>
/// Valores fixos das regras de negócio, num só lugar (Modelo de Classes do Backend, secção 4.2).
/// </summary>
public static class ParametrosDeNegocio
{
    /// <summary>
    /// Número mínimo de caracteres da palavra-passe (RNF-003).
    /// </summary>
    public const int TamanhoMinimoPalavraPasse = 8;

    /// <summary>
    /// Tamanho máximo de cada documento carregado: 10 MB (RNF-008).
    /// </summary>
    public const int TamanhoMaximoDocumento = 10 * 1024 * 1024;

    /// <summary>
    /// Dias da janela móvel em que se geram as tomas de um tratamento sem data de fim (RF-060).
    /// </summary>
    public const int JanelaMovelDias = 30;

    /// <summary>
    /// Número máximo de dias de um intervalo de consulta, contando o primeiro e o último (por exemplo OP-32).
    /// </summary>
    public const int IntervaloMaximoConsultaDias = 31;

    /// <summary>
    /// Data de nascimento mais antiga aceite numa ficha (<c>CK_Ficha_Nascimento</c>).
    /// </summary>
    public static readonly DateOnly DataNascimentoMinima = new(1900, 1, 1);

    /// <summary>
    /// Validade do token de sessão: 8 horas (RNF-009).
    /// </summary>
    public static readonly TimeSpan ValidadeSessao = TimeSpan.FromHours(8);

    /// <summary>
    /// Horizonte do calendário de tomas sincronizado na aplicação móvel: 48 horas (OP-34, RF-061).
    /// </summary>
    public static readonly TimeSpan JanelaSincronizacao = TimeSpan.FromHours(48);

    /// <summary>
    /// Verifica se a palavra-passe cumpre o RNF-003: pelo menos 8 caracteres, com uma letra e um algarismo.
    /// </summary>
    /// <param name="palavraPasse">Palavra-passe a verificar.</param>
    /// <returns><see langword="true"/> se a palavra-passe for válida.</returns>
    public static bool PalavraPasseValida(string? palavraPasse)
    {
        return palavraPasse is not null
            && palavraPasse.Length >= TamanhoMinimoPalavraPasse
            && palavraPasse.Any(char.IsLetter)
            && palavraPasse.Any(char.IsDigit);
    }
}
