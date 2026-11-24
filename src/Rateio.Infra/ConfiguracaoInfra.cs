using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rateio.Infra.Persistencia;

namespace Rateio.Infra;

public static class ConfiguracaoInfra
{
    public static IServiceCollection AdicionarInfra(this IServiceCollection servicos, string conexao)
    {
        servicos.AddDbContext<RateioDbContext>(opcoes =>
            opcoes.UseSqlServer(conexao, sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));
        servicos.AddScoped<RepositorioDeGrupos>();
        servicos.AddSingleton(TimeProvider.System);
        return servicos;
    }
}
