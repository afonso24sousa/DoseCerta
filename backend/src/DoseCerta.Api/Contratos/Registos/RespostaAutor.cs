// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Registos;

/// <summary>
/// Autor de uma ocorrência ou de uma nota (RF-092).
/// </summary>
/// <param name="Id">Identificador do Utilizador.</param>
/// <param name="Nome">Nome da conta.</param>
public sealed record RespostaAutor(Guid Id, string Nome);
