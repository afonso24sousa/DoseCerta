// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos;

/// <summary>
/// Entrada de uma categoria fechada.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nome">Nome.</param>
public record RespostaCategoria(int Id, string Nome);
