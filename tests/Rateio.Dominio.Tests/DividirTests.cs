namespace Rateio.Dominio.Tests;

public class DividirTests
{
    private static readonly Participante Ana = new("Ana");
    private static readonly Participante Bia = new("Bia");
    private static readonly Participante Caio = new("Caio");

    [Fact]
    public void Divide_igual_sem_perder_centavo()
    {
        var partes = Dividir.Igual(Dinheiro.DeReais(100m), [Ana, Bia, Caio]);

        Assert.Equal(new Dinheiro(3334), partes[Ana]);   // o centavo extra vai para o primeiro
        Assert.Equal(new Dinheiro(3333), partes[Bia]);
        Assert.Equal(new Dinheiro(3333), partes[Caio]);
        Assert.Equal(Dinheiro.DeReais(100m), partes.Values.Somar());
    }

    [Fact]
    public void Divide_por_cotas()
    {
        // Ana e Bia são um casal (2 cotas), Caio está sozinho (1 cota)
        var partes = Dividir.PorCotas(Dinheiro.DeReais(300m), new Dictionary<Participante, int> { [Ana] = 2, [Caio] = 1 });

        Assert.Equal(Dinheiro.DeReais(200m), partes[Ana]);
        Assert.Equal(Dinheiro.DeReais(100m), partes[Caio]);
    }

    [Fact]
    public void Divide_por_percentual_com_maiores_restos()
    {
        var partes = Dividir.PorPercentual(new Dinheiro(1000),
            new Dictionary<Participante, decimal> { [Ana] = 33.33m, [Bia] = 33.33m, [Caio] = 33.34m });

        Assert.Equal(new Dinheiro(1000), partes.Values.Somar());
        Assert.Equal(new Dinheiro(334), partes[Caio]);
    }

    [Fact]
    public void Percentuais_precisam_somar_100() =>
        Assert.Throws<ArgumentException>(() => Dividir.PorPercentual(new Dinheiro(1000),
            new Dictionary<Participante, decimal> { [Ana] = 50m, [Bia] = 40m }));

    [Fact]
    public void Valores_precisam_bater_com_o_total() =>
        Assert.Throws<ArgumentException>(() => Dividir.PorValores(new Dinheiro(1000),
            new Dictionary<Participante, Dinheiro> { [Ana] = new(600), [Bia] = new(300) }));

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 3)]
    [InlineData(100, 7)]
    [InlineData(99999, 13)]
    public void Soma_das_partes_e_sempre_o_total(long centavos, int pessoas)
    {
        var participantes = Enumerable.Range(1, pessoas).Select(i => new Participante($"P{i}")).ToList();
        var partes = Dividir.Igual(new Dinheiro(centavos), participantes);

        Assert.Equal(new Dinheiro(centavos), partes.Values.Somar());
        Assert.True(partes.Values.Max().Centavos - partes.Values.Min().Centavos <= 1);
    }
}
