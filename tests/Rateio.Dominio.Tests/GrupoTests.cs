namespace Rateio.Dominio.Tests;

public class GrupoTests
{
    [Fact]
    public void Churrasco_de_exemplo()
    {
        var grupo = new Grupo("Churrasco");
        var ana = grupo.AdicionarParticipante("Ana");
        var bia = grupo.AdicionarParticipante("Bia");
        var caio = grupo.AdicionarParticipante("Caio");
        var todos = grupo.Participantes;

        grupo.AdicionarDespesa(new Despesa("Carne", ana, Dinheiro.DeReais(180m), Dividir.Igual(Dinheiro.DeReais(180m), todos)));
        grupo.AdicionarDespesa(new Despesa("Bebidas", bia, Dinheiro.DeReais(60m), Dividir.Igual(Dinheiro.DeReais(60m), todos)));

        var saldos = grupo.CalcularSaldos();
        Assert.Equal(Dinheiro.DeReais(100m), saldos[ana]);    // pagou 180, consumiu 80
        Assert.Equal(Dinheiro.DeReais(-20m), saldos[bia]);    // pagou 60, consumiu 80
        Assert.Equal(Dinheiro.DeReais(-80m), saldos[caio]);   // consumiu 80

        var acertos = grupo.SugerirAcertos();
        Assert.Equal(2, acertos.Count);
        Assert.Contains(new Transferencia(caio, ana, Dinheiro.DeReais(80m)), acertos);
        Assert.Contains(new Transferencia(bia, ana, Dinheiro.DeReais(20m)), acertos);
    }

    [Fact]
    public void Pagamento_registrado_abate_o_saldo()
    {
        var grupo = new Grupo("Casa");
        var ana = grupo.AdicionarParticipante("Ana");
        var bia = grupo.AdicionarParticipante("Bia");
        grupo.AdicionarDespesa(new Despesa("Aluguel", ana, Dinheiro.DeReais(2000m), Dividir.Igual(Dinheiro.DeReais(2000m), grupo.Participantes)));

        grupo.RegistrarPagamento(new Pagamento(bia, ana, Dinheiro.DeReais(1000m)));

        Assert.All(grupo.CalcularSaldos().Values, saldo => Assert.True(saldo.EhZero));
        Assert.Empty(grupo.SugerirAcertos());
    }

    [Fact]
    public void Nao_aceita_despesa_de_quem_nao_e_do_grupo()
    {
        var grupo = new Grupo("Viagem");
        var ana = grupo.AdicionarParticipante("Ana");
        var intruso = new Participante("Intruso");

        Assert.Throws<InvalidOperationException>(() =>
            grupo.AdicionarDespesa(new Despesa("Hotel", intruso, new Dinheiro(100), new Dictionary<Participante, Dinheiro> { [ana] = new(100) })));
    }

    [Fact]
    public void Nao_aceita_participantes_com_mesmo_nome()
    {
        var grupo = new Grupo("Viagem");
        grupo.AdicionarParticipante("Ana");
        Assert.Throws<InvalidOperationException>(() => grupo.AdicionarParticipante(" ana "));
    }

    [Fact]
    public void Divisao_da_despesa_precisa_somar_o_valor()
    {
        var ana = new Participante("Ana");
        Assert.Throws<ArgumentException>(() =>
            new Despesa("Pizza", ana, new Dinheiro(100), new Dictionary<Participante, Dinheiro> { [ana] = new(90) }));
    }
}
