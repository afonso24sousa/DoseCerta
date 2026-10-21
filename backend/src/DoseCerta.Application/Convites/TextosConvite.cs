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
