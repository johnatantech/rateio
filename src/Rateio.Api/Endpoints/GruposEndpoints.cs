using Microsoft.AspNetCore.Http.HttpResults;
using Rateio.Api.Contratos;
using Rateio.Dominio;
using Rateio.Infra;

namespace Rateio.Api.Endpoints;

public static class GruposEndpoints
{
    public static RouteGroupBuilder MapearGrupos(this IEndpointRouteBuilder app)
    {
        var grupos = app.MapGroup("/api/grupos").WithTags("Grupos");

        grupos.MapPost("/", CriarGrupo).WithSummary("Cria um grupo");
        grupos.MapGet("/{grupoId:guid}", ObterGrupo).WithName("ObterGrupo").WithSummary("Dados do grupo e participantes");
        grupos.MapDelete("/{grupoId:guid}", ExcluirGrupo)
            .WithSummary("Exclui o grupo com tudo o que tem nele (exige todos quites)");
        grupos.MapPost("/{grupoId:guid}/nova-rodada", IniciarNovaRodada)
            .WithSummary("Apaga despesas e pagamentos e mantém as pessoas (exige todos quites)");

        grupos.MapPost("/{grupoId:guid}/participantes", AdicionarParticipante).WithTags("Participantes")
            .WithSummary("Adiciona um participante, com chave Pix opcional");

        grupos.MapGet("/{grupoId:guid}/despesas", ListarDespesas).WithTags("Despesas").WithSummary("Lista as despesas");
        grupos.MapPost("/{grupoId:guid}/despesas", AdicionarDespesa).WithTags("Despesas")
            .WithSummary("Lança uma despesa dividida em partes iguais, por valores, percentual ou cotas");
        grupos.MapDelete("/{grupoId:guid}/despesas/{despesaId:guid}", RemoverDespesa).WithTags("Despesas")
            .WithSummary("Remove uma despesa");

        grupos.MapGet("/{grupoId:guid}/saldos", ObterSaldos).WithTags("Acertos").WithSummary("Saldo de cada participante");
        grupos.MapGet("/{grupoId:guid}/acertos", SugerirAcertos).WithTags("Acertos")
            .WithSummary("Transferências mínimas para zerar o grupo, com Pix copia e cola");
        grupos.MapGet("/{grupoId:guid}/pagamentos", ListarPagamentos).WithTags("Acertos").WithSummary("Pagamentos registrados");
        grupos.MapPost("/{grupoId:guid}/pagamentos", RegistrarPagamento).WithTags("Acertos")
            .WithSummary("Marca um acerto como pago");

        return grupos;
    }

    // ------------------------------------------------------------ grupos

    private static async Task<CreatedAtRoute<GrupoResposta>> CriarGrupo(
        CriarGrupoRequisicao requisicao, RepositorioDeGrupos repositorio, CancellationToken ct)
    {
        var carregado = await repositorio.CriarAsync(requisicao.Nome, requisicao.Cidade, ct);
        return TypedResults.CreatedAtRoute(ParaResposta(carregado), "ObterGrupo", new { grupoId = carregado.Grupo.Id });
    }

