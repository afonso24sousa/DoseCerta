// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;

namespace DoseCerta.Api.Contratos;

/// <summary>
/// Representação de uma ficha devolvida pela API.
/// </summary>
/// <param name="Id">Identificador da ficha.</param>
/// <param name="NomeCompleto">Nome completo do utente.</param>
/// <param name="DataNascimento">Data de nascimento.</param>
/// <param name="ContactoEmergenciaNome">Nome do contacto de emergência.</param>
/// <param name="ContactoEmergenciaTelefone">Telefone do contacto de emergência.</param>
/// <param name="GrauAutonomia">Grau de autonomia e mobilidade.</param>
/// <param name="CondicoesSaude">Condições de saúde.</param>
/// <param name="Alergias">Alergias.</param>
/// <param name="EhFichaPropria">Indica se é a ficha própria de um Utilizador.</param>
/// <param name="UtilizadorProprietarioId">Dono da ficha própria.</param>
/// <param name="Estado">Estado da ficha.</param>
/// <param name="DataCriacao">Data de criação (UTC).</param>
/// <param name="DataArquivo">Data do arquivo (UTC).</param>
/// <param name="Documentos">Documentos associados.</param>
/// <param name="Associacoes">Pessoas associadas.</param>
public record RespostaFicha(
    Guid Id,
    string NomeCompleto,
    DateOnly DataNascimento,
    string ContactoEmergenciaNome,
    string ContactoEmergenciaTelefone,
    RespostaCategoria GrauAutonomia,
    IReadOnlyList<RespostaCategoria> CondicoesSaude,
    IReadOnlyList<RespostaCategoria> Alergias,
    bool EhFichaPropria,
    Guid? UtilizadorProprietarioId,
    EstadoFicha Estado,
    DateTime DataCriacao,
    DateTime? DataArquivo,
    IReadOnlyList<RespostaDocumento> Documentos,
    IReadOnlyList<RespostaAssociacao> Associacoes)
{
    /// <summary>
    /// Constrói a resposta a partir da entidade.
    /// </summary>
    /// <param name="ficha">Ficha com as relações carregadas.</param>
    /// <returns>A resposta.</returns>
    public static RespostaFicha De(Ficha ficha)
    {
        ArgumentNullException.ThrowIfNull(ficha);

        return new RespostaFicha(
            ficha.Id,
            ficha.NomeCompleto,
            ficha.DataNascimento,
            ficha.ContactoEmergenciaNome,
            ficha.ContactoEmergenciaTelefone,
            new RespostaCategoria(ficha.GrauAutonomiaId, ficha.GrauAutonomia?.Nome ?? string.Empty),
            ficha.CondicoesSaude.OrderBy(c => c.Id).Select(c => new RespostaCategoria(c.Id, c.Nome)).ToList(),
            ficha.Alergias.OrderBy(a => a.Id).Select(a => new RespostaCategoria(a.Id, a.Nome)).ToList(),
            ficha.EhFichaPropria,
            ficha.UtilizadorProprietarioId,
            ficha.Estado,
            ficha.DataCriacao,
            ficha.DataArquivo,
            ficha.Documentos
                .OrderBy(d => d.DataCarregamento)
                .Select(d => new RespostaDocumento(d.Id, d.Categoria, d.NomeOriginal, d.TipoConteudo, d.TamanhoBytes, d.DataCarregamento))
                .ToList(),
            ficha.Associacoes
                .OrderByDescending(a => a.EhCuidadorPrincipal)
                .ThenBy(a => a.DataAssociacao)
                .Select(a => new RespostaAssociacao(a.UtilizadorId, a.Utilizador?.Nome ?? string.Empty, a.Papel, a.EhCuidadorPrincipal))
                .ToList());
    }
}
