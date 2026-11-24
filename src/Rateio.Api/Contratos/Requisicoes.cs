using Rateio.Dominio;

namespace Rateio.Api.Contratos;

// Valores monetários trafegam em reais com até 2 casas (ex.: 33.34).
// Na entrada são convertidos para centavos inteiros; frações de centavo são recusadas.

public sealed record CriarGrupoRequisicao(string Nome, string Cidade);

public sealed record AdicionarParticipanteRequisicao(string Nome, string? ChavePix = null, TipoChavePix? TipoChavePix = null);

public enum TipoDivisao { Igual, Valores, Percentual, Cotas }

public sealed record ParteDaDivisao(Guid ParticipanteId, decimal? Valor = null, decimal? Percentual = null, int? Cotas = null);

/// <param name="Participantes">Na divisão igual, lista vazia significa todos do grupo.</param>
public sealed record DivisaoRequisicao(TipoDivisao Tipo, IReadOnlyList<ParteDaDivisao>? Participantes = null);

public sealed record AdicionarDespesaRequisicao(
    string Descricao,
    Guid PagoPorId,
    decimal Valor,
    DivisaoRequisicao Divisao,
    DateOnly? Data = null,
    string? Categoria = null);

public sealed record RegistrarPagamentoRequisicao(Guid DeId, Guid ParaId, decimal Valor, DateOnly? Data = null);
