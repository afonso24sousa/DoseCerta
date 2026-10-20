// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Documentos;

/// <summary>
/// Documento obtido para descarregar (OP-17): os metadados e o conteúdo do ficheiro, que o chamador fecha.
/// </summary>
/// <param name="Documento">Metadados do documento.</param>
/// <param name="Conteudo">Conteúdo do ficheiro.</param>
public sealed record FicheiroLido(Documento Documento, Stream Conteudo);
