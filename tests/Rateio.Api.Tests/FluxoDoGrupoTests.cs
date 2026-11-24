using System.Net;
using Rateio.Api.Contratos;
using Rateio.Dominio;

namespace Rateio.Api.Tests;

public sealed class FluxoDoGrupoTests(ApiComBanco api) : IClassFixture<ApiComBanco>
{
    private readonly ClienteRateio cliente = new(api.CreateClient());

    [FactComBanco]
    public async Task Churrasco_completo_do_lancamento_ao_acerto()
    {
        var grupo = await cliente.CriarGrupo();
        var ana = await cliente.AdicionarParticipante(grupo.Id, "Ana", "ana@email.com");
        var bia = await cliente.AdicionarParticipante(grupo.Id, "Bia", "+5511987654321");
        var caio = await cliente.AdicionarParticipante(grupo.Id, "Caio");

        await cliente.AdicionarDespesa(grupo.Id, "Carne", ana.Id, 180m);
        await cliente.AdicionarDespesa(grupo.Id, "Bebidas", bia.Id, 60m);

        var saldos = await cliente.Obter<List<SaldoResposta>>($"/api/grupos/{grupo.Id}/saldos");
        Assert.Equal(100m, saldos.Single(s => s.ParticipanteId == ana.Id).Saldo);
        Assert.Equal(-20m, saldos.Single(s => s.ParticipanteId == bia.Id).Saldo);
        Assert.Equal(-80m, saldos.Single(s => s.ParticipanteId == caio.Id).Saldo);
        Assert.Equal(SituacaoSaldo.AReceber, saldos.Single(s => s.ParticipanteId == ana.Id).Situacao);

        var acertos = await cliente.Obter<List<AcertoResposta>>($"/api/grupos/{grupo.Id}/acertos");
        Assert.Equal(2, acertos.Count);
        Assert.All(acertos, a => Assert.Equal(ana.Id, a.Para.Id));

        // O Pix gerado é válido (CRC confere) e leva a chave completa e o valor exato
        var doCaio = acertos.Single(a => a.De.Id == caio.Id);
        var pix = PixCopiaECola.Ler(doCaio.PixCopiaECola!);
        Assert.Equal("ana@email.com", pix.Chave);
        Assert.Equal(Dinheiro.DeReais(80m), pix.Valor);
        Assert.Equal("Sao Paulo", pix.Cidade);

        // Depois de pagar os dois acertos, o grupo fica zerado
        foreach (var acerto in acertos)
            await cliente.Enviar<PagamentoResposta>(HttpMethod.Post, $"/api/grupos/{grupo.Id}/pagamentos",
                new RegistrarPagamentoRequisicao(acerto.De.Id, acerto.Para.Id, acerto.Valor));

        Assert.Empty(await cliente.Obter<List<AcertoResposta>>($"/api/grupos/{grupo.Id}/acertos"));
        Assert.All(await cliente.Obter<List<SaldoResposta>>($"/api/grupos/{grupo.Id}/saldos"),
            s => Assert.Equal(SituacaoSaldo.Quite, s.Situacao));
        Assert.Equal(2, (await cliente.Obter<List<PagamentoResposta>>($"/api/grupos/{grupo.Id}/pagamentos")).Count);
    }

