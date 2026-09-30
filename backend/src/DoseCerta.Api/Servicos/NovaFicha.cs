// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dominio;

namespace DoseCerta.Api.Servicos;

/// <summary>
/// Pedido de criação de uma ficha, partilhado pela F002 (ficha de terceiro) e pela F003 (ficha própria).
/// </summary>
/// <param name="ParaOUtilizadorAutenticado">
/// <see langword="true"/> para criar a ficha própria do Utilizador autenticado (F003);
/// <see langword="false"/> para uma ficha de terceiro (F002).
/// </param>
/// <param name="Identificacao">Dados de identificação.</param>
/// <param name="CondicoesSaudeIds">Condições de saúde da categoria fechada.</param>
/// <param name="AlergiasIds">Alergias da categoria fechada.</param>
/// <param name="Documentos">Documentos a associar.</param>
public record NovaFicha(
    bool ParaOUtilizadorAutenticado,
    DadosIdentificacaoFicha Identificacao,
    IReadOnlyCollection<int> CondicoesSaudeIds,
    IReadOnlyCollection<int> AlergiasIds,
    IReadOnlyCollection<NovoDocumento> Documentos);
