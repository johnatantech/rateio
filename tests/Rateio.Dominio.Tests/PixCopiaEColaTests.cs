namespace Rateio.Dominio.Tests;

public class PixCopiaEColaTests
{
    /// <summary>Exemplo oficial do Manual de Padrões para Iniciação do Pix (Banco Central).</summary>
    private const string ExemploBcb =
        "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D";

    [Fact]
    public void Gera_exatamente_o_exemplo_oficial_do_Banco_Central()
    {
        var chave = ChavePix.Criar(TipoChavePix.Aleatoria, "123e4567-e12b-12d1-a456-426655440000");
        Assert.Equal(ExemploBcb, PixCopiaECola.Gerar(chave, "Fulano de Tal", "BRASILIA"));
    }

    [Fact]
    public void Crc_do_exemplo_oficial() =>
        Assert.Equal("1D3D", PixCopiaECola.Crc16(ExemploBcb[..^4]));

    [Fact]
    public void Le_o_exemplo_oficial()
    {
        var dados = PixCopiaECola.Ler(ExemploBcb);
        Assert.Equal("123e4567-e12b-12d1-a456-426655440000", dados.Chave);
        Assert.Equal("Fulano de Tal", dados.NomeRecebedor);
        Assert.Equal("BRASILIA", dados.Cidade);
        Assert.Null(dados.Valor);
        Assert.Equal("***", dados.Txid);
    }

    [Fact]
    public void Ida_e_volta_com_valor_descricao_e_acentos()
    {
        var chave = ChavePix.Criar(TipoChavePix.Email, "ana@email.com");
        var codigo = PixCopiaECola.Gerar(chave, "Ana Conceição", "São Paulo", Dinheiro.DeReais(80m), "Churrasco de sábado");

        var dados = PixCopiaECola.Ler(codigo);
        Assert.Equal("ana@email.com", dados.Chave);
        Assert.Equal("Ana Conceicao", dados.NomeRecebedor);   // sem acentos, como pede o padrão
        Assert.Equal("Sao Paulo", dados.Cidade);
        Assert.Equal(Dinheiro.DeReais(80m), dados.Valor);
        Assert.Equal("Churrasco de sabado", dados.Descricao);
        Assert.Contains("540580.00", codigo);                  // campo 54, tamanho 05, valor 80.00
    }

    [Fact]
    public void Corta_nome_e_cidade_no_tamanho_maximo()
    {
        var chave = ChavePix.Criar(TipoChavePix.Cpf, "52601815906");
        var dados = PixCopiaECola.Ler(PixCopiaECola.Gerar(chave,
            "Maria Aparecida dos Santos Oliveira", "Sao Jose dos Campos", new Dinheiro(100)));

        Assert.True(dados.NomeRecebedor.Length <= 25);
        Assert.True(dados.Cidade.Length <= 15);
    }

    [Fact]
    public void Codigo_alterado_falha_no_CRC()
    {
        var adulterado = ExemploBcb.Replace("Fulano", "Ciclano");
        Assert.Throws<FormatException>(() => PixCopiaECola.Ler(adulterado));
    }

    [Fact]
    public void Gera_pix_de_um_acerto_do_grupo()
    {
        var grupo = new Grupo("Churrasco");
        var ana = grupo.AdicionarParticipante("Ana", ChavePix.Criar(TipoChavePix.Telefone, "+5511987654321"));
        var bia = grupo.AdicionarParticipante("Bia");
        grupo.AdicionarDespesa(new Despesa("Carne", ana, Dinheiro.DeReais(100m), Dividir.Igual(Dinheiro.DeReais(100m), grupo.Participantes)));

        var acerto = Assert.Single(grupo.SugerirAcertos());
        var dados = PixCopiaECola.Ler(PixCopiaECola.GerarParaTransferencia(acerto, "Sao Paulo", "Churrasco"));

        Assert.Equal(bia, acerto.De);
        Assert.Equal("+5511987654321", dados.Chave);
        Assert.Equal(Dinheiro.DeReais(50m), dados.Valor);
    }

    [Fact]
    public void Recebedor_sem_chave_gera_erro_claro()
    {
        var transferencia = new Transferencia(new Participante("Bia"), new Participante("Ana"), new Dinheiro(100));
        var erro = Assert.Throws<InvalidOperationException>(() => PixCopiaECola.GerarParaTransferencia(transferencia, "Sao Paulo"));
        Assert.Contains("Ana", erro.Message);
    }
}
