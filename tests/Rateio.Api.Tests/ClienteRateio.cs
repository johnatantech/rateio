using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rateio.Api.Contratos;

namespace Rateio.Api.Tests;

/// <summary>Atalhos para chamar a API nos testes, deixando cada cenário legível.</summary>
public sealed class ClienteRateio(HttpClient http)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public HttpClient Http => http;

    public async Task<GrupoResposta> CriarGrupo(string nome = "Churrasco", string cidade = "São Paulo") =>
        await Enviar<GrupoResposta>(HttpMethod.Post, "/api/grupos", new CriarGrupoRequisicao(nome, cidade));

    public async Task<ParticipanteResposta> AdicionarParticipante(Guid grupo, string nome, string? chave = null) =>
        await Enviar<ParticipanteResposta>(HttpMethod.Post, $"/api/grupos/{grupo}/participantes", new AdicionarParticipanteRequisicao(nome, chave));

    public async Task<DespesaResposta> AdicionarDespesa(Guid grupo, string descricao, Guid pagoPor, decimal valor, DivisaoRequisicao? divisao = null) =>
        await Enviar<DespesaResposta>(HttpMethod.Post, $"/api/grupos/{grupo}/despesas",
            new AdicionarDespesaRequisicao(descricao, pagoPor, valor, divisao ?? new DivisaoRequisicao(TipoDivisao.Igual)));

    public async Task<T> Obter<T>(string caminho) =>
        await http.GetFromJsonAsync<T>(caminho, Json) ?? throw new InvalidOperationException("Resposta vazia.");

    public async Task<T> Enviar<T>(HttpMethod metodo, string caminho, object corpo)
    {
        var resposta = await http.SendAsync(new HttpRequestMessage(metodo, caminho) { Content = JsonContent.Create(corpo, options: Json) });
        if (!resposta.IsSuccessStatusCode)
            throw new HttpRequestException($"{(int)resposta.StatusCode}: {await resposta.Content.ReadAsStringAsync()}", null, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public async Task<(HttpStatusCode Status, JsonElement Corpo)> EnviarBruto(HttpMethod metodo, string caminho, object corpo)
    {
        var resposta = await http.SendAsync(new HttpRequestMessage(metodo, caminho) { Content = JsonContent.Create(corpo, options: Json) });
        var texto = await resposta.Content.ReadAsStringAsync();
        return (resposta.StatusCode, texto.Length == 0 ? default : JsonDocument.Parse(texto).RootElement.Clone());
    }
}
