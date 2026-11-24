using Npgsql;
using Rateio.Infra;

namespace Rateio.Api.Tests;

public class ConexaoPostgresTests
{
    [Fact]
    public void Converte_url_do_Neon_para_o_formato_do_Npgsql()
    {
        var convertida = ConexaoPostgres.Normalizar(
            "postgresql://rateio_owner:s%40nh4@ep-exemplo-123.sa-east-1.aws.neon.tech/rateio?sslmode=require");

        var c = new NpgsqlConnectionStringBuilder(convertida);
        Assert.Equal("ep-exemplo-123.sa-east-1.aws.neon.tech", c.Host);
        Assert.Equal(5432, c.Port);
        Assert.Equal("rateio", c.Database);
        Assert.Equal("rateio_owner", c.Username);
        Assert.Equal("s@nh4", c.Password);
        Assert.Equal(SslMode.Require, c.SslMode);
    }

    [Fact]
    public void Mantem_conexao_que_ja_esta_no_formato_chave_valor()
    {
        const string conexao = "Host=localhost;Database=rateio;Username=postgres";
        Assert.Equal(conexao, ConexaoPostgres.Normalizar(conexao));
    }

    [Fact]
    public void Respeita_porta_e_sslmode_informados()
    {
        var c = new NpgsqlConnectionStringBuilder(ConexaoPostgres.Normalizar("postgres://u:p@db.local:6543/app?sslmode=disable"));
        Assert.Equal(6543, c.Port);
        Assert.Equal(SslMode.Disable, c.SslMode);
    }
}
