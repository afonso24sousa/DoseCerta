// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Ocorrencias;

/// <summary>
/// Texto em português europeu da notificação <c>NovaOcorrencia</c> na aplicação (RF-115, RNF-020, RNF-029). O e-mail da
/// notificação é da F011.
/// </summary>
public static class TextosOcorrencia
{
    /// <summary>
    /// Mensagem da notificação <c>NovaOcorrencia</c>. Não inclui a descrição, que é um dado de saúde com até 2000 caracteres:
    /// lê-se na consulta das ocorrências (OP-48), sempre com verificação da associação à ficha.
    /// </summary>
    /// <param name="nomeFicha">Nome completo do utente da ficha (até 150 caracteres).</param>
    /// <param name="nomeAutor">Nome do Cuidador que registou a ocorrência (até 120 caracteres).</param>
    /// <param name="ocorridaEm">Instante em que a ocorrência aconteceu, já formatado em dd/mm/aaaa hh:mm na hora de Lisboa.</param>
    /// <returns>A mensagem, com no máximo 400 caracteres.</returns>
    public static string Notificacao(string nomeFicha, string nomeAutor, string ocorridaEm)
    {
        return $"{nomeAutor} registou uma ocorrência na ficha de {nomeFicha}, ocorrida em {ocorridaEm}. "
            + "Pode consultá-la nas ocorrências da ficha.";
    }
}
