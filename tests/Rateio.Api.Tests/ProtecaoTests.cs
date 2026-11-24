using System.Net;
using Rateio.Api.Contratos;

namespace Rateio.Api.Tests;

public sealed class ProtecaoTests(ApiComLimiteBaixo api) : IClassFixture<ApiComLimiteBaixo>
{
    private readonly ClienteRateio cliente = new(api.CreateClient());

    [FactComBanco]
    public async Task Gravacoes_acima_do_limite_retornam_429_e_leituras_continuam_liberadas()
    {
        var grupo = await cliente.CriarGrupo();                       // 1
        await cliente.AdicionarParticipante(grupo.Id, "Ana");          // 2
        await cliente.AdicionarParticipante(grupo.Id, "Bia");          // 3

        var (status, corpo) = await cliente.EnviarBruto(HttpMethod.Post, $"/api/grupos/{grupo.Id}/participantes",
            new AdicionarParticipanteRequisicao("Caio"));               // 4: passou do limite

        Assert.Equal(HttpStatusCode.TooManyRequests, status);
        Assert.Contains("Aguarde um minuto", corpo.GetProperty("detail").GetString());

        var leitura = await cliente.Http.GetAsync($"/api/grupos/{grupo.Id}");
        Assert.Equal(HttpStatusCode.OK, leitura.StatusCode);
    }
}
