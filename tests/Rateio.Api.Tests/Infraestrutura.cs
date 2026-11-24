using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rateio.Infra.Persistencia;

namespace Rateio.Api.Tests;

/// <summary>
/// Teste que precisa de SQL Server real. Usa a variável ConnectionStrings__Rateio
/// (já definida no Codespace e no CI). Sem ela, o teste é pulado com o motivo explicado.
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
/// (Rateio_Testes_xxxxxxxx), criado pelas migrations e apagado ao final.
/// </summary>
public sealed class ApiComBanco : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _conexao;

    public ApiComBanco()
    {
        var baseConexao = Environment.GetEnvironmentVariable("ConnectionStrings__Rateio") ?? "Server=localhost";
        _conexao = new SqlConnectionStringBuilder(baseConexao)
        {
            InitialCatalog = $"Rateio_Testes_{Guid.NewGuid():N}"[..28],
        }.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Rateio", _conexao);
        builder.UseSetting("Banco:AplicarMigrationsAoIniciar", "true");
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
