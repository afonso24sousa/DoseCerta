// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Contas;

/// <summary>
/// Corpo do pedido de alteração da palavra-passe (OP-06).
/// </summary>
/// <param name="PalavraPasseAtual">Palavra-passe atual.</param>
/// <param name="PalavraPasseNova">Nova palavra-passe (RNF-003).</param>
public sealed record PedidoAlteracaoPalavraPasse(
    string? PalavraPasseAtual,
    string? PalavraPasseNova);
