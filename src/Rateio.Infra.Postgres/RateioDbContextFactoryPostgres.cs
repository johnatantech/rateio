using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Rateio.Infra;
using Rateio.Infra.Persistencia;

namespace Rateio.Infra.Postgres;

/// <summary>
/// Usada só pela ferramenta dotnet-ef para gerar as migrations do PostgreSQL (banco da demo pública).
/// As migrations do SQL Server ficam em Rateio.Infra.
/// </summary>
public sealed class RateioDbContextFactoryPostgres : IDesignTimeDbContextFactory<RateioDbContext>
{
    public RateioDbContext CreateDbContext(string[] args)
    {
        var opcoes = new DbContextOptionsBuilder<RateioDbContext>();
        ConfiguracaoInfra.Configurar(opcoes, "Host=localhost;Database=rateio;Username=postgres", ProvedorBanco.Postgres);
        return new RateioDbContext(opcoes.Options);
    }
}
