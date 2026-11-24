namespace Rateio.Infra.Persistencia;

// Modelos de persistência: espelham as tabelas. O domínio (Rateio.Dominio) não conhece o banco;
// o RepositorioDeGrupos converte entre os dois mundos.

public sealed class GrupoRegistro
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string Cidade { get; set; } = "";
    public DateTimeOffset CriadoEm { get; set; }

    public List<ParticipanteRegistro> Participantes { get; set; } = [];
    public List<DespesaRegistro> Despesas { get; set; } = [];
    public List<PagamentoRegistro> Pagamentos { get; set; } = [];
}

public sealed class ParticipanteRegistro
{
    public Guid Id { get; set; }
    public Guid GrupoId { get; set; }
    public string Nome { get; set; } = "";
    public string? TipoChavePix { get; set; }
    public string? ChavePix { get; set; }
    /// <summary>Ordem de entrada no grupo: mantém o rateio e os acertos determinísticos.</summary>
    public int Ordem { get; set; }
}

public sealed class DespesaRegistro
{
    public Guid Id { get; set; }
    public Guid GrupoId { get; set; }
    public string Descricao { get; set; } = "";
    public Guid PagoPorId { get; set; }
    public long ValorCentavos { get; set; }
    public DateOnly Data { get; set; }
    public string? Categoria { get; set; }
    public DateTimeOffset CriadoEm { get; set; }

    public List<DivisaoRegistro> Divisao { get; set; } = [];
}

public sealed class DivisaoRegistro
{
    public Guid DespesaId { get; set; }
    public Guid ParticipanteId { get; set; }
    public long ValorCentavos { get; set; }
}

public sealed class PagamentoRegistro
{
    public Guid Id { get; set; }
    public Guid GrupoId { get; set; }
    public Guid DeId { get; set; }
    public Guid ParaId { get; set; }
    public long ValorCentavos { get; set; }
    public DateOnly Data { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
