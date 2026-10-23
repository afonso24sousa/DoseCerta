// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Net;
using DoseCerta.Domain.Enumerados;
using DoseCerta.Domain.Tempo;
using DoseCerta.IntegrationTests.Suporte;
using Microsoft.EntityFrameworkCore;

namespace DoseCerta.IntegrationTests.Api;

/// <summary>
/// Testes de integração da criação de convites (F004, OP-19), com a API completa e o SQL Server.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class ConvitesApiTestes
{
    private readonly FabricaApi fabrica;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConvitesApiTestes"/> class.
    /// </summary>
    /// <param name="fabrica">Fábrica da API partilhada.</param>
    public ConvitesApiTestes(FabricaApi fabrica)
    {
        this.fabrica = fabrica;
    }

    /// <summary>
    /// RF-040 CA1, RP-09: o Cuidador convida um e-mail como Familiar; a resposta 201 tem os campos da documentação, com
    /// <c>criadoEm</c> no desvio de Lisboa, e o convite fica pendente na base de dados sem associar ninguém à ficha.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task CuidadorConvidaFamiliar()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica, "Zé Cuidador");
        using var cliente = cuidador.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        var email = DadosTeste.EmailUnico("rui");

        using var resposta = await ClienteAssociacao.ConvidarAsync(cliente, fichaId, new { email, papel = "Familiar", paraPrincipal = false });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await ClienteApi.JsonAsync(resposta);
        var conviteId = corpo.GetProperty("id").GetGuid();
        Assert.Equal(fichaId, corpo.GetProperty("fichaId").GetGuid());
        Assert.Equal("Maria Fictícia Santos", corpo.GetProperty("nomeFicha").GetString());
        Assert.Equal("Familiar", corpo.GetProperty("papelOferecido").GetString());
        Assert.False(corpo.GetProperty("paraPrincipal").GetBoolean());
        Assert.Equal("Zé Cuidador", corpo.GetProperty("convidadoPor").GetString());
        Assert.Equal("Pendente", corpo.GetProperty("estado").GetString());
        var criadoEm = corpo.GetProperty("criadoEm").GetDateTimeOffset();
        Assert.Equal(new CalendarioLisboa().ParaLocal(criadoEm.UtcDateTime).Offset, criadoEm.Offset);
        var convite = await this.fabrica.ComContextoAsync(c => c.Convites.SingleAsync(v => v.Id == conviteId));
        Assert.Equal(email, convite.EmailConvidado);
        Assert.Equal(EstadoConvite.Pendente, convite.Estado);
        Assert.Equal(cuidador.Id, convite.ConvidadoPorId);
        Assert.Single(await ClienteAssociacao.PessoasAsync(cliente, fichaId));
    }

    /// <summary>
    /// RF-040 CA2, RNF-028: sem papel, com papel desconhecido, com e-mail inválido ou com <c>paraPrincipal=true</c> e papel Familiar,
    /// o convite não é criado e a resposta 400 indica o campo.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task DadosInvalidosDao400()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = cuidador.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);
        var email = DadosTeste.EmailUnico("rui");
        var casos = new (object Corpo, string Campo)[]
        {
            (new { email }, "papel"),
            (new { email, papel = "Administrador" }, "papel"),
            (new { email = "nao-e-email", papel = "Familiar" }, "email"),
            (new { papel = "Familiar" }, "email"),
            (new { email, papel = "Familiar", paraPrincipal = true }, "papel"),
        };

        foreach (var (corpo, campo) in casos)
        {
            using var resposta = await ClienteAssociacao.ConvidarAsync(cliente, fichaId, corpo);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var erro = await ClienteApi.JsonAsync(resposta);
            Assert.Equal("VALIDACAO_INVALIDA", erro.GetProperty("codigo").GetString());
            Assert.Contains(erro.GetProperty("campos").EnumerateArray(), c => c.GetProperty("campo").GetString() == campo);
        }

        Assert.False(await this.fabrica.ComContextoAsync(c => c.Convites.AnyAsync(v => v.FichaId == fichaId)));
    }

    /// <summary>
    /// RF-043 CA1 e CA3: um segundo convite pendente para o mesmo e-mail na mesma ficha é recusado, mesmo com maiúsculas diferentes;
    /// noutra ficha, o convite é criado.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConvitePendenteDuplicadoSoNaMesmaFicha()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = cuidador.Cliente;
        var fichaX = await ClienteFichas.CriarFichaAsync(cliente);
        var fichaY = await ClienteFichas.CriarFichaAsync(cliente, "Joaquim Fictício");
        var email = DadosTeste.EmailUnico("ana");

        using var primeiro = await ClienteAssociacao.ConvidarAsync(cliente, fichaX, new { email, papel = "Familiar" });
        using var repetido = await ClienteAssociacao.ConvidarAsync(cliente, fichaX, new { email = email.ToUpperInvariant(), papel = "Cuidador" });
        using var noutraFicha = await ClienteAssociacao.ConvidarAsync(cliente, fichaY, new { email, papel = "Familiar" });

        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal("CONVITE_PENDENTE_EXISTE", await ClienteApi.CodigoErroAsync(repetido));
        Assert.Equal(HttpStatusCode.Created, noutraFicha.StatusCode);
        Assert.Equal(1, await this.fabrica.ComContextoAsync(c => c.Convites.CountAsync(v => v.FichaId == fichaX)));
    }

    /// <summary>
    /// OP-19: o e-mail de uma pessoa já associada à ficha é recusado com 409 <c>PESSOA_JA_ASSOCIADA</c>, também o de quem convida.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task PessoaJaAssociadaDa409()
    {
        var (principal, familiar, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Familiar);
        using var cliente = principal.Cliente;
        familiar.Cliente.Dispose();

        using var paraFamiliar = await ClienteAssociacao.ConvidarAsync(cliente, fichaId, new { email = familiar.Email, papel = "Cuidador" });
        using var paraSiProprio = await ClienteAssociacao.ConvidarAsync(cliente, fichaId, new { email = principal.Email, papel = "Familiar" });

        foreach (var resposta in new[] { paraFamiliar, paraSiProprio })
        {
            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal("PESSOA_JA_ASSOCIADA", await ClienteApi.CodigoErroAsync(resposta));
        }
    }

    /// <summary>
    /// RF-025, D2a, D2b: o convite para Cuidador principal deixa o principal atual provisório; um segundo convite para Cuidador
    /// principal, para outro e-mail, dá 409 <c>CONVITE_PENDENTE_EXISTE</c>.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task ConviteParaPrincipalDeixaOPrincipalProvisorio()
    {
        var cuidador = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = cuidador.Cliente;
        var fichaId = await ClienteFichas.CriarFichaAsync(cliente);

        using var primeiro = await ClienteAssociacao.ConvidarAsync(
            cliente, fichaId, new { email = DadosTeste.EmailUnico("rui"), papel = "Cuidador", paraPrincipal = true });
        using var segundo = await ClienteAssociacao.ConvidarAsync(
            cliente, fichaId, new { email = DadosTeste.EmailUnico("rita"), papel = "Cuidador", paraPrincipal = true });

        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        Assert.True((await ClienteApi.JsonAsync(primeiro)).GetProperty("paraPrincipal").GetBoolean());
        Assert.True(Assert.Single(await ClienteAssociacao.PessoasAsync(cliente, fichaId)).GetProperty("provisorio").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
        Assert.Equal("CONVITE_PENDENTE_EXISTE", await ClienteApi.CodigoErroAsync(segundo));
    }

    /// <summary>
    /// RF-034, RF-032: na ficha própria, o convite para Cuidador principal dá 403 <c>CUIDADOR_PRINCIPAL_PROTEGIDO</c>, mas os convites
    /// para Cuidador ou Familiar seguem as regras das restantes fichas.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task NaFichaPropriaSoOConviteParaPrincipalERecusado()
    {
        var titular = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var cliente = titular.Cliente;
        var fichaId = await ClienteAssociacao.CriarFichaPropriaAsync(cliente);

        using var paraPrincipal = await ClienteAssociacao.ConvidarAsync(
            cliente, fichaId, new { email = DadosTeste.EmailUnico("rui"), papel = "Cuidador", paraPrincipal = true });
        using var paraCuidador = await ClienteAssociacao.ConvidarAsync(
            cliente, fichaId, new { email = DadosTeste.EmailUnico("rita"), papel = "Cuidador" });

        Assert.Equal(HttpStatusCode.Forbidden, paraPrincipal.StatusCode);
        Assert.Equal("CUIDADOR_PRINCIPAL_PROTEGIDO", await ClienteApi.CodigoErroAsync(paraPrincipal));
        Assert.Equal(HttpStatusCode.Created, paraCuidador.StatusCode);
    }

    /// <summary>
    /// RF-048 CA2, RNF-005, RP-13: o Familiar recebe 403, quem não está associado recebe 404 e, numa ficha arquivada, 409.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task FamiliarNaoAssociadoEFichaArquivadaNaoConvidam()
    {
        var (principal, familiar, fichaId) = await ClienteAssociacao.FichaComPessoaAsync(this.fabrica, Papel.Familiar);
        var estranho = await ClienteApi.NovoUtilizadorAutenticadoAsync(this.fabrica);
        using var clientePrincipal = principal.Cliente;
        using var clienteFamiliar = familiar.Cliente;
        using var clienteEstranho = estranho.Cliente;
        var corpo = new { email = DadosTeste.EmailUnico("rui"), papel = "Familiar" };

        using var doFamiliar = await ClienteAssociacao.ConvidarAsync(clienteFamiliar, fichaId, corpo);
        using var doEstranho = await ClienteAssociacao.ConvidarAsync(clienteEstranho, fichaId, corpo);
        using var arquivo = await clientePrincipal.PostAsync($"api/v1/fichas/{fichaId}/arquivo", null);
        arquivo.EnsureSuccessStatusCode();
        using var arquivada = await ClienteAssociacao.ConvidarAsync(clientePrincipal, fichaId, corpo);

        Assert.Equal(HttpStatusCode.Forbidden, doFamiliar.StatusCode);
        Assert.Equal("ACESSO_PROIBIDO", await ClienteApi.CodigoErroAsync(doFamiliar));
        Assert.Equal(HttpStatusCode.NotFound, doEstranho.StatusCode);
        Assert.Equal("FICHA_NAO_ENCONTRADA", await ClienteApi.CodigoErroAsync(doEstranho));
        Assert.Equal(HttpStatusCode.Conflict, arquivada.StatusCode);
        Assert.Equal("FICHA_ARQUIVADA", await ClienteApi.CodigoErroAsync(arquivada));
        Assert.False(await this.fabrica.ComContextoAsync(c => c.Convites.AnyAsync(v => v.FichaId == fichaId)));
    }

    /// <summary>
    /// RF-017: sem token, o pedido é recusado com 401.
    /// </summary>
    /// <returns>Uma tarefa assíncrona.</returns>
    [Fact]
    public async Task SemSessaoDa401()
    {
        using var cliente = this.fabrica.CreateClient();

        using var resposta = await ClienteAssociacao.ConvidarAsync(cliente, Guid.NewGuid(), new { email = "rui@exemplo.pt", papel = "Familiar" });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