    private static async Task<Results<Ok<GrupoResposta>, NotFound>> ObterGrupo(
        Guid grupoId, RepositorioDeGrupos repositorio, CancellationToken ct) =>
        await repositorio.CarregarAsync(grupoId, ct) is { } carregado
            ? TypedResults.Ok(ParaResposta(carregado))
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound>> ExcluirGrupo(
        Guid grupoId, RepositorioDeGrupos repositorio, CancellationToken ct)
    {
        if (await repositorio.CarregarAsync(grupoId, ct) is not { } carregado)
            return TypedResults.NotFound();
        await repositorio.ExcluirAsync(carregado, ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<GrupoResposta>, NotFound>> IniciarNovaRodada(
        Guid grupoId, RepositorioDeGrupos repositorio, CancellationToken ct)
    {
        if (await repositorio.CarregarAsync(grupoId, ct) is not { } carregado)
            return TypedResults.NotFound();
        await repositorio.IniciarNovaRodadaAsync(carregado, ct);
        return TypedResults.Ok(ParaResposta(carregado));
    }

    // ------------------------------------------------------------ participantes

    private static async Task<Results<Created<ParticipanteResposta>, NotFound>> AdicionarParticipante(
        Guid grupoId, AdicionarParticipanteRequisicao requisicao, RepositorioDeGrupos repositorio, CancellationToken ct)
    {
        if (await repositorio.CarregarAsync(grupoId, ct) is not { } carregado)
            return TypedResults.NotFound();

        ChavePix? chave = null;
        if (!string.IsNullOrWhiteSpace(requisicao.ChavePix))
        {
            chave = requisicao.TipoChavePix is { } tipo
                ? ChavePix.Criar(tipo, requisicao.ChavePix)
                : ChavePix.Identificar(requisicao.ChavePix)
                  ?? throw new ArgumentException(
                      "Não foi possível identificar o tipo da chave Pix. Informe o campo tipoChavePix " +
                      "(telefone precisa começar com +55).", nameof(requisicao.ChavePix));
        }

        var participante = await repositorio.AdicionarParticipanteAsync(carregado, requisicao.Nome, chave, ct);
        return TypedResults.Created($"/api/grupos/{grupoId}", ParticipanteResposta.De(participante));
    }

    // ------------------------------------------------------------ despesas

    private static async Task<Results<Ok<List<DespesaResposta>>, NotFound>> ListarDespesas(
        Guid grupoId, RepositorioDeGrupos repositorio, CancellationToken ct) =>
        await repositorio.CarregarAsync(grupoId, ct) is { } carregado
            ? TypedResults.Ok(carregado.Grupo.Despesas.Select(DespesaResposta.De).ToList())
            : TypedResults.NotFound();

    private static async Task<Results<Created<DespesaResposta>, NotFound>> AdicionarDespesa(
        Guid grupoId, AdicionarDespesaRequisicao requisicao, RepositorioDeGrupos repositorio, CancellationToken ct)
    {
        if (await repositorio.CarregarAsync(grupoId, ct) is not { } carregado)
            return TypedResults.NotFound();

        var grupo = carregado.Grupo;
        var pagador = Participante(grupo, requisicao.PagoPorId);
        var valor = Dinheiro.DeReais(requisicao.Valor);
        var divisao = MontarDivisao(grupo, valor, requisicao.Divisao);

        var despesa = new Despesa(requisicao.Descricao, pagador, valor, divisao, requisicao.Data, requisicao.Categoria);
        await repositorio.AdicionarDespesaAsync(carregado, despesa, ct);

        return TypedResults.Created($"/api/grupos/{grupoId}/despesas/{despesa.Id}", DespesaResposta.De(despesa));
    }

    private static async Task<Results<NoContent, NotFound>> RemoverDespesa(
        Guid grupoId, Guid despesaId, RepositorioDeGrupos repositorio, CancellationToken ct) =>
        await repositorio.RemoverDespesaAsync(grupoId, despesaId, ct) ? TypedResults.NoContent() : TypedResults.NotFound();

    // ------------------------------------------------------------ saldos, acertos e pagamentos

    private static async Task<Results<Ok<List<SaldoResposta>>, NotFound>> ObterSaldos(
        Guid grupoId, RepositorioDeGrupos repositorio, CancellationToken ct)
    {
        if (await repositorio.CarregarAsync(grupoId, ct) is not { } carregado)
            return TypedResults.NotFound();

        var saldos = carregado.Grupo.CalcularSaldos()
            .Select(s => new SaldoResposta(s.Key.Id, s.Key.Nome, s.Value.Reais,
                s.Value.EhPositivo ? SituacaoSaldo.AReceber : s.Value.EhNegativo ? SituacaoSaldo.APagar : SituacaoSaldo.Quite))
            .ToList();
        return TypedResults.Ok(saldos);
    }

    private static async Task<Results<Ok<List<AcertoResposta>>, NotFound>> SugerirAcertos(
        Guid grupoId, RepositorioDeGrupos repositorio, CancellationToken ct)
    {
        if (await repositorio.CarregarAsync(grupoId, ct) is not { } carregado)
            return TypedResults.NotFound();

        var acertos = carregado.Grupo.SugerirAcertos()
            .Select(t => t.Para.Chave is null
                ? new AcertoResposta(Pessoa(t.De), Pessoa(t.Para), t.Valor.Reais, null,
                    $"{t.Para.Nome} não cadastrou chave Pix. Combine o pagamento diretamente.")
                : new AcertoResposta(Pessoa(t.De), Pessoa(t.Para), t.Valor.Reais,
                    PixCopiaECola.GerarParaTransferencia(t, carregado.Cidade, $"Rateio {carregado.Grupo.Nome}"), null))
            .ToList();
        return TypedResults.Ok(acertos);
    }

    private static async Task<Results<Ok<List<PagamentoResposta>>, NotFound>> ListarPagamentos(
        Guid grupoId, RepositorioDeGrupos repositorio, CancellationToken ct) =>
        await repositorio.CarregarAsync(grupoId, ct) is { } carregado
            ? TypedResults.Ok(carregado.Grupo.Pagamentos.Select(PagamentoParaResposta).ToList())
            : TypedResults.NotFound();

    private static async Task<Results<Created<PagamentoResposta>, NotFound>> RegistrarPagamento(
        Guid grupoId, RegistrarPagamentoRequisicao requisicao, RepositorioDeGrupos repositorio, CancellationToken ct)
    {
        if (await repositorio.CarregarAsync(grupoId, ct) is not { } carregado)
            return TypedResults.NotFound();

        var grupo = carregado.Grupo;
        var pagamento = new Pagamento(
            Participante(grupo, requisicao.DeId), Participante(grupo, requisicao.ParaId),
            Dinheiro.DeReais(requisicao.Valor), requisicao.Data ?? DateOnly.FromDateTime(DateTime.Today));

        await repositorio.RegistrarPagamentoAsync(carregado, pagamento, ct);
        return TypedResults.Created($"/api/grupos/{grupoId}/pagamentos", PagamentoParaResposta(pagamento));
    }

    // ------------------------------------------------------------ auxiliares

    private static IReadOnlyDictionary<Participante, Dinheiro> MontarDivisao(Grupo grupo, Dinheiro valor, DivisaoRequisicao divisao)
    {
        var partes = divisao.Participantes ?? [];
        if (partes.Select(p => p.ParticipanteId).Distinct().Count() != partes.Count)
            throw new ArgumentException("Um participante aparece mais de uma vez na divisão.", nameof(divisao));

        return divisao.Tipo switch
        {
            TipoDivisao.Igual => Dividir.Igual(valor,
                partes.Count == 0 ? grupo.Participantes : partes.Select(p => Participante(grupo, p.ParticipanteId)).ToList()),

            TipoDivisao.Valores => Dividir.PorValores(valor, Exigir(partes, "valor").ToDictionary(
                p => Participante(grupo, p.ParticipanteId),
                p => Dinheiro.DeReais(p.Valor ?? throw ParteIncompleta("valor")))),

            TipoDivisao.Percentual => Dividir.PorPercentual(valor, Exigir(partes, "percentual").ToDictionary(
                p => Participante(grupo, p.ParticipanteId),
                p => p.Percentual ?? throw ParteIncompleta("percentual"))),

            TipoDivisao.Cotas => Dividir.PorCotas(valor, Exigir(partes, "cotas").ToDictionary(
                p => Participante(grupo, p.ParticipanteId),
                p => p.Cotas ?? throw ParteIncompleta("cotas"))),

            _ => throw new ArgumentException("Tipo de divisão desconhecido.", nameof(divisao)),
        };
    }

    private static IReadOnlyList<ParteDaDivisao> Exigir(IReadOnlyList<ParteDaDivisao> partes, string campo) =>
        partes.Count > 0 ? partes : throw new ArgumentException($"Informe os participantes e o campo {campo} de cada um.");

    private static ArgumentException ParteIncompleta(string campo) =>
        new($"Todos os participantes da divisão precisam do campo {campo}.");

    private static Participante Participante(Grupo grupo, Guid id) =>
        grupo.BuscarParticipante(id) ?? throw new ArgumentException($"O participante {id} não pertence ao grupo.");

    private static PessoaResposta Pessoa(Participante p) => new(p.Id, p.Nome);

    private static PagamentoResposta PagamentoParaResposta(Pagamento p) =>
        new(p.Id, Pessoa(p.De), Pessoa(p.Para), p.Valor.Reais, p.Data);

    private static GrupoResposta ParaResposta(GrupoCarregado c) => new(
        c.Grupo.Id, c.Grupo.Nome, c.Cidade, c.CriadoEm, c.Grupo.TotalGasto.Reais, c.Grupo.Despesas.Count,
        c.Grupo.Participantes.Select(ParticipanteResposta.De).ToList());
}
