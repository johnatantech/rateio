namespace Rateio.Dominio.Tests;

public class DinheiroTests
{
    [Fact]
    public void Soma_de_centavos_e_exata()
    {
        // Em double, 0.1 + 0.2 == 0.30000000000000004
        Assert.Equal(Dinheiro.DeReais(0.30m), Dinheiro.DeReais(0.10m) + Dinheiro.DeReais(0.20m));
    }

    [Fact]
    public void Rejeita_fracao_de_centavo() =>
        Assert.Throws<ArgumentException>(() => Dinheiro.DeReais(10.005m));

    [Theory]
    [InlineData(123456, "R$ 1.234,56")]
    [InlineData(5, "R$ 0,05")]
    public void Formata_em_reais(long centavos, string esperado) =>
        Assert.Equal(esperado, new Dinheiro(centavos).ToString().Replace('\u00A0', ' '));

    [Theory]
    [InlineData(123456, "1234.56")]
    [InlineData(1000, "10.00")]
    [InlineData(5, "0.05")]
    public void Formata_para_o_campo_de_valor_do_Pix(long centavos, string esperado) =>
        Assert.Equal(esperado, new Dinheiro(centavos).ParaPix());
}
