// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Dados;
using DoseCerta.Api.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DoseCerta.Api.Servicos;

/// <summary>
/// Implementação de <see cref="IServicoFichas"/>.
/// </summary>
public sealed partial class ServicoFichas : IServicoFichas
{
    private const int IdadeMaximaAnos = 130;

    private readonly DoseCertaDbContext contexto;
    private readonly IArmazenamentoDocumentos armazenamento;
    private readonly IVerificadorDependenciasFicha verificadorDependencias;
    private readonly TimeProvider relogio;
    private readonly ILogger<ServicoFichas> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServicoFichas"/> class.
    /// </summary>
    /// <param name="contexto">Contexto de dados.</param>
    /// <param name="armazenamento">Armazenamento dos ficheiros.</param>
    /// <param name="verificadorDependencias">Verificação de medicação, tarefas e histórico.</param>
    /// <param name="relogio">Relógio do sistema.</param>
    /// <param name="logger">Registo de eventos.</param>
    public ServicoFichas(
        DoseCertaDbContext contexto,
        IArmazenamentoDocumentos armazenamento,
        IVerificadorDependenciasFicha verificadorDependencias,
        TimeProvider relogio,
        ILogger<ServicoFichas> logger)
    {
        this.contexto = contexto;
        this.armazenamento = armazenamento;
        this.verificadorDependencias = verificadorDependencias;
        this.relogio = relogio;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Ficha> CriarAsync(Guid utilizadorId, NovaFicha pedido, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        if (!pedido.ParaOUtilizadorAutenticado)
        {
            throw new RegraNegocioException(
                CodigoErro.NaoImplementado,
                "A criação de fichas de terceiro (F002) é implementada na I002.");
        }

        await this.GarantirQueUtilizadorExisteAsync(utilizadorId, cancellationToken);
        var (condicoes, alergias) = await this.ValidarAsync(pedido, cancellationToken);

        if (await this.ExisteFichaPropriaAsync(utilizadorId, cancellationToken))
        {
            throw FichaPropriaJaExiste(null);
        }

        var agora = this.relogio.GetUtcNow().UtcDateTime;
        var ficha = Ficha.CriarPropria(utilizadorId, pedido.Identificacao, condicoes, alergias, agora);
        await this.GuardarNovaFichaAsync(ficha, pedido.Documentos, agora, cancellationToken);

        return await this.ObterAsync(utilizadorId, ficha.Id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Ficha> ObterAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancellationToken)
    {
        var ficha = await this.ConsultaCompleta().FirstOrDefaultAsync(f => f.Id == fichaId, cancellationToken);
        if (ficha is null || !ficha.EstaAssociado(utilizadorId))
        {
            throw FichaNaoEncontrada();
        }

        return ficha;
    }

    /// <inheritdoc/>
    public Task<Ficha?> ObterPropriaAsync(Guid utilizadorId, CancellationToken cancellationToken) =>
        this.ConsultaCompleta().FirstOrDefaultAsync(f => f.UtilizadorProprietarioId == utilizadorId, cancellationToken);

    /// <inheritdoc/>
    public async Task EliminarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancellationToken)
    {
        var ficha = await this.CarregarParaGestaoAsync(utilizadorId, fichaId, cancellationToken);

        if (await this.verificadorDependencias.TemDependenciasAsync(fichaId, cancellationToken))
        {
            throw new RegraNegocioException(
                CodigoErro.FichaComHistorico,
                "A ficha tem medicação, tarefas ou histórico e não pode ser eliminada; pode apenas ser arquivada.");
        }

        var chaves = ficha.Documentos.Select(d => d.ChaveArmazenamento).ToList();
        this.contexto.Fichas.Remove(ficha);
        await this.contexto.SaveChangesAsync(cancellationToken);

        // Os ficheiros só são apagados depois de os registos terem sido eliminados com sucesso.
        await this.RemoverFicheirosAsync(chaves);
    }

    /// <inheritdoc/>
    public async Task ArquivarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancellationToken)
    {
        var ficha = await this.CarregarParaGestaoAsync(utilizadorId, fichaId, cancellationToken);
        ficha.Arquivar(this.relogio.GetUtcNow().UtcDateTime);
        await this.contexto.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ReativarAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancellationToken)
    {
        var ficha = await this.CarregarParaGestaoAsync(utilizadorId, fichaId, cancellationToken);
        ficha.Reativar();
        await this.contexto.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AlterarPapelAsync(Guid utilizadorId, Guid fichaId, Guid alvoId, PapelFicha novoPapel, CancellationToken cancellationToken)
    {
        var ficha = await this.CarregarComoCuidadorAsync(utilizadorId, fichaId, cancellationToken);
        ficha.AlterarPapel(alvoId, novoPapel);
        await this.contexto.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoverAssociacaoAsync(Guid utilizadorId, Guid fichaId, Guid alvoId, CancellationToken cancellationToken)
    {
        var ficha = await this.CarregarComoCuidadorAsync(utilizadorId, fichaId, cancellationToken);
        ficha.RemoverAssociacao(alvoId);
        await this.contexto.SaveChangesAsync(cancellationToken);
    }

    private static RegraNegocioException FichaNaoEncontrada() =>
        new(CodigoErro.FichaNaoEncontrada, "A ficha não existe ou não está associada ao Utilizador.");

    private static RegraNegocioException AcessoNegado(string mensagem) =>
        new(CodigoErro.AcessoNegado, mensagem);

    private static RegraNegocioException FichaPropriaJaExiste(Exception? causa) =>
        new(CodigoErro.FichaPropriaJaExiste, "O Utilizador já tem uma ficha própria; só é permitida uma.", causa);

    private static void Acrescentar(Dictionary<string, List<string>> erros, string campo, string mensagem)
    {
        if (!erros.TryGetValue(campo, out var lista))
        {
            lista = new List<string>();
            erros[campo] = lista;
        }

        lista.Add(mensagem);
    }

    private static void ValidarSelecao<T>(
        Dictionary<string, List<string>> erros,
        string campo,
        IReadOnlyCollection<int> idsPedidos,
        List<T> encontrados,
        Func<T, bool> ehAusencia,
        string descricao)
    {
        if (idsPedidos.Count == 0)
        {
            Acrescentar(erros, campo, $"Indique pelo menos uma opção de {descricao} (ou a opção de ausência).");
            return;
        }

        if (encontrados.Count != idsPedidos.Distinct().Count())
        {
            Acrescentar(erros, campo, $"Existem opções de {descricao} que não pertencem à categoria fechada.");
            return;
        }

        if (encontrados.Count > 1 && encontrados.Exists(e => ehAusencia(e)))
        {
            Acrescentar(erros, campo, $"A opção de ausência de {descricao} não pode ser combinada com outras.");
        }
    }

    private static void ValidarDocumentos(Dictionary<string, List<string>> erros, IReadOnlyCollection<NovoDocumento> documentos)
    {
        if (documentos.Count > RegrasDocumentos.MaximoPorPedido)
        {
            Acrescentar(erros, "documentos", $"Só são admitidos até {RegrasDocumentos.MaximoPorPedido} documentos por pedido.");
        }

        var indice = 0;
        foreach (var documento in documentos)
        {
            var campo = $"documentos[{indice++}]";
            if (!Enum.IsDefined(documento.Categoria))
            {
                Acrescentar(erros, campo + ".categoria", "A categoria tem de ser receita médica, relatório clínico ou documento de identificação.");
            }

            if (documento.TamanhoBytes <= 0)
            {
                Acrescentar(erros, campo + ".ficheiro", "O ficheiro está vazio.");
            }
            else if (documento.TamanhoBytes > RegrasDocumentos.TamanhoMaximoBytes)
            {
                Acrescentar(erros, campo + ".ficheiro", "O ficheiro excede o tamanho máximo de 10 MB.");
            }

            var extensao = Path.GetExtension(documento.NomeFicheiro);
            if (!RegrasDocumentos.TiposAdmitidos.TryGetValue(extensao, out var tipoEsperado)
                || !string.Equals(tipoEsperado, documento.TipoConteudo, StringComparison.OrdinalIgnoreCase))
            {
                Acrescentar(erros, campo + ".ficheiro", "Só são admitidos ficheiros PDF, JPEG ou PNG.");
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível remover o ficheiro {Chave} do armazenamento.")]
    private static partial void LogFicheiroNaoRemovido(ILogger logger, string chave, Exception exception);

    private IQueryable<Ficha> ConsultaCompleta() =>
        this.contexto.Fichas
            .Include(f => f.GrauAutonomia)
            .Include(f => f.CondicoesSaude)
            .Include(f => f.Alergias)
            .Include(f => f.Documentos)
            .Include(f => f.Associacoes).ThenInclude(a => a.Utilizador)
            .AsSplitQuery();

    private Task<bool> ExisteFichaPropriaAsync(Guid utilizadorId, CancellationToken cancellationToken) =>
        this.contexto.Fichas.AsNoTracking().AnyAsync(f => f.UtilizadorProprietarioId == utilizadorId, cancellationToken);

    private async Task GarantirQueUtilizadorExisteAsync(Guid utilizadorId, CancellationToken cancellationToken)
    {
        if (!await this.contexto.Utilizadores.AnyAsync(u => u.Id == utilizadorId, cancellationToken))
        {
            throw new RegraNegocioException(CodigoErro.UtilizadorNaoEncontrado, "O Utilizador autenticado não existe.");
        }
    }

    private async Task<(List<CondicaoSaude> Condicoes, List<Alergia> Alergias)> ValidarAsync(NovaFicha pedido, CancellationToken cancellationToken)
    {
        var erros = new Dictionary<string, List<string>>();
        var dados = pedido.Identificacao;

        if (string.IsNullOrWhiteSpace(dados.NomeCompleto))
        {
            Acrescentar(erros, "nomeCompleto", "O nome completo é obrigatório.");
        }

        var hoje = DateOnly.FromDateTime(this.relogio.GetUtcNow().UtcDateTime);
        if (dados.DataNascimento > hoje)
        {
            Acrescentar(erros, "dataNascimento", "A data de nascimento não pode ser futura.");
        }
        else if (dados.DataNascimento < hoje.AddYears(-IdadeMaximaAnos))
        {
            Acrescentar(erros, "dataNascimento", "A data de nascimento não é válida.");
        }

        if (string.IsNullOrWhiteSpace(dados.ContactoEmergenciaNome))
        {
            Acrescentar(erros, "contactoEmergenciaNome", "O nome do contacto de emergência é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dados.ContactoEmergenciaTelefone))
        {
            Acrescentar(erros, "contactoEmergenciaTelefone", "O telefone do contacto de emergência é obrigatório.");
        }

        if (!await this.contexto.GrausAutonomia.AnyAsync(g => g.Id == dados.GrauAutonomiaId, cancellationToken))
        {
            Acrescentar(erros, "grauAutonomiaId", "O grau de autonomia não pertence à categoria fechada.");
        }

        var condicoes = await this.contexto.CondicoesSaude
            .Where(c => pedido.CondicoesSaudeIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        ValidarSelecao(erros, "condicoesSaudeIds", pedido.CondicoesSaudeIds, condicoes, c => c.EhAusencia, "condições de saúde");

        var alergias = await this.contexto.Alergias
            .Where(a => pedido.AlergiasIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        ValidarSelecao(erros, "alergiasIds", pedido.AlergiasIds, alergias, a => a.EhAusencia, "alergias");

        ValidarDocumentos(erros, pedido.Documentos);

        if (erros.Count > 0)
        {
            throw new ValidacaoException(erros.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }

        return (condicoes, alergias);
    }

    /// <summary>
    /// Grava a ficha e os documentos numa única transação. Cada documento só é associado depois de o
    /// ficheiro estar guardado; se um ficheiro ou a base de dados falhar, a transação é revertida e os
    /// ficheiros já guardados são removidos, pelo que nenhum dado parcial fica gravado.
    /// </summary>
    private async Task GuardarNovaFichaAsync(
        Ficha ficha,
        IReadOnlyCollection<NovoDocumento> documentos,
        DateTime agora,
        CancellationToken cancellationToken)
    {
        var chavesGuardadas = new List<string>();
        await using var transacao = await this.contexto.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var documento in documentos)
            {
                var chave = await this.GuardarFicheiroAsync(documento, cancellationToken);
                chavesGuardadas.Add(chave);
                ficha.AdicionarDocumento(
                    documento.Categoria,
                    Path.GetFileName(documento.NomeFicheiro),
                    documento.TipoConteudo.ToLowerInvariant(),
                    documento.TamanhoBytes,
                    chave,
                    agora);
            }

            this.contexto.Fichas.Add(ficha);
            await this.contexto.SaveChangesAsync(cancellationToken);
            await transacao.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            await this.DesfazerAsync(transacao, chavesGuardadas);

            // Duas criações simultâneas: o índice único da base de dados trava a segunda.
            if (ficha.EhFichaPropria && await this.ExisteFichaPropriaAsync(ficha.UtilizadorProprietarioId!.Value, CancellationToken.None))
            {
                throw FichaPropriaJaExiste(ex);
            }

            throw;
        }
        catch
        {
            await this.DesfazerAsync(transacao, chavesGuardadas);
            throw;
        }
    }

    private async Task<string> GuardarFicheiroAsync(NovoDocumento documento, CancellationToken cancellationToken)
    {
        try
        {
            await using var conteudo = documento.AbrirConteudo();
            return await this.armazenamento.GuardarAsync(conteudo, documento.NomeFicheiro, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var mensagem = $"Não foi possível guardar o documento \"{Path.GetFileName(documento.NomeFicheiro)}\". "
                + "Nenhum dado da ficha foi guardado; tente novamente.";
            throw new RegraNegocioException(CodigoErro.FalhaGravacaoDocumento, mensagem, ex);
        }
    }

    private async Task DesfazerAsync(IDbContextTransaction transacao, List<string> chavesGuardadas)
    {
        await transacao.RollbackAsync(CancellationToken.None);
        this.contexto.ChangeTracker.Clear();
        await this.RemoverFicheirosAsync(chavesGuardadas);
    }

    private async Task RemoverFicheirosAsync(IEnumerable<string> chaves)
    {
        foreach (var chave in chaves)
        {
            try
            {
                await this.armazenamento.RemoverAsync(chave, CancellationToken.None);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Um ficheiro órfão não compromete a consistência dos dados; fica registado para limpeza manual.
                LogFicheiroNaoRemovido(this.logger, chave, ex);
            }
        }
    }

    private async Task<Ficha> CarregarParaGestaoAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancellationToken)
    {
        var ficha = await this.CarregarAssociadaAsync(utilizadorId, fichaId, cancellationToken);
        if (!ficha.PodeGerirCicloDeVida(utilizadorId))
        {
            var mensagem = ficha.EhFichaPropria
                ? "Só o dono da ficha própria a pode eliminar, arquivar ou reativar."
                : "Só um Cuidador pode eliminar, arquivar ou reativar a ficha.";
            throw AcessoNegado(mensagem);
        }

        return ficha;
    }

    private async Task<Ficha> CarregarComoCuidadorAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancellationToken)
    {
        var ficha = await this.CarregarAssociadaAsync(utilizadorId, fichaId, cancellationToken);
        if (!ficha.EhCuidador(utilizadorId))
        {
            throw AcessoNegado("Só um Cuidador pode gerir as pessoas associadas à ficha.");
        }

        return ficha;
    }

    private async Task<Ficha> CarregarAssociadaAsync(Guid utilizadorId, Guid fichaId, CancellationToken cancellationToken)
    {
        var ficha = await this.contexto.Fichas
            .Include(f => f.Associacoes)
            .Include(f => f.Documentos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == fichaId, cancellationToken);

        if (ficha is null || !ficha.EstaAssociado(utilizadorId))
        {
            throw FichaNaoEncontrada();
        }

        return ficha;
    }
}
