// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Ficha de um utente: identificação, necessidades específicas, documentos e pessoas associadas.
/// </summary>
/// <remarks>
/// A mesma entidade serve as fichas de terceiro (F002) e a ficha própria (F003).
/// Uma ficha é própria quando <see cref="UtilizadorProprietarioId"/> está preenchido; a base de dados
/// garante, através de um índice único, que cada Utilizador tem no máximo uma ficha própria.
/// </remarks>
public class Ficha
{
    private readonly List<AssociacaoFicha> associacoes = new();
    private readonly List<DocumentoFicha> documentos = new();
    private readonly List<CondicaoSaude> condicoesSaude = new();
    private readonly List<Alergia> alergias = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Ficha"/> class.
    /// </summary>
    /// <param name="nomeCompleto">Nome completo do utente.</param>
    /// <param name="dataNascimento">Data de nascimento do utente.</param>
    /// <param name="contactoEmergenciaNome">Nome do contacto de emergência.</param>
    /// <param name="contactoEmergenciaTelefone">Telefone do contacto de emergência.</param>
    /// <param name="grauAutonomiaId">Grau de autonomia e mobilidade.</param>
    /// <param name="dataCriacao">Data de criação (UTC).</param>
    private Ficha(
        string nomeCompleto,
        DateOnly dataNascimento,
        string contactoEmergenciaNome,
        string contactoEmergenciaTelefone,
        int grauAutonomiaId,
        DateTime dataCriacao)
    {
        this.Id = Guid.NewGuid();
        this.NomeCompleto = nomeCompleto;
        this.DataNascimento = dataNascimento;
        this.ContactoEmergenciaNome = contactoEmergenciaNome;
        this.ContactoEmergenciaTelefone = contactoEmergenciaTelefone;
        this.GrauAutonomiaId = grauAutonomiaId;
        this.DataCriacao = dataCriacao;
        this.Estado = EstadoFicha.Ativa;
    }

    /// <summary>
    /// Gets o identificador da ficha.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets o nome completo do utente.
    /// </summary>
    public string NomeCompleto { get; private set; }

    /// <summary>
    /// Gets a data de nascimento do utente.
    /// </summary>
    public DateOnly DataNascimento { get; private set; }

    /// <summary>
    /// Gets o nome do contacto de emergência.
    /// </summary>
    public string ContactoEmergenciaNome { get; private set; }

    /// <summary>
    /// Gets o telefone do contacto de emergência.
    /// </summary>
    public string ContactoEmergenciaTelefone { get; private set; }

    /// <summary>
    /// Gets o identificador do grau de autonomia e mobilidade.
    /// </summary>
    public int GrauAutonomiaId { get; private set; }

    /// <summary>
    /// Gets o grau de autonomia e mobilidade.
    /// </summary>
    public GrauAutonomia? GrauAutonomia { get; private set; }

    /// <summary>
    /// Gets o identificador do Utilizador dono, quando a ficha é própria (F003); nulo numa ficha de terceiro.
    /// </summary>
    public Guid? UtilizadorProprietarioId { get; private set; }

