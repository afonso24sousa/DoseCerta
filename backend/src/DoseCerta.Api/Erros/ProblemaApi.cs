// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text.Json.Serialization;

namespace DoseCerta.Api.Erros;

/// <summary>
/// Corpo comum de todas as respostas de erro (Documentação da API, secção 3.3).
/// </summary>
/// <param name="Codigo">Código de erro da API.</param>
/// <param name="Mensagem">Mensagem em português europeu.</param>
/// <param name="Campos">Campos em erro; só existe nos erros de validação (RNF-028).</param>
/// <param name="Confirmacao">Confirmação já gravada; só existe no erro <c>TOMA_JA_CONFIRMADA</c> (OP-33).</param>
public sealed record ProblemaApi(
    string Codigo,
    string Mensagem,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<ErroCampoApi>? Campos = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ConfirmacaoApi? Confirmacao = null);
