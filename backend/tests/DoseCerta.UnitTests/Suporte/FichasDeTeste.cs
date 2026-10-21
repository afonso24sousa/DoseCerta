// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Entidades;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.UnitTests.Suporte;

/// <summary>
/// Fichas de exemplo, com dados fictícios, para os testes do agregado <see cref="Ficha"/> na associação de pessoas (F004).
/// </summary>
public static class FichasDeTeste
{
    /// <summary>
    /// Instante fixo usado na criação das fichas e das associações.
    /// </summary>
    public static readonly DateTime Agora = new(2026, 10, 21, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Cria uma ficha de terceiro com o Cuidador principal indicado.
    /// </summary>
    /// <param name="principalId">Cuidador principal.</param>
    /// <param name="provisorio">Indica se o Cuidador principal é provisório (convite para Cuidador principal pendente).</param>
    /// <returns>A ficha.</returns>
    public static Ficha DeTerceiro(Guid principalId, bool provisorio = false)
    {
        var ficha = new Ficha("Maria Fictícia Santos", new DateOnly(1941, 5, 17), "+351 910 000 001", null, principalId, Agora);
        ficha.AssociarCuidadorPrincipal(principalId, provisorio, Agora);
        return ficha;
    }

    /// <summary>
    /// Cria a ficha própria do titular indicado, que é o Cuidador principal.
    /// </summary>
    /// <param name="titularId">Titular da ficha.</param>
    /// <returns>A ficha.</returns>
    public static Ficha Propria(Guid titularId)
    {
        var ficha = new Ficha("Ana Fictícia", new DateOnly(1980, 2, 29), "+351 910 000 003", titularId, titularId, Agora);
        ficha.AssociarCuidadorPrincipal(titularId, provisorio: false, Agora);
        return ficha;
    }

    /// <summary>
    /// Associa uma pessoa nova à ficha com o papel indicado.
    /// </summary>
    /// <param name="ficha">Ficha.</param>
    /// <param name="papel">Papel da pessoa.</param>
    /// <returns>O identificador da pessoa associada.</returns>
    public static Guid Associar(Ficha ficha, Papel papel)
    {
        ArgumentNullException.ThrowIfNull(ficha);
        var utilizadorId = Guid.NewGuid();
        ficha.Associar(utilizadorId, papel, Agora);
        return utilizadorId;
    }
}
