using System.Net;
using Rateio.Api.Contratos;

namespace Rateio.Api.Tests;

public sealed class ValidacoesTests(ApiComBanco api) : IClassFixture<ApiComBanco>
{
    private readonly ClienteRateio cliente = new(api.CreateClient());

    [FactComBanco]
    public async Task Grupo_inexistente_retorna_404()
    {
        var resposta = await cliente.Http.GetAsync($"/api/grupos/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [FactComBanco]
    public async Task Nome_de_participante_repetido_retorna_422()
    {
        var grupo = await cliente.CriarGrupo();
        await cliente.AdicionarParticipante(grupo.Id, "Ana");

        var (status, corpo) = await cliente.EnviarBruto(HttpMethod.Post, $"/api/grupos/{grupo.Id}/participantes",
            new AdicionarParticipanteRequisicao(" ana "));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
        Assert.Contains("Já existe um participante", corpo.GetProperty("detail").GetString());
    }

    [FactComBanco]
    public async Task Valor_com_fracao_de_centavo_retorna_400()
    {
        var grupo = await cliente.CriarGrupo();
        var ana = await cliente.AdicionarParticipante(grupo.Id, "Ana");

        var (status, corpo) = await cliente.EnviarBruto(HttpMethod.Post, $"/api/grupos/{grupo.Id}/despesas",
            new AdicionarDespesaRequisicao("Café", ana.Id, 10.005m, new DivisaoRequisicao(TipoDivisao.Igual)));

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Contains("fração de centavo", corpo.GetProperty("detail").GetString());
    }

    [FactComBanco]
    public async Task Percentuais_que_nao_somam_100_retornam_400()
    {
        var grupo = await cliente.CriarGrupo();
        var ana = await cliente.AdicionarParticipante(grupo.Id, "Ana");
        var bia = await cliente.AdicionarParticipante(grupo.Id, "Bia");

        var (status, corpo) = await cliente.EnviarBruto(HttpMethod.Post, $"/api/grupos/{grupo.Id}/despesas",
            new AdicionarDespesaRequisicao("Aluguel", ana.Id, 1000m,
                new DivisaoRequisicao(TipoDivisao.Percentual, [new(ana.Id, Percentual: 60m), new(bia.Id, Percentual: 30m)])));

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Contains("precisam somar 100%", corpo.GetProperty("detail").GetString());
    }

    [FactComBanco]
    public async Task Chave_pix_invalida_retorna_400()
    {
        var grupo = await cliente.CriarGrupo();

        var (status, _) = await cliente.EnviarBruto(HttpMethod.Post, $"/api/grupos/{grupo.Id}/participantes",
            new AdicionarParticipanteRequisicao("Ana", "123.456.789-00"));

        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [FactComBanco]
    public async Task Participante_de_outro_grupo_nao_pode_pagar_despesa()
    {
        var grupoA = await cliente.CriarGrupo("A");
        var grupoB = await cliente.CriarGrupo("B");
        await cliente.AdicionarParticipante(grupoA.Id, "Ana");
        var intruso = await cliente.AdicionarParticipante(grupoB.Id, "Intruso");

        var (status, _) = await cliente.EnviarBruto(HttpMethod.Post, $"/api/grupos/{grupoA.Id}/despesas",
            new AdicionarDespesaRequisicao("Hotel", intruso.Id, 100m, new DivisaoRequisicao(TipoDivisao.Igual)));

        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [FactComBanco]
    public async Task Health_check_confere_o_banco()
    {
        var resposta = await cliente.Http.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Healthy", await resposta.Content.ReadAsStringAsync());
    }
}
