using System.Net;
using Rateio.Api.Contratos;

namespace Rateio.Api.Tests;

public sealed class FechamentoTests(ApiComBanco api) : IClassFixture<ApiComBanco>
{
    private readonly ClienteRateio cliente = new(api.CreateClient());

    private async Task<(GrupoResposta Grupo, ParticipanteResposta Ana, ParticipanteResposta Bia)> GrupoComDivida()
    {
        var grupo = await cliente.CriarGrupo("Casa");
        var ana = await cliente.AdicionarParticipante(grupo.Id, "Ana");
        var bia = await cliente.AdicionarParticipante(grupo.Id, "Bia");
        await cliente.AdicionarDespesa(grupo.Id, "Luz", ana.Id, 100m);
        return (grupo, ana, bia);
    }

    private Task Quitar(Guid grupoId, ParticipanteResposta de, ParticipanteResposta para, decimal valor) =>
        cliente.Enviar<PagamentoResposta>(HttpMethod.Post, $"/api/grupos/{grupoId}/pagamentos",
            new RegistrarPagamentoRequisicao(de.Id, para.Id, valor));

    [FactComBanco]
    public async Task Com_acerto_pendente_nao_e_possivel_limpar_nem_excluir()
    {
        var (grupo, _, _) = await GrupoComDivida();

        var (statusRodada, corpo) = await cliente.EnviarBruto(HttpMethod.Post, $"/api/grupos/{grupo.Id}/nova-rodada", new { });
        var exclusao = await cliente.Http.DeleteAsync($"/api/grupos/{grupo.Id}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, statusRodada);
        Assert.Contains("falta 1 acerto", corpo.GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, exclusao.StatusCode);
        Assert.Single(await cliente.Obter<List<DespesaResposta>>($"/api/grupos/{grupo.Id}/despesas"));
    }

    [FactComBanco]
    public async Task Nova_rodada_apaga_despesas_e_pagamentos_e_mantem_as_pessoas()
    {
        var (grupo, ana, bia) = await GrupoComDivida();
        await Quitar(grupo.Id, bia, ana, 50m);

        var resposta = await cliente.Enviar<GrupoResposta>(HttpMethod.Post, $"/api/grupos/{grupo.Id}/nova-rodada", new { });

        Assert.Equal(0, resposta.QuantidadeDespesas);
        Assert.Equal(2, resposta.Participantes.Count);
        Assert.Empty(await cliente.Obter<List<DespesaResposta>>($"/api/grupos/{grupo.Id}/despesas"));
        Assert.Empty(await cliente.Obter<List<PagamentoResposta>>($"/api/grupos/{grupo.Id}/pagamentos"));

        // A nova rodada funciona normalmente
        await cliente.AdicionarDespesa(grupo.Id, "Internet", bia.Id, 120m);
        var acerto = Assert.Single(await cliente.Obter<List<AcertoResposta>>($"/api/grupos/{grupo.Id}/acertos"));
        Assert.Equal(60m, acerto.Valor);
    }

    [FactComBanco]
    public async Task Excluir_grupo_quitado_remove_tudo()
    {
        var (grupo, ana, bia) = await GrupoComDivida();
        await Quitar(grupo.Id, bia, ana, 50m);

        var exclusao = await cliente.Http.DeleteAsync($"/api/grupos/{grupo.Id}");
        var depois = await cliente.Http.GetAsync($"/api/grupos/{grupo.Id}");

        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, depois.StatusCode);
    }
}
