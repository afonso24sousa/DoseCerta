// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Tratamentos;

/// <summary>
/// Tratamento com o identificador da receita mais recente, para as respostas de OP-27 a OP-31.
/// </summary>
/// <param name="Tratamento">Tratamento.</param>
/// <param name="ReceitaDocumentoId">Receita mais recente do tratamento, se existir (RF-052).</param>
public sealed record TratamentoComReceita(Tratamento Tratamento, Guid? ReceitaDocumentoId);
