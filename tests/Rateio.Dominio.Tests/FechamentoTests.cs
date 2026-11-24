namespace Rateio.Dominio.Tests;

public class FechamentoTests
{
    private static (Grupo Grupo, Participante Ana, Participante Bia) GrupoComDivida()
    {
        var grupo = new Grupo("Casa");
        var ana = grupo.AdicionarParticipante("Ana");
        var bia = grupo.AdicionarParticipante("Bia");
        var valor = Dinheiro.DeReais(100m);
        grupo.AdicionarDespesa(new Despesa("Luz", ana, valor, Dividir.Igual(valor, grupo.Participantes)));
        return (grupo, ana, bia);
    }

    [Fact]
    public void Nova_rodada_com_acerto_pendente_e_recusada()
    {
        var (grupo, _, _) = GrupoComDivida();

        var erro = Assert.Throws<InvalidOperationException>(grupo.IniciarNovaRodada);

        Assert.Contains("falta 1 acerto", erro.Message);
        Assert.Single(grupo.Despesas);
    }

    [Fact]
    public void Nova_rodada_depois_de_quitado_apaga_despesas_e_pagamentos_e_mantem_as_pessoas()
    {
        var (grupo, ana, bia) = GrupoComDivida();
        grupo.RegistrarPagamento(new Pagamento(bia, ana, Dinheiro.DeReais(50m)));
        Assert.True(grupo.EstaQuitado);

        grupo.IniciarNovaRodada();

        Assert.Empty(grupo.Despesas);
        Assert.Empty(grupo.Pagamentos);
        Assert.Equal(2, grupo.Participantes.Count);
        Assert.False(grupo.EstaQuitado);
    }

    [Fact]
    public void Grupo_sem_despesas_nao_e_considerado_quitado()
    {
        var grupo = new Grupo("Vazio");
        grupo.AdicionarParticipante("Ana");

        Assert.False(grupo.EstaQuitado);
        Assert.Throws<InvalidOperationException>(() => grupo.GarantirQuitado("excluir o grupo"));
    }
}
