// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Infrastructure.Email;

/// <summary>
/// Configuração do serviço de e-mail transacional (secção <c>Email</c> da configuração).
/// O endereço do remetente é dedicado ao projeto (RNF-052). As credenciais nunca são versionadas:
/// ficam em variáveis de ambiente, em user-secrets ou em GitHub Secrets (Regulamento Interno do Grupo, secção 14.2).
/// </summary>
public sealed class OpcoesEmail
{
    /// <summary>
    /// Nome da secção de configuração.
    /// </summary>
    public const string Seccao = "Email";

    /// <summary>
    /// Gets or sets o servidor SMTP do serviço transacional.
    /// </summary>
    public string Servidor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a porta SMTP (normalmente 587, com STARTTLS).
    /// </summary>
    public int Porta { get; set; } = 587;

    /// <summary>
    /// Gets or sets a value indicating whether a ligação usa TLS.
    /// </summary>
    public bool UsarTls { get; set; } = true;

    /// <summary>
    /// Gets or sets o utilizador da conta SMTP.
    /// </summary>
    public string Utilizador { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a palavra-passe ou chave da conta SMTP (segredo, nunca versionado).
    /// </summary>
    public string PalavraPasse { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets o endereço do remetente dedicado ao projeto.
    /// </summary>
    public string Remetente { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets o nome apresentado do remetente.
    /// </summary>
    public string NomeRemetente { get; set; } = "DoseCerta";

    /// <summary>
    /// Gets a value indicating whether a configuração mínima está preenchida.
    /// </summary>
    public bool Configurado =>
        !string.IsNullOrWhiteSpace(this.Servidor) && !string.IsNullOrWhiteSpace(this.Remetente);
}
