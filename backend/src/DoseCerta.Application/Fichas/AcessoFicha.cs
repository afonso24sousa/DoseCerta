// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;

namespace DoseCerta.Application.Fichas;

/// <summary>
/// Ficha a que o Utilizador acedeu e a respetiva associação, que dá o papel (<c>meuPapel</c>) e se é o Cuidador principal (<c>souPrincipal</c>).
/// </summary>
/// <param name="Ficha">Ficha acedida.</param>
/// <param name="Associacao">Associação do Utilizador à ficha.</param>
public sealed record AcessoFicha(Ficha Ficha, Associacao Associacao);
