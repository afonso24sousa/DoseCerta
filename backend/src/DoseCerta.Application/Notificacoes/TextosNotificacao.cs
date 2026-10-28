// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Notificacoes;

/// <summary>
/// Textos em português europeu das notificações de atraso das tomas e do e-mail de qualquer notificação (RNF-020, RNF-029).
/// O assunto nunca leva dados de saúde; a mensagem só indica o medicamento, a ficha e a hora prevista, como pede a documentação
/// (RF-110 CA1, OP-51). Os textos do convite estão em <see cref="TextosConvite"/>.
/// </summary>
public static class TextosNotificacao
{
    /// <summary>
    /// Assunto do e-mail da notificação <c>TomaEmAtraso</c>, aos Cuidadores.
    /// </summary>
    public const string AssuntoTomaEmAtraso = "DoseCerta: toma por confirmar";

    /// <summary>
    /// Assunto do e-mail da notificação <c>TomaGraveFamiliares</c>, aos Familiares.
    /// </summary>
    public const string AssuntoTomaGraveFamiliares = "DoseCerta: toma de grau grave por confirmar";

    /// <summary>
    /// Assunto do e-mail das notificações sem assunto próprio, por exemplo as criadas por outras funcionalidades.
    /// </summary>
    public const string AssuntoGenerico = "DoseCerta: tem uma nova notificação";

    /// <summary>
    /// Mensagem das notificações <c>TomaEmAtraso</c> e <c>TomaGraveFamiliares</c>, como no exemplo de OP-51.
    /// </summary>
    /// <param name="medicamento">Medicamento copiado para a toma.</param>
    /// <param name="nomeFicha">Nome completo do utente da ficha.</param>
    /// <param name="horaPrevista">Hora prevista, já formatada em hh:mm na hora de Lisboa (RNF-029).</param>
    /// <returns>A mensagem, com no máximo 400 caracteres.</returns>
    public static string TomaPorConfirmar(string medicamento, string nomeFicha, string horaPrevista)
    {
        return $"A toma de {medicamento} de {nomeFicha} das {horaPrevista} não foi confirmada.";
    }

    /// <summary>
    /// Assunto do e-mail de uma notificação, conforme o tipo; cabe sempre nos 150 caracteres de <c>EnvioEmail.Assunto</c>.
    /// </summary>
    /// <param name="tipo">Tipo da notificação.</param>
    /// <returns>O assunto do e-mail.</returns>
    public static string AssuntoEmail(TipoNotificacao tipo)
    {
        return tipo switch
        {
            TipoNotificacao.TomaEmAtraso => AssuntoTomaEmAtraso,
            TipoNotificacao.TomaGraveFamiliares => AssuntoTomaGraveFamiliares,
            TipoNotificacao.ConviteRecebido => TextosConvite.Assunto,
            _ => AssuntoGenerico,
        };
    }

    /// <summary>
    /// Corpo, em texto simples, do e-mail de uma notificação: a mensagem da notificação na aplicação, onde a consultar e quando
    /// foi criada. Serve qualquer tipo de notificação, para todas serem enviadas por e-mail (RF-141).
    /// </summary>
    /// <param name="mensagem">Mensagem da notificação na aplicação.</param>
    /// <param name="criadaEm">Instante da notificação, já formatado em dd/mm/aaaa hh:mm na hora de Lisboa (RNF-029).</param>
    /// <returns>O corpo do e-mail.</returns>
    public static string CorpoEmail(string mensagem, string criadaEm)
    {
        return $"{mensagem}\n\n"
            + "Pode consultar esta notificação no histórico de notificações do DoseCerta.\n\n"
            + $"Notificação de {criadaEm}.";
    }
}
