// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Contratos.Tomas;

/// <summary>
/// Cuidador que confirmou uma toma (RF-064).
/// </summary>
/// <param name="Id">Identificador do Utilizador.</param>
/// <param name="Nome">Nome do Utilizador.</param>
public sealed record PessoaConfirmou(Guid Id, string Nome);
