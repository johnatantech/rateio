using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Rateio.Infra.Persistencia;

namespace Rateio.Infra;

public enum ProvedorBanco { SqlServer, Postgres }

public static class ConfiguracaoInfra
{
    public const string AssemblyMigrationsPostgres = "Rateio.Infra.Postgres";

    public static IServiceCollection AdicionarInfra(this IServiceCollection servicos, string conexao, ProvedorBanco provedor = ProvedorBanco.SqlServer)
    {
        servicos.AddDbContext<RateioDbContext>(opcoes => Configurar(opcoes, conexao, provedor));
        servicos.AddScoped<RepositorioDeGrupos>();
        servicos.AddSingleton(TimeProvider.System);
        return servicos;
    }

    public static void Configurar(DbContextOptionsBuilder opcoes, string conexao, ProvedorBanco provedor)
    {
        if (provedor == ProvedorBanco.Postgres)
            opcoes.UseNpgsql(ConexaoPostgres.Normalizar(conexao), npg => npg
                .MigrationsAssembly(AssemblyMigrationsPostgres)
                .EnableRetryOnFailure(maxRetryCount: 3));
        else
            opcoes.UseSqlServer(conexao, sql => sql.EnableRetryOnFailure(maxRetryCount: 3));

        opcoes.ReplaceService<IModelCacheKeyFactory, ChaveDeModeloPorProvedor>();
    }
}

/// <summary>
/// Provedores como o Neon entregam a conexão no formato URL (postgresql://usuario:senha@host/banco?sslmode=require).
/// O Npgsql espera chave=valor; esta classe aceita os dois formatos.
/// </summary>
public static class ConexaoPostgres
{
    public static string Normalizar(string conexao)
    {
        if (!conexao.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !conexao.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return conexao;

        var uri = new Uri(conexao);
        var credenciais = uri.UserInfo.Split(':', 2);
        var parametros = System.Web.HttpUtility.ParseQueryString(uri.Query);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credenciais[0]),
            Password = credenciais.Length > 1 ? Uri.UnescapeDataString(credenciais[1]) : null,
            SslMode = parametros["sslmode"]?.ToLowerInvariant() switch
            {
                "disable" => SslMode.Disable,
                "allow" => SslMode.Allow,
                "prefer" => SslMode.Prefer,
                "verify-ca" => SslMode.VerifyCA,
                "verify-full" => SslMode.VerifyFull,
                _ => SslMode.Require,
            },
        };
        return builder.ConnectionString;
    }
}
