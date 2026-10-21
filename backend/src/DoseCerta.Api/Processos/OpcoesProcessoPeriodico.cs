// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Processos;

/// <summary>
/// Configuração do processo periódico (secção <c>ProcessoPeriodico</c>).
/// </summary>
public sealed class OpcoesProcessoPeriodico
{
    /// <summary>
    /// Nome da secção de configuração.
    /// </summary>
    public const string Seccao = "ProcessoPeriodico";

    /// <summary>
    /// Gets or sets a value indicating whether o processo periódico corre. Fica desligado nos testes de integração, que chamam
    /// os passos diretamente com o instante que querem.
    /// </summary>
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Gets or sets o intervalo entre ciclos, em segundos; por omissão 30 (Modelo de Classes do Backend, secção 8).
    /// </summary>
    public int IntervaloSegundos { get; set; } = 30;
}
