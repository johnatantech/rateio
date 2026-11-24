using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rateio.Infra.Persistencia;

/// <summary>Usada só pela ferramenta dotnet-ef para gerar migrations, sem precisar subir a API.</summary>
public sealed class RateioDbContextFactory : IDesignTimeDbContextFactory<RateioDbContext>
{
    public RateioDbContext CreateDbContext(string[] args)
    {
        var conexao = Environment.GetEnvironmentVariable("ConnectionStrings__Rateio")
            ?? "Server=localhost,1433;Database=Rateio;Integrated Security=false;TrustServerCertificate=True";
        return new RateioDbContext(new DbContextOptionsBuilder<RateioDbContext>().UseSqlServer(conexao).Options);
    }
}
