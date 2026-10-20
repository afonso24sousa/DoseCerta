// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Application.Documentos;

/// <summary>
/// Documento recebido num pedido, ainda por validar (OP-09, OP-11 e OP-15).
/// </summary>
/// <param name="Categoria">Categoria indicada: <c>ReceitaMedica</c>, <c>RelatorioClinico</c> ou <c>DocumentoIdentificacao</c>.</param>
/// <param name="NomeOriginal">Nome do ficheiro enviado pelo cliente.</param>
/// <param name="TamanhoBytes">Tamanho do ficheiro, em bytes.</param>
/// <param name="AbrirLeitura">Abre o conteúdo do ficheiro para leitura (cada chamada devolve um fluxo novo), ou <see langword="null"/> se não foi enviado ficheiro.</param>
public sealed record DocumentoSubmetido(
    string? Categoria,
    string? NomeOriginal,
    long TamanhoBytes,
    Func<Stream>? AbrirLeitura);
