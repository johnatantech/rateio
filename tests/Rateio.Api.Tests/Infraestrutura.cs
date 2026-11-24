using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Rateio.Infra;
using Rateio.Infra.Persistencia;

namespace Rateio.Api.Tests;

/// <summary>
/// Teste que precisa de banco real. Usa ConnectionStrings__Rateio (já definida no Codespace e no CI)
/// e Banco__Provedor (SqlServer, o padrão, ou Postgres). Sem a conexão, o teste é pulado com o motivo explicado.
/// </summary>
public sealed class FactComBancoAttribute : FactAttribute
{
    public FactComBancoAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__Rateio")))
            Skip = "Defina ConnectionStrings__Rateio para rodar os testes de integração (já vem configurada no Codespace e no CI).";
    }
}

/// <summary>
/// Sobe a API em memória apontando para um banco exclusivo e descartável
/// (rateio_testes_xxxxxxxx), criado pelas migrations e apagado ao final.
/// </summary>
public class ApiComBanco : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly ProvedorBanco Provedor =
        Enum.TryParse<ProvedorBanco>(Environment.GetEnvironmentVariable("Banco__Provedor"), true, out var p) ? p : ProvedorBanco.SqlServer;

    private readonly string _conexao;

    public ApiComBanco()
    {
        var baseConexao = Environment.GetEnvironmentVariable("ConnectionStrings__Rateio") ?? "Server=localhost";
        var nome = $"rateio_testes_{Guid.NewGuid():N}"[..28];

        _conexao = Provedor == ProvedorBanco.Postgres
            ? new NpgsqlConnectionStringBuilder(ConexaoPostgres.Normalizar(baseConexao)) { Database = nome }.ConnectionString
            : new SqlConnectionStringBuilder(baseConexao) { InitialCatalog = nome }.ConnectionString;
    }

    /// <summary>Configurações extras para cenários específicos (ex.: limite de requisições baixo).</summary>
    protected virtual IDictionary<string, string> Configuracoes => new Dictionary<string, string>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Rateio", _conexao);
        builder.UseSetting("Banco:Provedor", Provedor.ToString());
        builder.UseSetting("Banco:AplicarMigrationsAoIniciar", "true");
        foreach (var (chave, valor) in Configuracoes)
            builder.UseSetting(chave, valor);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__Rateio")))
        {
            await using var escopo = Services.CreateAsyncScope();
            await escopo.ServiceProvider.GetRequiredService<RateioDbContext>().Database.EnsureDeletedAsync();
        }
        await base.DisposeAsync();
    }
}

/// <summary>API com limite de 3 gravações por minuto, para testar a proteção contra abuso.</summary>
public sealed class ApiComLimiteBaixo : ApiComBanco
{
    protected override IDictionary<string, string> Configuracoes =>
        new Dictionary<string, string> { ["LimiteDeRequisicoes:EscritasPorMinuto"] = "3" };
}
