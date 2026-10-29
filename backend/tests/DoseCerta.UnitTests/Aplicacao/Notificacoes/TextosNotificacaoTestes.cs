// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Application.Notificacoes;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Aplicacao.Notificacoes;

/// <summary>
/// Testes unitários de <see cref="TextosNotificacao"/> (RNF-020, RNF-029, OP-51).
/// </summary>
public class TextosNotificacaoTestes
{
    /// <summary>
    /// A mensagem da toma por confirmar segue o exemplo de OP-51 e cabe nos 400 caracteres de <c>Notificacao.Mensagem</c>,
    /// mesmo com o medicamento (120) e o nome (150) mais longos.
    /// </summary>
    [Fact]
    public void MensagemDaTomaComoNoExemploDaApi()
    {
        Assert.Equal(
            "A toma de Insulina de Maria Fictícia Santos das 10:00 não foi confirmada.",
            TextosNotificacao.TomaPorConfirmar("Insulina", "Maria Fictícia Santos", "10:00"));
        Assert.True(TextosNotificacao.TomaPorConfirmar(new string('m', 120), new string('n', 150), "23:59").Length <= 400);
    }

    /// <summary>
    /// O assunto do e-mail depende do tipo, não tem dados de saúde e cabe nos 150 caracteres de <c>EnvioEmail.Assunto</c>; os
    /// tipos sem assunto próprio usam o genérico.
    /// </summary>
    /// <param name="tipo">Tipo da notificação.</param>
    /// <param name="esperado">Assunto esperado.</param>
    [Theory]
    [InlineData(TipoNotificacao.TomaEmAtraso, TextosNotificacao.AssuntoTomaEmAtraso)]
    [InlineData(TipoNotificacao.TomaGraveFamiliares, TextosNotificacao.AssuntoTomaGraveFamiliares)]
    [InlineData(TipoNotificacao.ConviteRecebido, TextosConvite.Assunto)]
    [InlineData(TipoNotificacao.NovaOcorrencia, TextosNotificacao.AssuntoGenerico)]
    [InlineData(TipoNotificacao.ProximoCompromisso, TextosNotificacao.AssuntoGenerico)]
    public void AssuntoDoEmailPorTipo(TipoNotificacao tipo, string esperado)
    {
        var assunto = TextosNotificacao.AssuntoEmail(tipo);

        Assert.Equal(esperado, assunto);
        Assert.True(assunto.Length <= 150);
    }

    /// <summary>
    /// O corpo do e-mail repete a mensagem da notificação, indica onde a consultar e quando foi criada.
    /// </summary>
    [Fact]
    public void CorpoDoEmailTemAMensagemEAData()
    {
        Assert.Equal(
            "Mensagem da notificação.\n\nPode consultar esta notificação no histórico de notificações do DoseCerta.\n\n"
            + "Notificação de 07/10/2026 11:00.",
            TextosNotificacao.CorpoEmail("Mensagem da notificação.", "07/10/2026 11:00"));
    }
}
