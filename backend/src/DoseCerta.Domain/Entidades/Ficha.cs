// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Domain.Comum;
using DoseCerta.Domain.Enumerados;

namespace DoseCerta.Domain.Entidades;

/// <summary>
/// Raiz do agregado da ficha: dados do utente, necessidades específicas, associações e estado.
/// As associações só se alteram através da ficha (Modelo de Classes do Backend, secção 4.1).
/// </summary>
public class Ficha
{
    /// <summary>
    /// Mensagem do erro <c>PESSOA_JA_ASSOCIADA</c>, igual na verificação prévia e na violação do índice único
    /// <c>UQ_Associacao_FichaUtilizador</c>.
    /// </summary>
    public const string MensagemPessoaJaAssociada = "A pessoa já está associada à ficha.";

    private const string MensagemPrincipalSemSubstituto =
        "O Cuidador principal só pode ser despromovido ou removido depois de outro Cuidador assumir o papel; "
        + "transfira primeiro o papel de Cuidador principal (POST /api/v1/fichas/{fichaId}/cuidador-principal).";

    private readonly List<Associacao> associacoes = new();
    private readonly List<CondicaoSaude> condicoesSaude = new();
    private readonly List<Alergia> alergias = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Ficha"/> class.
    /// </summary>
    /// <param name="nomeCompleto">Nome completo do utente.</param>
    /// <param name="dataNascimento">Data de nascimento do utente.</param>
    /// <param name="contactoEmergencia">Contacto de emergência.</param>
    /// <param name="utilizadorProprioId">Titular da ficha própria, ou <see langword="null"/> numa ficha de terceiro.</param>
    /// <param name="criadaPorId">Utilizador que cria a ficha.</param>
    /// <param name="criadaEmUtc">Instante de criação, em UTC.</param>
    public Ficha(
        string nomeCompleto,
        DateOnly dataNascimento,
        string contactoEmergencia,
        Guid? utilizadorProprioId,
        Guid criadaPorId,
        DateTime criadaEmUtc)
    {
        this.Id = Guid.NewGuid();
        this.NomeCompleto = nomeCompleto;
        this.DataNascimento = dataNascimento;
        this.ContactoEmergencia = contactoEmergencia;
        this.UtilizadorProprioId = utilizadorProprioId;
        this.Tipo = utilizadorProprioId.HasValue ? TipoFicha.Propria : TipoFicha.Terceira;
        this.Estado = EstadoFicha.Ativa;
        this.CriadaPorId = criadaPorId;
        this.CriadaEmUtc = criadaEmUtc;
        this.AtivaDesdeUtc = criadaEmUtc;
    }

