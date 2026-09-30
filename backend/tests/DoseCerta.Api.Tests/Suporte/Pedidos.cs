// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text;
using DoseCerta.Api.Dominio;
using DoseCerta.Api.Servicos;

namespace DoseCerta.Api.Tests.Suporte;

/// <summary>
/// Construção de pedidos válidos de criação de ficha, com dados fictícios.
/// </summary>
public static class Pedidos
{
    /// <summary>
    /// Identificador da opção "Sem condições de saúde relevantes".
    /// </summary>
    public const int SemCondicoes = 1;

    /// <summary>
    /// Identificador da condição "Hipertensão arterial".
    /// </summary>
    public const int Hipertensao = 2;

    /// <summary>
    /// Identificador da opção "Sem alergias conhecidas".
    /// </summary>
    public const int SemAlergias = 1;

    /// <summary>
    /// Identificador da alergia "Penicilina e derivados".
    /// </summary>
    public const int Penicilina = 2;

    /// <summary>
    /// Identificador do grau "Autónomo".
    /// </summary>
    public const int Autonomo = 1;

    /// <summary>
    /// Cria um pedido válido de ficha própria.
    /// </summary>
    /// <param name="documentos">Documentos a incluir.</param>
    /// <returns>O pedido.</returns>
    public static NovaFicha FichaPropria(params NovoDocumento[] documentos) =>
        new(
            ParaOUtilizadorAutenticado: true,
            new DadosIdentificacaoFicha("Ana Maria Teste", new DateOnly(1980, 5, 17), "Rui Teste", "+351 912 345 678", Autonomo),
            new List<int> { Hipertensao },
            new List<int> { Penicilina },
            documentos);

    /// <summary>
    /// Cria um documento PDF fictício.
    /// </summary>
    /// <param name="categoria">Categoria do documento.</param>
    /// <param name="nome">Nome do ficheiro.</param>
    /// <returns>O documento.</returns>
    public static NovoDocumento Pdf(CategoriaDocumento categoria = CategoriaDocumento.ReceitaMedica, string nome = "receita.pdf")
    {
        var conteudo = Encoding.ASCII.GetBytes("%PDF-1.4 documento ficticio");
        return new NovoDocumento(categoria, nome, "application/pdf", conteudo.Length, () => new MemoryStream(conteudo));
    }
}
