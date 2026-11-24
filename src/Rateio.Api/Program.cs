using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rateio.Api;
using Rateio.Api.Endpoints;
using Rateio.Infra;
using Rateio.Infra.Persistencia;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------ banco

var conexao = builder.Configuration.GetConnectionString("Rateio")
    ?? throw new InvalidOperationException(
        "String de conexão 'Rateio' não configurada. Defina ConnectionStrings__Rateio " +
        "(no Codespace ela já vem configurada pelo .devcontainer).");

// SQL Server no desenvolvimento e nos testes; PostgreSQL na demo pública (Neon)
var provedor = builder.Configuration.GetValue("Banco:Provedor", ProvedorBanco.SqlServer);
builder.Services.AdicionarInfra(conexao, provedor);

// ------------------------------------------------------------ API

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorDeErros>();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((documento, _, _) =>
{
    documento.Info.Title = "Rateio API";
    documento.Info.Description =
        "Divisão de despesas em grupo com acertos simplificados e Pix copia e cola. " +
        "Valores em reais com até 2 casas decimais.";
    // Sem servidor fixo: a documentação usa o endereço pelo qual foi aberta
    // (localhost ou a URL encaminhada do Codespace)
    documento.Servers?.Clear();
    return Task.CompletedTask;
}));
builder.Services.AddHealthChecks().AddDbContextCheck<RateioDbContext>("banco");
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var origens = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod()));

// ------------------------------------------------------------ proteção da demo pública

// Limita gravações por IP: a API é aberta, e isso evita que alguém encha o banco gratuito de lixo.
// Leituras não são limitadas.
var escritasPorMinuto = builder.Configuration.GetValue("LimiteDeRequisicoes:EscritasPorMinuto", 60);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
        HttpMethods.IsGet(contexto.Request.Method) || HttpMethods.IsHead(contexto.Request.Method) || HttpMethods.IsOptions(contexto.Request.Method)
            ? RateLimitPartition.GetNoLimiter("leitura")
            : RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = escritasPorMinuto, Window = TimeSpan.FromMinutes(1) }));
    o.OnRejected = async (contexto, ct) =>
    {
        contexto.HttpContext.Response.Headers.RetryAfter = "60";
        await contexto.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Muitas alterações seguidas",
            Detail = "Aguarde um minuto e tente de novo.",
        }, ct);
    };
});

// Atrás de um proxy (Render), o IP real do visitante vem no cabeçalho X-Forwarded-For
var atrasDeProxy = builder.Configuration.GetValue<bool>("Proxy:ConfiarEmCabecalhosEncaminhados");
if (atrasDeProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownIPNetworks.Clear();
        o.KnownProxies.Clear();
    });
}

var app = builder.Build();

if (atrasDeProxy)
    app.UseForwardedHeaders();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseRateLimiter();

if (app.Configuration.GetValue<bool>("Banco:AplicarMigrationsAoIniciar"))
{
    await using var escopo = app.Services.CreateAsyncScope();
    await escopo.ServiceProvider.GetRequiredService<RateioDbContext>().Database.MigrateAsync();
}

app.MapOpenApi();
app.MapScalarApiReference(o =>
{
    o.Title = "Rateio API";
    o.ShowDeveloperTools = DeveloperToolsVisibility.Never;
    o.Telemetry = false;
    o.Agent = new ScalarAgentOptions { Disabled = true };
});
app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
app.MapHealthChecks("/health");
app.MapearGrupos();

app.Run();

/// <summary>Exposto para os testes de integração (WebApplicationFactory).</summary>
public partial class Program;
