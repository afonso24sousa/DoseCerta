// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Aplicacao.Convites;

/// <summary>
/// Testes unitários dos textos do convite (RF-041, RNF-020).
/// </summary>
public class TextosConviteTestes
{
    /// <summary>
    /// A notificação indica quem convidou, a ficha e o papel oferecido, incluindo o de Cuidador principal.
    /// </summary>
    /// <param name="papel">Papel oferecido.</param>
    /// <param name="paraPrincipal">Indica se o convite é para Cuidador principal.</param>
    /// <param name="papelNoTexto">Papel como aparece no texto.</param>
    [Theory]
    [InlineData(Papel.Familiar, false, "Familiar")]
    [InlineData(Papel.Cuidador, false, "Cuidador")]
    [InlineData(Papel.Cuidador, true, "Cuidador principal")]
    public void NotificacaoIndicaQuemAFichaEOPapel(Papel papel, bool paraPrincipal, string papelNoTexto)
    {
        var texto = TextosConvite.Notificacao("Maria Fictícia Santos", papel, paraPrincipal, "Zé Cuidador");

        Assert.Equal(
            $"Tem um convite de Zé Cuidador para a ficha de Maria Fictícia Santos, como {papelNoTexto}. Pode aceitá-lo ou recusá-lo na área de convites.",
            texto);
    }

    /// <summary>
    /// Com o nome da conta (120) e o nome da ficha (150) no máximo, a mensagem cabe nos 400 caracteres de <c>Notificacao.Mensagem</c>
    /// e o assunto cabe nos 150 de <c>EnvioEmail.Assunto</c>.
    /// </summary>
    [Fact]
    public void TextosCabemNasColunas()
    {
        var texto = TextosConvite.Notificacao(new string('F', 150), Papel.Cuidador, true, new string('C', 120));

        Assert.True(texto.Length <= 400, $"A mensagem tem {texto.Length} caracteres.");
        Assert.True(TextosConvite.Assunto.Length <= 150);
    }
}
