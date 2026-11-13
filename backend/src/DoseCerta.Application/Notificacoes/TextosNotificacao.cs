// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Application.Convites;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Notificacoes;

/// <summary>
/// Textos em português europeu das notificações de atraso das tomas, do aviso antes de um compromisso e do e-mail de qualquer
/// notificação (RNF-020, RNF-029). O assunto nunca leva dados de saúde; a mensagem de uma toma só indica o medicamento, a ficha
/// e a hora prevista (RF-110 CA1, OP-51) e a de um compromisso o motivo, o local, a ficha, a data e a hora (RF-117 CA1). Os
/// textos do convite estão em <see cref="TextosConvite"/>.
/// </summary>
public static class TextosNotificacao
{
    /// <summary>
    /// Tamanho máximo da mensagem de uma notificação (<c>Notificacao.Mensagem</c>, nvarchar(400)).
    /// </summary>
    public const int MensagemMaxima = 400;

    /// <summary>
    /// Assunto do e-mail da notificação <c>TomaEmAtraso</c>, aos Cuidadores.
    /// </summary>
    public const string AssuntoTomaEmAtraso = "DoseCerta: toma por confirmar";

    /// <summary>
    /// Assunto do e-mail da notificação <c>TomaGraveFamiliares</c>, aos Familiares.
    /// </summary>
    public const string AssuntoTomaGraveFamiliares = "DoseCerta: toma de grau grave por confirmar";

    /// <summary>
    /// Assunto do e-mail da notificação <c>ProximoCompromisso</c>, sem motivo, local nem nome do utente (RF-117, RNF-020).
    /// </summary>
    public const string AssuntoProximoCompromisso = "DoseCerta: compromisso próximo";

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
    /// Mensagem da notificação <c>ProximoCompromisso</c>, com o motivo, o nome do utente, o local, a data e a hora (RF-117 CA1).
    /// Se não couber nos 400 caracteres (nome até 150, local até 150, motivo até 250), encurta primeiro o motivo e depois o
    /// local, terminando-os em «…»; o nome, a data e a hora nunca são cortados.
    /// </summary>
    /// <param name="motivo">Motivo do compromisso.</param>
    /// <param name="local">Local do compromisso.</param>
    /// <param name="nomeFicha">Nome completo do utente da ficha.</param>
    /// <param name="data">Data do compromisso, já formatada em dd/mm/aaaa na hora de Lisboa (RNF-029).</param>
    /// <param name="hora">Hora do compromisso, já formatada em hh:mm na hora de Lisboa (RNF-029).</param>
    /// <returns>A mensagem, com no máximo 400 caracteres.</returns>
    public static string ProximoCompromisso(string motivo, string local, string nomeFicha, string data, string hora)
    {
        ArgumentNullException.ThrowIfNull(motivo);
        ArgumentNullException.ThrowIfNull(local);
        var disponivel = Math.Max(MensagemMaxima - MensagemCompromisso(nomeFicha, string.Empty, string.Empty, data, hora).Length, 2);
        if (motivo.Length + local.Length > disponivel)
        {
            motivo = Encurtar(motivo, Math.Max(disponivel - local.Length, Math.Min(motivo.Length, disponivel / 2)));
            local = Encurtar(local, disponivel - motivo.Length);
        }

        return MensagemCompromisso(nomeFicha, motivo, local, data, hora);
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
            TipoNotificacao.ProximoCompromisso => AssuntoProximoCompromisso,
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

    private static string MensagemCompromisso(string nomeFicha, string motivo, string local, string data, string hora)
    {
        return $"Compromisso de {nomeFicha}: {motivo}, em {local}, a {data} às {hora}.";
    }

    private static string Encurtar(string texto, int maximo)
    {
        if (texto.Length <= maximo)
        {
            return texto;
        }

        return maximo <= 0 ? string.Empty : string.Concat(texto.AsSpan(0, maximo - 1).TrimEnd(), "…");
    }
}
