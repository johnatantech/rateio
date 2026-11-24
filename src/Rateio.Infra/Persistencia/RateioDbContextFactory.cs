using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rateio.Infra.Persistencia;

/// <summary>Usada só pela ferramenta dotnet-ef para gerar migrations, sem precisar subir a API.</summary>
public sealed class RateioDbContextFactory : IDesignTimeDbContextFactory<RateioDbContext>
{
    public RateioDbContext CreateDbContext(string[] args)
    {
        // Conexão fictícia: gerar migrations não acessa o banco
        var opcoes = new DbContextOptionsBuilder<RateioDbContext>();
        ConfiguracaoInfra.Configurar(opcoes, "Server=localhost;Database=Rateio;TrustServerCertificate=True", ProvedorBanco.SqlServer);
        return new RateioDbContext(opcoes.Options);
    }
}
