// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;

namespace DoseCerta.Api.Contratos;

/// <summary>
/// Pessoa associada a uma ficha.
/// </summary>
/// <param name="UtilizadorId">Identificador do Utilizador.</param>
/// <param name="Nome">Nome do Utilizador.</param>
/// <param name="Papel">Papel na ficha.</param>
/// <param name="EhCuidadorPrincipal">Indica se é o Cuidador principal.</param>
public record RespostaAssociacao(Guid UtilizadorId, string Nome, PapelFicha Papel, bool EhCuidadorPrincipal);