    private Ficha()
    {
    }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o nome completo do utente.
    /// </summary>
    public string NomeCompleto { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a data de nascimento do utente.
    /// </summary>
    public DateOnly DataNascimento { get; private set; }

    /// <summary>
    /// Gets o contacto de emergência.
    /// </summary>
    public string ContactoEmergencia { get; private set; } = string.Empty;

    /// <summary>
    /// Gets o identificador do grau de autonomia ou mobilidade, se definido.
    /// </summary>
    public short? GrauAutonomiaId { get; private set; }

    /// <summary>
    /// Gets o grau de autonomia ou mobilidade, quando carregado.
    /// </summary>
    public GrauAutonomia? GrauAutonomia { get; private set; }

    /// <summary>
    /// Gets o tipo da ficha (própria ou de terceiro).
    /// </summary>
    public TipoFicha Tipo { get; private set; }

    /// <summary>
    /// Gets o identificador do titular, na ficha própria.
    /// </summary>
    public Guid? UtilizadorProprioId { get; private set; }

    /// <summary>
    /// Gets o estado da ficha.
    /// </summary>
    public EstadoFicha Estado { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador que criou a ficha.
    /// </summary>
    public Guid CriadaPorId { get; private set; }

    /// <summary>
    /// Gets o instante de criação, em UTC.
    /// </summary>
    public DateTime CriadaEmUtc { get; private set; }

    /// <summary>
    /// Gets o instante a partir do qual a ficha está ativa, em UTC (DM-19).
    /// </summary>
    public DateTime AtivaDesdeUtc { get; private set; }

    /// <summary>
    /// Gets o instante do arquivo, em UTC, quando arquivada.
    /// </summary>
    public DateTime? ArquivadaEmUtc { get; private set; }

    /// <summary>
    /// Gets as pessoas associadas à ficha.
    /// </summary>
    public IReadOnlyCollection<Associacao> Associacoes => this.associacoes;

    /// <summary>
    /// Gets as condições de saúde registadas.
    /// </summary>
    public IReadOnlyCollection<CondicaoSaude> CondicoesSaude => this.condicoesSaude;

    /// <summary>
    /// Gets as alergias registadas.
    /// </summary>
    public IReadOnlyCollection<Alergia> Alergias => this.alergias;

    /// <summary>
    /// Indica se a ficha pode ser eliminada: só sem medicação, tarefas nem histórico (tomas, execuções, ocorrências e notas) (RF-123).
    /// </summary>
    /// <param name="temMedicacao">Indica se a ficha tem medicação registada.</param>
    /// <param name="temTarefas">Indica se a ficha tem tarefas registadas.</param>
    /// <param name="temHistorico">Indica se a ficha tem tomas, execuções, ocorrências ou notas.</param>
    /// <returns><see langword="true"/> se a ficha puder ser eliminada.</returns>
    public static bool PodeSerEliminada(bool temMedicacao, bool temTarefas, bool temHistorico)
    {
        return !temMedicacao && !temTarefas && !temHistorico;
    }

    /// <summary>
    /// Indica se é a ficha própria de um Utilizador (F003).
    /// </summary>
    /// <returns><see langword="true"/> se for a ficha própria.</returns>
    public bool EProprio()
    {
        return this.Tipo == TipoFicha.Propria;
    }

    /// <summary>
    /// Indica se a ficha está ativa.
    /// </summary>
    /// <returns><see langword="true"/> se a ficha estiver ativa.</returns>
    public bool EstaAtiva()
    {
        return this.Estado == EstadoFicha.Ativa;
    }

    /// <summary>
    /// Recusa as operações de escrita numa ficha arquivada (RF-120).
    /// </summary>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.FichaArquivada"/>, se a ficha estiver arquivada.</exception>
    public void ExigirAtiva()
    {
        if (!this.EstaAtiva())
        {
            throw new DominioException(
                CodigosErro.FichaArquivada,
                "A ficha está arquivada; a operação só é possível depois de a reativar.");
        }
    }

    /// <summary>
    /// Indica se o Utilizador é o titular desta ficha, ou seja, se a ficha é a sua ficha própria (F003).
    /// </summary>
    /// <param name="utilizadorId">Identificador do Utilizador.</param>
    /// <returns><see langword="true"/> se a ficha for própria e pertencer ao Utilizador.</returns>
    public bool ETitular(Guid utilizadorId)
    {
        return this.EProprio() && this.UtilizadorProprioId == utilizadorId;
    }

    /// <summary>
    /// Recusa despromover ou remover o titular da ficha própria, mesmo a pedido do próprio (RF-033).
    /// Usada por <c>AlterarPapel</c> e <c>RemoverAcesso</c> depois de confirmar que a pessoa está associada.
    /// </summary>
    /// <param name="utilizadorId">Pessoa visada pela operação.</param>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.CuidadorPrincipalProtegido"/>, se a pessoa for o titular.</exception>
    public void ExigirQueNaoETitular(Guid utilizadorId)
    {
        if (this.ETitular(utilizadorId))
        {
            throw new DominioException(
                CodigosErro.CuidadorPrincipalProtegido,
                "O titular da ficha própria é sempre o Cuidador principal e não pode ser despromovido nem removido.");
        }
    }

    /// <summary>
    /// Recusa transferir o papel de Cuidador principal, ou convidar alguém para esse papel, numa ficha própria (RF-034).
    /// </summary>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.CuidadorPrincipalProtegido"/>, se a ficha for própria.</exception>
    public void ExigirPrincipalTransferivel()
    {
        if (this.EProprio())
        {
            throw new DominioException(
                CodigosErro.CuidadorPrincipalProtegido,
                "Na ficha própria o titular é sempre o Cuidador principal; o papel não se transfere.");
        }
    }

    /// <summary>
    /// Obtém a associação de um Utilizador à ficha.
    /// </summary>
    /// <param name="utilizadorId">Identificador do Utilizador.</param>
    /// <returns>A associação, ou <see langword="null"/> se o Utilizador não estiver associado.</returns>
    public Associacao? ObterAssociacao(Guid utilizadorId)
    {
        return this.associacoes.FirstOrDefault(a => a.UtilizadorId == utilizadorId);
    }

    /// <summary>
    /// Atualiza os dados de identificação do utente (RF-029).
    /// </summary>
    /// <param name="nomeCompleto">Nome completo.</param>
    /// <param name="dataNascimento">Data de nascimento.</param>
    /// <param name="contactoEmergencia">Contacto de emergência.</param>
    public void AtualizarDados(string nomeCompleto, DateOnly dataNascimento, string contactoEmergencia)
    {
        this.NomeCompleto = nomeCompleto;
        this.DataNascimento = dataNascimento;
        this.ContactoEmergencia = contactoEmergencia;
    }

    /// <summary>
    /// Substitui as necessidades específicas pelos valores das listas fechadas indicados (RF-021).
    /// </summary>
    /// <param name="grauAutonomia">Grau de autonomia ou mobilidade, ou <see langword="null"/> para nenhum.</param>
    /// <param name="condicoes">Condições de saúde.</param>
    /// <param name="alergiasDaFicha">Alergias.</param>
    public void DefinirNecessidades(GrauAutonomia? grauAutonomia, IEnumerable<CondicaoSaude> condicoes, IEnumerable<Alergia> alergiasDaFicha)
    {
        ArgumentNullException.ThrowIfNull(condicoes);
        ArgumentNullException.ThrowIfNull(alergiasDaFicha);

        this.GrauAutonomia = grauAutonomia;
        this.GrauAutonomiaId = grauAutonomia?.Id;
        this.condicoesSaude.Clear();
        this.condicoesSaude.AddRange(condicoes.DistinctBy(c => c.Id));
        this.alergias.Clear();
        this.alergias.AddRange(alergiasDaFicha.DistinctBy(a => a.Id));
    }

    /// <summary>
    /// Arquiva a ficha, preservando todo o histórico (RF-120).
    /// </summary>
    /// <param name="agoraUtc">Instante do arquivo, em UTC.</param>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.FichaJaArquivada"/>, se a ficha já estiver arquivada.</exception>
    public void Arquivar(DateTime agoraUtc)
    {
        if (!this.EstaAtiva())
        {
            throw new DominioException(CodigosErro.FichaJaArquivada, "A ficha já está arquivada.");
        }

        this.Estado = EstadoFicha.Arquivada;
        this.ArquivadaEmUtc = agoraUtc;
    }

    /// <summary>
    /// Reativa a ficha; a geração de eventos retoma a partir deste instante (RF-121, RF-122, DM-19).
    /// </summary>
    /// <param name="agoraUtc">Instante da reativação, em UTC.</param>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.FichaNaoArquivada"/>, se a ficha não estiver arquivada.</exception>
    public void Reativar(DateTime agoraUtc)
    {
        if (this.EstaAtiva())
        {
            throw new DominioException(CodigosErro.FichaNaoArquivada, "A ficha não está arquivada.");
        }

        this.Estado = EstadoFicha.Ativa;
        this.ArquivadaEmUtc = null;
        this.AtivaDesdeUtc = agoraUtc;
    }

    /// <summary>
    /// Associa um Utilizador como Cuidador principal; provisório enquanto o convite para Cuidador principal estiver pendente (RF-024, RF-025, RF-030).
    /// </summary>
    /// <param name="utilizadorId">Identificador do Utilizador.</param>
    /// <param name="provisorio">Indica se o Cuidador principal é provisório.</param>
    /// <param name="agoraUtc">Instante da associação, em UTC.</param>
    /// <returns>A associação criada.</returns>
    /// <exception cref="InvalidOperationException">Se a ficha já tiver Cuidador principal ou o Utilizador já estiver associado.</exception>
    public Associacao AssociarCuidadorPrincipal(Guid utilizadorId, bool provisorio, DateTime agoraUtc)
    {
        if (this.associacoes.Exists(a => a.EPrincipal || a.UtilizadorId == utilizadorId))
        {
            throw new InvalidOperationException("A ficha já tem Cuidador principal ou o Utilizador já está associado.");
        }

        if (provisorio && this.EProprio())
        {
            throw new InvalidOperationException("Na ficha própria o titular é sempre o Cuidador principal, nunca provisório.");
        }

        var associacao = new Associacao(this.Id, utilizadorId, Papel.Cuidador, agoraUtc);
        associacao.MarcarPrincipal(provisorio);
        this.associacoes.Add(associacao);
        return associacao;
    }

    /// <summary>
    /// Associa um Utilizador à ficha com um papel, sem ser Cuidador principal (por exemplo, ao aceitar um convite, RF-045).
    /// </summary>
    /// <param name="utilizadorId">Identificador do Utilizador.</param>
    /// <param name="papel">Papel na ficha.</param>
    /// <param name="agoraUtc">Instante da associação, em UTC.</param>
    /// <returns>A associação criada.</returns>
    /// <exception cref="InvalidOperationException">Se o Utilizador já estiver associado.</exception>
    public Associacao Associar(Guid utilizadorId, Papel papel, DateTime agoraUtc)
    {
        if (this.ObterAssociacao(utilizadorId) is not null)
        {
            throw new InvalidOperationException("O Utilizador já está associado à ficha.");
        }

        var associacao = new Associacao(this.Id, utilizadorId, papel, agoraUtc);
        this.associacoes.Add(associacao);
        return associacao;
    }

    /// <summary>
    /// OP-23 e OP-24: promove um Familiar a Cuidador ou despromove um Cuidador a Familiar (RF-049, RF-130).
    /// Verifica, por esta ordem: a pessoa está associada, não é o titular da ficha própria, a ficha não fica sem Cuidador
    /// e a pessoa não é o Cuidador principal (Modelo de Classes do Backend, secção 10; RF-033, RF-133, RF-134).
    /// Um pedido com o papel atual não altera nada.
    /// </summary>
    /// <param name="utilizadorId">Pessoa visada.</param>
    /// <param name="novoPapel">Papel pretendido.</param>
    /// <returns>A associação da pessoa, já com o papel pretendido.</returns>
    /// <exception cref="DominioException">
    /// Com <see cref="CodigosErro.PessoaNaoEncontrada"/>, <see cref="CodigosErro.CuidadorPrincipalProtegido"/>,
    /// <see cref="CodigosErro.FichaSemCuidador"/> ou <see cref="CodigosErro.CuidadorPrincipalSemSubstituto"/>.
    /// </exception>
    public Associacao AlterarPapel(Guid utilizadorId, Papel novoPapel)
    {
        var associacao = this.ExigirAssociacao(utilizadorId);
        if (associacao.Papel == novoPapel)
        {
            return associacao;
        }

        if (novoPapel == Papel.Familiar)
        {
            this.ExigirQueNaoETitular(utilizadorId);
            this.ExigirQuePodeDeixarDeSerCuidador(associacao);
        }

        associacao.DefinirPapel(novoPapel);
        return associacao;
    }

    /// <summary>
    /// OP-25: remove o acesso de uma pessoa à ficha, incluindo o do próprio Cuidador que o pede (RF-131).
    /// Verifica, por esta ordem: a pessoa está associada, não é o titular da ficha própria, a ficha não fica sem Cuidador
    /// e a pessoa não é o Cuidador principal (Modelo de Classes do Backend, secção 10; RF-033, RF-133, RF-134).
    /// </summary>
    /// <param name="utilizadorId">Pessoa visada.</param>
    /// <exception cref="DominioException">
    /// Com <see cref="CodigosErro.PessoaNaoEncontrada"/>, <see cref="CodigosErro.CuidadorPrincipalProtegido"/>,
    /// <see cref="CodigosErro.FichaSemCuidador"/> ou <see cref="CodigosErro.CuidadorPrincipalSemSubstituto"/>.
    /// </exception>
    public void RemoverAcesso(Guid utilizadorId)
    {
        var associacao = this.ExigirAssociacao(utilizadorId);
        this.ExigirQueNaoETitular(utilizadorId);
        this.ExigirQuePodeDeixarDeSerCuidador(associacao);
        this.associacoes.Remove(associacao);
    }

    /// <summary>
    /// OP-26: atribui o papel de Cuidador principal a outro Cuidador da ficha; o anterior continua Cuidador e a ficha fica
    /// com exatamente um Cuidador principal (RF-132). Se o anterior era provisório, por haver um convite para Cuidador principal
    /// pendente, o novo fica provisório no seu lugar. Não se aplica à ficha própria (RF-034).
    /// A marca é retirada ao anterior e <paramref name="gravarAlteracoes"/> é chamada antes de a dar ao novo, porque o índice único
    /// filtrado <c>UQ_Associacao_Principal</c> é verificado a cada escrita (Modelo de Dados, secção 7).
    /// </summary>
    /// <param name="novoPrincipalId">Cuidador que passa a Cuidador principal.</param>
    /// <param name="gravarAlteracoes">Grava, na transação em curso, a associação do anterior já sem a marca.</param>
    /// <returns>A associação do novo Cuidador principal.</returns>
    /// <exception cref="DominioException">
    /// Com <see cref="CodigosErro.CuidadorPrincipalProtegido"/>, numa ficha própria, ou <see cref="CodigosErro.DestinatarioNaoECuidador"/>,
    /// se a pessoa indicada não for Cuidador da ficha.
    /// </exception>
    public async Task<Associacao> TransferirPrincipalAsync(Guid novoPrincipalId, Func<Task> gravarAlteracoes)
    {
        ArgumentNullException.ThrowIfNull(gravarAlteracoes);
        this.ExigirPrincipalTransferivel();
        var novo = this.ObterAssociacao(novoPrincipalId);
        if (novo is null || !novo.EhCuidador())
        {
            throw new DominioException(
                CodigosErro.DestinatarioNaoECuidador,
                "A pessoa indicada não é Cuidador da ficha; promova-a ou convide-a como Cuidador primeiro.");
        }

        var anterior = this.associacoes.Single(a => a.EPrincipal);
        if (anterior == novo)
        {
            return novo;
        }

        var provisorio = anterior.EProvisorio;
        anterior.DesmarcarPrincipal();
        await gravarAlteracoes().ConfigureAwait(false);
        novo.MarcarPrincipal(provisorio);
        return novo;
    }

    /// <summary>
    /// Recusa associar uma pessoa que já está associada à ficha: cada Utilizador tem uma só associação por ficha
    /// (OP-19, OP-21; <c>UQ_Associacao_FichaUtilizador</c>).
    /// </summary>
    /// <param name="utilizadorId">Pessoa a associar ou a convidar.</param>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.PessoaJaAssociada"/>, se a pessoa já estiver associada.</exception>
    public void ExigirQueNaoEstaAssociado(Guid utilizadorId)
    {
        if (this.ObterAssociacao(utilizadorId) is not null)
        {
            throw new DominioException(CodigosErro.PessoaJaAssociada, MensagemPessoaJaAssociada);
        }
    }

    /// <summary>
    /// OP-19 com <c>paraPrincipal=true</c>: o Cuidador principal atual passa a provisório até à resposta ao convite (RF-025, RF-027).
    /// Só pode haver um convite para Cuidador principal pendente, o que a marca de provisório indica. Não se aplica à ficha própria (RF-034).
    /// </summary>
    /// <exception cref="DominioException">
    /// Com <see cref="CodigosErro.CuidadorPrincipalProtegido"/>, numa ficha própria, ou <see cref="CodigosErro.ConvitePendenteExiste"/>,
    /// se já existir um convite para Cuidador principal pendente.
    /// </exception>
    public void MarcarPrincipalProvisorio()
    {
        this.ExigirPrincipalTransferivel();
        var principal = this.associacoes.Single(a => a.EPrincipal);
        if (principal.EProvisorio)
        {
            throw new DominioException(
                CodigosErro.ConvitePendenteExiste,
                "Já existe um convite para Cuidador principal pendente nesta ficha; aguarde a resposta antes de convidar outra pessoa.");
        }

        principal.MarcarPrincipal(provisorio: true);
    }

    /// <summary>
    /// OP-21 de um convite para Cuidador principal: o convidado passa a Cuidador principal e o Cuidador principal provisório
    /// deixa de estar associado à ficha (RF-026).
    /// </summary>
    /// <param name="convidadoId">Utilizador que aceita o convite.</param>
    /// <param name="agoraUtc">Instante da aceitação, em UTC.</param>
    /// <returns>A associação do novo Cuidador principal.</returns>
    /// <exception cref="DominioException">Com <see cref="CodigosErro.PessoaJaAssociada"/>, se o convidado já estiver associado à ficha.</exception>
    /// <exception cref="InvalidOperationException">Se a ficha não tiver Cuidador principal provisório.</exception>
    public Associacao AceitarPrincipalConvidado(Guid convidadoId, DateTime agoraUtc)
    {
        this.ExigirQueNaoEstaAssociado(convidadoId);
        var provisorio = this.ObterPrincipalProvisorio();
        this.associacoes.Remove(provisorio);
        return this.AssociarCuidadorPrincipal(convidadoId, provisorio: false, agoraUtc);
    }

    /// <summary>
    /// OP-22 de um convite para Cuidador principal: o Cuidador principal provisório mantém o papel e deixa de ser provisório,
    /// podendo convidar outra pessoa (RF-027).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a ficha não tiver Cuidador principal provisório.</exception>
    public void RecusarPrincipalConvidado()
    {
        this.ObterPrincipalProvisorio().MarcarPrincipal(provisorio: false);
    }

    private Associacao ObterPrincipalProvisorio()
    {
        return this.associacoes.SingleOrDefault(a => a.EPrincipal && a.EProvisorio)
            ?? throw new InvalidOperationException("A ficha não tem Cuidador principal provisório: não existe convite para Cuidador principal pendente.");
    }

    private Associacao ExigirAssociacao(Guid utilizadorId)
    {
        return this.ObterAssociacao(utilizadorId)
            ?? throw new DominioException(CodigosErro.PessoaNaoEncontrada, "A pessoa não está associada à ficha.");
    }

    /// <summary>
    /// Recusa que um Cuidador deixe de o ser, por despromoção ou remoção, se a ficha ficar sem Cuidador (RF-134)
    /// ou se for o Cuidador principal sem que outro Cuidador tenha assumido o papel (RF-133).
    /// </summary>
    private void ExigirQuePodeDeixarDeSerCuidador(Associacao associacao)
    {
        if (!associacao.EhCuidador())
        {
            return;
        }

        if (this.associacoes.Count(a => a.EhCuidador()) == 1)
        {
            throw new DominioException(
                CodigosErro.FichaSemCuidador,
                "A operação deixaria a ficha sem Cuidador; promova primeiro um Familiar ou convide um novo Cuidador.");
        }

        if (associacao.EPrincipal)
        {
            throw new DominioException(CodigosErro.CuidadorPrincipalSemSubstituto, MensagemPrincipalSemSubstituto);
        }
    }
}
