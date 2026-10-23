// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Application.Convites;

/// <summary>
/// Textos em português europeu da notificação e do e-mail de convite (RF-041, RNF-020). Os textos das restantes notificações
/// são da F011.
/// </summary>
public static class TextosConvite
{
    /// <summary>
    /// Assunto do e-mail de convite; não inclui o nome da ficha, para caber sempre nos 150 caracteres de <c>EnvioEmail.Assunto</c>.
    /// </summary>
    public const string Assunto = "DoseCerta: recebeu um convite para uma ficha";

    /// <summary>
    /// Mensagem da notificação <c>ConviteRecebido</c> na aplicação.
    /// </summary>
    /// <param name="nomeFicha">Nome completo do utente da ficha.</param>
    /// <param name="papel">Papel oferecido.</param>
    /// <param name="paraPrincipal">Indica se o convite é para Cuidador principal.</param>
    /// <param name="convidadoPor">Nome de quem convidou.</param>
    /// <returns>A mensagem, com no máximo 400 caracteres.</returns>
    public static string Notificacao(string nomeFicha, Papel papel, bool paraPrincipal, string convidadoPor)
    {
        return $"Tem um convite de {convidadoPor} para a ficha de {nomeFicha}, como {PapelOferecido(papel, paraPrincipal)}. "
            + "Pode aceitá-lo ou recusá-lo na área de convites.";
    }

    /// <summary>
    /// Corpo, em texto simples, do e-mail de convite. Explica como aceitar ou recusar, incluindo a quem ainda não tem conta (RF-044).
    /// </summary>
    /// <param name="convite">Convite, com a ficha e quem convidou.</param>
    /// <param name="criadoEm">Instante do convite, já formatado em dd/mm/aaaa hh:mm na hora de Lisboa (RNF-029).</param>
    /// <returns>O corpo do e-mail.</returns>
    public static string CorpoEmail(ConviteResumo convite, string criadoEm)
    {
        ArgumentNullException.ThrowIfNull(convite);
        return $"Recebeu um convite de {convite.ConvidadoPor} para a ficha de {convite.NomeFicha} no DoseCerta, como "
            + $"{PapelOferecido(convite.PapelOferecido, convite.ParaPrincipal)}.\n\n"
            + "Para aceitar ou recusar, inicie sessão no DoseCerta com este endereço de e-mail e abra a área de convites. "
            + "Se ainda não tem conta, crie uma com este endereço: o convite aparece na área de convites.\n\n"
            + $"Convite feito em {criadoEm}.";
    }

    /// <summary>
    /// Nome do papel oferecido, como aparece nos textos do convite.
    /// </summary>
    /// <param name="papel">Papel oferecido.</param>
    /// <param name="paraPrincipal">Indica se o convite é para Cuidador principal.</param>
    /// <returns><c>Cuidador principal</c>, <c>Cuidador</c> ou <c>Familiar</c>.</returns>
    public static string PapelOferecido(Papel papel, bool paraPrincipal)
    {
        return paraPrincipal ? "Cuidador principal" : papel == Papel.Cuidador ? "Cuidador" : "Familiar";
    }
}
