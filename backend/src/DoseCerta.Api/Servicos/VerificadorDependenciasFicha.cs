// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Servicos;

/// <summary>
/// Verificação das dependências de uma ficha sobre o modelo de dados atual.
/// </summary>
/// <remarks>
/// No Sprint 01 ainda não existem entidades de medicação, tarefas nem histórico, pelo que nenhuma ficha
/// tem dependências. Cada Issue que introduzir essas entidades deve acrescentar aqui a respetiva verificação
/// (injetando o <c>DoseCertaDbContext</c>), para que a regra de eliminação continue a ser cumprida.
/// </remarks>
public sealed class VerificadorDependenciasFicha : IVerificadorDependenciasFicha
{
    /// <inheritdoc/>
    public Task<bool> TemDependenciasAsync(Guid fichaId, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
