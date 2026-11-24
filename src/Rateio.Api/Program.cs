using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Rateio.Api;
using Rateio.Api.Endpoints;
using Rateio.Infra;
using Rateio.Infra.Persistencia;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var conexao = builder.Configuration.GetConnectionString("Rateio")
    ?? throw new InvalidOperationException(
        "String de conexão 'Rateio' não configurada. Defina ConnectionStrings__Rateio " +
        "(no Codespace ela já vem configurada pelo .devcontainer).");

builder.Services.AdicionarInfra(conexao);
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

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

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
