using Rateio.Dominio;

namespace Rateio.Api.Contratos;

public sealed record PessoaResposta(Guid Id, string Nome);

/// <summary>A chave Pix é exibida mascarada; o valor completo só aparece dentro do Pix copia e cola do acerto.</summary>
public sealed record ParticipanteResposta(Guid Id, string Nome, TipoChavePix? TipoChavePix, string? ChavePixMascarada)
{
    public static ParticipanteResposta De(Participante p) => new(p.Id, p.Nome, p.Chave?.Tipo, p.Chave?.Mascarada());
}

public sealed record GrupoResposta(
    Guid Id, string Nome, string Cidade, DateTimeOffset CriadoEm,
    decimal TotalGasto, int QuantidadeDespesas, IReadOnlyList<ParticipanteResposta> Participantes);

public sealed record ParteResposta(Guid ParticipanteId, string Nome, decimal Valor);

public sealed record DespesaResposta(
    Guid Id, string Descricao, PessoaResposta PagoPor, decimal Valor,
    DateOnly Data, string? Categoria, IReadOnlyList<ParteResposta> Divisao)
{
    public static DespesaResposta De(Despesa d) => new(
        d.Id, d.Descricao, new PessoaResposta(d.PagoPor.Id, d.PagoPor.Nome), d.Valor.Reais, d.Data, d.Categoria,
        d.Divisao.Select(x => new ParteResposta(x.Key.Id, x.Key.Nome, x.Value.Reais)).ToList());
}

public enum SituacaoSaldo { AReceber, APagar, Quite }

public sealed record SaldoResposta(Guid ParticipanteId, string Nome, decimal Saldo, SituacaoSaldo Situacao);

/// <param name="PixCopiaECola">Nulo quando quem recebe não cadastrou chave Pix (veja Aviso).</param>
public sealed record AcertoResposta(PessoaResposta De, PessoaResposta Para, decimal Valor, string? PixCopiaECola, string? Aviso);

public sealed record PagamentoResposta(Guid Id, PessoaResposta De, PessoaResposta Para, decimal Valor, DateOnly Data);