    /// <summary>
    /// Gets o Utilizador dono, quando a ficha é própria.
    /// </summary>
    public Utilizador? UtilizadorProprietario { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a ficha é a ficha própria de um Utilizador.
    /// </summary>
    public bool EhFichaPropria => this.UtilizadorProprietarioId.HasValue;

    /// <summary>
    /// Gets o estado da ficha.
    /// </summary>
    public EstadoFicha Estado { get; private set; }

    /// <summary>
    /// Gets a data de criação (UTC).
    /// </summary>
    public DateTime DataCriacao { get; private set; }

    /// <summary>
    /// Gets a data do arquivo (UTC), quando a ficha está arquivada.
    /// </summary>
    public DateTime? DataArquivo { get; private set; }

    /// <summary>
    /// Gets os Utilizadores associados à ficha.
    /// </summary>
    public IReadOnlyCollection<AssociacaoFicha> Associacoes => this.associacoes;

    /// <summary>
    /// Gets os documentos associados à ficha.
    /// </summary>
    public IReadOnlyCollection<DocumentoFicha> Documentos => this.documentos;

    /// <summary>
    /// Gets as condições de saúde relevantes.
    /// </summary>
    public IReadOnlyCollection<CondicaoSaude> CondicoesSaude => this.condicoesSaude;

    /// <summary>
    /// Gets as alergias.
    /// </summary>
    public IReadOnlyCollection<Alergia> Alergias => this.alergias;

    /// <summary>
    /// Cria a ficha própria de um Utilizador (F003), que fica imediatamente associado como Cuidador principal,
    /// sem convite nem aceitação.
    /// </summary>
    /// <param name="utilizadorId">Utilizador dono da ficha.</param>
    /// <param name="dados">Dados de identificação e necessidades específicas.</param>
    /// <param name="condicoesSaude">Condições de saúde, já validadas contra a categoria fechada.</param>
    /// <param name="alergias">Alergias, já validadas contra a categoria fechada.</param>
    /// <param name="agora">Instante atual (UTC).</param>
    /// <returns>A ficha criada.</returns>
    public static Ficha CriarPropria(
        Guid utilizadorId,
        DadosIdentificacaoFicha dados,
        IEnumerable<CondicaoSaude> condicoesSaude,
        IEnumerable<Alergia> alergias,
        DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(dados);
        ArgumentNullException.ThrowIfNull(condicoesSaude);
        ArgumentNullException.ThrowIfNull(alergias);

        var ficha = new Ficha(
            dados.NomeCompleto,
            dados.DataNascimento,
            dados.ContactoEmergenciaNome,
            dados.ContactoEmergenciaTelefone,
            dados.GrauAutonomiaId,
            agora)
        {
            UtilizadorProprietarioId = utilizadorId,
        };

        ficha.condicoesSaude.AddRange(condicoesSaude);
        ficha.alergias.AddRange(alergias);
        ficha.associacoes.Add(new AssociacaoFicha(ficha.Id, utilizadorId, PapelFicha.Cuidador, ehCuidadorPrincipal: true, agora));
        return ficha;
    }

    /// <summary>
    /// Indica se o Utilizador está associado à ficha com qualquer papel.
    /// </summary>
    /// <param name="utilizadorId">Utilizador a verificar.</param>
    /// <returns><see langword="true"/> se estiver associado.</returns>
    public bool EstaAssociado(Guid utilizadorId) => this.ObterAssociacao(utilizadorId) is not null;

    /// <summary>
    /// Indica se o Utilizador é Cuidador da ficha.
    /// </summary>
    /// <param name="utilizadorId">Utilizador a verificar.</param>
    /// <returns><see langword="true"/> se for Cuidador.</returns>
    public bool EhCuidador(Guid utilizadorId) => this.ObterAssociacao(utilizadorId)?.Papel == PapelFicha.Cuidador;

    /// <summary>
    /// Indica se o Utilizador pode eliminar, arquivar ou reativar a ficha.
    /// Numa ficha própria só o dono o pode fazer; numa ficha de terceiro, qualquer Cuidador.
    /// </summary>
    /// <param name="utilizadorId">Utilizador a verificar.</param>
    /// <returns><see langword="true"/> se puder gerir o ciclo de vida da ficha.</returns>
    public bool PodeGerirCicloDeVida(Guid utilizadorId) =>
        this.EhFichaPropria ? this.UtilizadorProprietarioId == utilizadorId : this.EhCuidador(utilizadorId);

    /// <summary>
    /// Associa um Utilizador à ficha com o papel indicado. Aplica-se igualmente à ficha própria:
    /// o dono pode associar outras pessoas como Cuidador ou Familiar (F004).
    /// </summary>
    /// <param name="utilizadorId">Utilizador a associar.</param>
    /// <param name="papel">Papel atribuído.</param>
    /// <param name="agora">Instante atual (UTC).</param>
    /// <returns>A associação criada.</returns>
    public AssociacaoFicha AdicionarAssociacao(Guid utilizadorId, PapelFicha papel, DateTime agora)
    {
        if (this.EstaAssociado(utilizadorId))
        {
            throw new RegraNegocioException(CodigoErro.AssociacaoJaExiste, "O Utilizador já está associado a esta ficha.");
        }

        var associacao = new AssociacaoFicha(this.Id, utilizadorId, papel, ehCuidadorPrincipal: false, agora);
        this.associacoes.Add(associacao);
        return associacao;
    }

    /// <summary>
    /// Altera o papel de um Utilizador associado.
    /// </summary>
    /// <param name="utilizadorId">Utilizador cujo papel é alterado.</param>
    /// <param name="novoPapel">Novo papel.</param>
    public void AlterarPapel(Guid utilizadorId, PapelFicha novoPapel)
    {
        var associacao = this.ObterAssociacaoObrigatoria(utilizadorId);
        if (associacao.Papel == novoPapel)
        {
            return;
        }

        this.GarantirQueNaoEhDonoDaFichaPropria(associacao);
        if (associacao.EhCuidadorPrincipal)
        {
            throw new RegraNegocioException(
                CodigoErro.EstadoFichaInvalido,
                "O Cuidador principal não pode ser despromovido sem antes transferir essa função (F004).");
        }

        associacao.Papel = novoPapel;
    }

    /// <summary>
    /// Remove a associação de um Utilizador à ficha.
    /// </summary>
    /// <param name="utilizadorId">Utilizador a remover.</param>
    public void RemoverAssociacao(Guid utilizadorId)
    {
        var associacao = this.ObterAssociacaoObrigatoria(utilizadorId);
        this.GarantirQueNaoEhDonoDaFichaPropria(associacao);
        if (associacao.EhCuidadorPrincipal)
        {
            throw new RegraNegocioException(
                CodigoErro.EstadoFichaInvalido,
                "O Cuidador principal não pode ser removido sem antes transferir essa função (F004).");
        }

        this.associacoes.Remove(associacao);
    }

    /// <summary>
    /// Associa um documento cujo ficheiro já foi guardado com sucesso.
    /// </summary>
    /// <param name="categoria">Categoria do documento.</param>
    /// <param name="nomeOriginal">Nome original do ficheiro.</param>
    /// <param name="tipoConteudo">Tipo MIME.</param>
    /// <param name="tamanhoBytes">Tamanho em bytes.</param>
    /// <param name="chaveArmazenamento">Chave devolvida pelo armazenamento.</param>
    /// <param name="agora">Instante atual (UTC).</param>
    /// <returns>O documento associado.</returns>
    public DocumentoFicha AdicionarDocumento(
        CategoriaDocumento categoria,
        string nomeOriginal,
        string tipoConteudo,
        long tamanhoBytes,
        string chaveArmazenamento,
        DateTime agora)
    {
        var documento = new DocumentoFicha(categoria, nomeOriginal, tipoConteudo, tamanhoBytes, chaveArmazenamento, agora);
        this.documentos.Add(documento);
        return documento;
    }

    /// <summary>
    /// Arquiva a ficha: preserva o histórico e deixa de gerar tarefas e alarmes.
    /// </summary>
    /// <param name="agora">Instante atual (UTC).</param>
    public void Arquivar(DateTime agora)
    {
        if (this.Estado == EstadoFicha.Arquivada)
        {
            throw new RegraNegocioException(CodigoErro.EstadoFichaInvalido, "A ficha já está arquivada.");
        }

        this.Estado = EstadoFicha.Arquivada;
        this.DataArquivo = agora;
    }

    /// <summary>
    /// Reativa uma ficha arquivada.
    /// </summary>
    public void Reativar()
    {
        if (this.Estado != EstadoFicha.Arquivada)
        {
            throw new RegraNegocioException(CodigoErro.EstadoFichaInvalido, "A ficha não está arquivada.");
        }

        this.Estado = EstadoFicha.Ativa;
        this.DataArquivo = null;
    }

    private AssociacaoFicha? ObterAssociacao(Guid utilizadorId) =>
        this.associacoes.Find(a => a.UtilizadorId == utilizadorId);

    private AssociacaoFicha ObterAssociacaoObrigatoria(Guid utilizadorId) =>
        this.ObterAssociacao(utilizadorId)
        ?? throw new RegraNegocioException(CodigoErro.AssociacaoNaoEncontrada, "O Utilizador não está associado a esta ficha.");

    private void GarantirQueNaoEhDonoDaFichaPropria(AssociacaoFicha associacao)
    {
        if (this.EhFichaPropria && associacao.UtilizadorId == this.UtilizadorProprietarioId)
        {
            throw new RegraNegocioException(
                CodigoErro.CuidadorPrincipalFichaPropriaImutavel,
                "Na ficha própria, o dono é sempre o Cuidador principal e não pode ser despromovido nem removido.");
        }
    }
}