    [FactComBanco]
    public async Task Divisoes_por_valores_percentual_e_cotas_sao_gravadas_e_lidas_de_volta()
    {
        var grupo = await cliente.CriarGrupo("Viagem");
        var ana = await cliente.AdicionarParticipante(grupo.Id, "Ana");
        var bia = await cliente.AdicionarParticipante(grupo.Id, "Bia");

        await cliente.AdicionarDespesa(grupo.Id, "Hotel", ana.Id, 300m,
            new DivisaoRequisicao(TipoDivisao.Cotas, [new(ana.Id, Cotas: 2), new(bia.Id, Cotas: 1)]));
        await cliente.AdicionarDespesa(grupo.Id, "Jantar", bia.Id, 100m,
            new DivisaoRequisicao(TipoDivisao.Percentual, [new(ana.Id, Percentual: 70m), new(bia.Id, Percentual: 30m)]));
        await cliente.AdicionarDespesa(grupo.Id, "Táxi", ana.Id, 45.50m,
            new DivisaoRequisicao(TipoDivisao.Valores, [new(ana.Id, Valor: 20.50m), new(bia.Id, Valor: 25m)]));

        var despesas = await cliente.Obter<List<DespesaResposta>>($"/api/grupos/{grupo.Id}/despesas");
        Assert.Equal(3, despesas.Count);
        Assert.Equal(200m, despesas.Single(d => d.Descricao == "Hotel").Divisao.Single(p => p.ParticipanteId == ana.Id).Valor);
        Assert.Equal(70m, despesas.Single(d => d.Descricao == "Jantar").Divisao.Single(p => p.ParticipanteId == ana.Id).Valor);
        Assert.Equal(25m, despesas.Single(d => d.Descricao == "Táxi").Divisao.Single(p => p.ParticipanteId == bia.Id).Valor);

        var grupoAtual = await cliente.Obter<GrupoResposta>($"/api/grupos/{grupo.Id}");
        Assert.Equal(445.50m, grupoAtual.TotalGasto);
        Assert.Equal(3, grupoAtual.QuantidadeDespesas);
    }

    [FactComBanco]
    public async Task Divisao_igual_de_100_reais_entre_3_nao_perde_centavo()
    {
        var grupo = await cliente.CriarGrupo("Pizza");
        var ana = await cliente.AdicionarParticipante(grupo.Id, "Ana");
        await cliente.AdicionarParticipante(grupo.Id, "Bia");
        await cliente.AdicionarParticipante(grupo.Id, "Caio");

        var despesa = await cliente.AdicionarDespesa(grupo.Id, "Pizza", ana.Id, 100m);

        Assert.Equal([33.34m, 33.33m, 33.33m], despesa.Divisao.Select(p => p.Valor));
        Assert.Equal(100m, despesa.Divisao.Sum(p => p.Valor));
    }

    [FactComBanco]
    public async Task Remover_despesa_atualiza_os_saldos()
    {
        var grupo = await cliente.CriarGrupo("Casa");
        var ana = await cliente.AdicionarParticipante(grupo.Id, "Ana");
        await cliente.AdicionarParticipante(grupo.Id, "Bia");
        var despesa = await cliente.AdicionarDespesa(grupo.Id, "Mercado", ana.Id, 50m);

        var resposta = await cliente.Http.DeleteAsync($"/api/grupos/{grupo.Id}/despesas/{despesa.Id}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Empty(await cliente.Obter<List<DespesaResposta>>($"/api/grupos/{grupo.Id}/despesas"));
        Assert.Empty(await cliente.Obter<List<AcertoResposta>>($"/api/grupos/{grupo.Id}/acertos"));
    }

    [FactComBanco]
    public async Task Chave_pix_aparece_mascarada_e_acerto_sem_chave_traz_aviso()
    {
        var grupo = await cliente.CriarGrupo("Trabalho");
        var ana = await cliente.AdicionarParticipante(grupo.Id, "Ana", "526.018.159-06");
        var bia = await cliente.AdicionarParticipante(grupo.Id, "Bia");

        Assert.Equal(TipoChavePix.Cpf, ana.TipoChavePix);
        Assert.Equal("***.018.159-**", ana.ChavePixMascarada);

        await cliente.AdicionarDespesa(grupo.Id, "Almoço", bia.Id, 40m);
        var acerto = Assert.Single(await cliente.Obter<List<AcertoResposta>>($"/api/grupos/{grupo.Id}/acertos"));

        Assert.Null(acerto.PixCopiaECola);
        Assert.Contains("Bia não cadastrou chave Pix", acerto.Aviso);
    }
}
