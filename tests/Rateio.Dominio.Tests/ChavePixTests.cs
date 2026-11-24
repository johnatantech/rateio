namespace Rateio.Dominio.Tests;

public class ChavePixTests
{
    [Theory]
    [InlineData(TipoChavePix.Cpf, "526.018.159-06", "52601815906")]
    [InlineData(TipoChavePix.Cnpj, "93.786.579/0001-40", "93786579000140")]
    [InlineData(TipoChavePix.Cnpj, "12.abc.345/01de-35", "12ABC34501DE35")]  // CNPJ alfanumérico
    [InlineData(TipoChavePix.Email, " Ana.Souza@Email.com ", "ana.souza@email.com")]
    [InlineData(TipoChavePix.Telefone, "(11) 98765-4321", "+5511987654321")]
    [InlineData(TipoChavePix.Telefone, "+55 21 3456-7890", "+552134567890")]
    [InlineData(TipoChavePix.Aleatoria, "123E4567-E12B-12D1-A456-426655440000", "123e4567-e12b-12d1-a456-426655440000")]
    public void Normaliza_chaves_validas(TipoChavePix tipo, string entrada, string esperado) =>
        Assert.Equal(esperado, ChavePix.Criar(tipo, entrada).Valor);

    [Theory]
    [InlineData(TipoChavePix.Cpf, "123.456.789-00")]
    [InlineData(TipoChavePix.Cpf, "111.111.111-11")]
    [InlineData(TipoChavePix.Cnpj, "93.786.579/0001-41")]
    [InlineData(TipoChavePix.Email, "ana@email")]
    [InlineData(TipoChavePix.Telefone, "123")]
    [InlineData(TipoChavePix.Aleatoria, "nao-e-uuid")]
    public void Rejeita_chaves_invalidas(TipoChavePix tipo, string entrada) =>
        Assert.Throws<ArgumentException>(() => ChavePix.Criar(tipo, entrada));

    [Theory]
    [InlineData("ana@email.com", TipoChavePix.Email)]
    [InlineData("+5511987654321", TipoChavePix.Telefone)]
    [InlineData("52601815906", TipoChavePix.Cpf)]
    [InlineData("93786579000140", TipoChavePix.Cnpj)]
    [InlineData("123e4567-e12b-12d1-a456-426655440000", TipoChavePix.Aleatoria)]
    public void Identifica_o_tipo_pela_forma(string entrada, TipoChavePix esperado) =>
        Assert.Equal(esperado, ChavePix.Identificar(entrada)?.Tipo);

    [Fact]
    public void Onze_digitos_sem_mais_e_tratado_como_CPF_e_nao_como_telefone() =>
        Assert.Null(ChavePix.Identificar("11987654321"));  // não é CPF válido, e telefone exige "+"

    [Fact]
    public void Mascara_para_exibicao()
    {
        Assert.Equal("***.018.159-**", ChavePix.Criar(TipoChavePix.Cpf, "52601815906").Mascarada());
        Assert.Equal("a***@email.com", ChavePix.Criar(TipoChavePix.Email, "ana@email.com").Mascarada());
        Assert.Equal("+5511*****4321", ChavePix.Criar(TipoChavePix.Telefone, "+5511987654321").Mascarada());
    }
}
