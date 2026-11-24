namespace Rateio.Dominio;

/// <summary>Grupo de pessoas que dividem despesas (casa, viagem, churrasco).</summary>
public sealed class Grupo
{
    private readonly List<Participante> _participantes = [];
    private readonly List<Despesa> _despesas = [];
    private readonly List<Pagamento> _pagamentos = [];

    public Guid Id { get; }
    public string Nome { get; }
    public IReadOnlyList<Participante> Participantes => _participantes;
    public IReadOnlyList<Despesa> Despesas => _despesas;
    public IReadOnlyList<Pagamento> Pagamentos => _pagamentos;

    public Grupo(string nome, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do grupo é obrigatório.", nameof(nome));
        Id = id ?? Guid.NewGuid();
        Nome = nome.Trim();
    }

    public Participante AdicionarParticipante(string nome, ChavePix? chave = null) =>
        AdicionarParticipante(new Participante(nome, chave));

    /// <summary>Adiciona um participante já existente (ex.: ao reconstruir o grupo a partir do banco).</summary>
    public Participante AdicionarParticipante(Participante participante)
    {
        if (_participantes.Any(p => p.Nome.Equals(participante.Nome, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Já existe um participante chamado {participante.Nome} no grupo.");
        if (_participantes.Any(p => p.Id == participante.Id))
            throw new InvalidOperationException("Participante já adicionado ao grupo.");
        _participantes.Add(participante);
        return participante;
    }

    public Participante? BuscarParticipante(Guid id) => _participantes.FirstOrDefault(p => p.Id == id);

    public void AdicionarDespesa(Despesa despesa)
    {
        GarantirMembro(despesa.PagoPor);
        foreach (var participante in despesa.Divisao.Keys)
            GarantirMembro(participante);
        _despesas.Add(despesa);
    }

    public bool RemoverDespesa(Guid despesaId) => _despesas.RemoveAll(d => d.Id == despesaId) > 0;

    public void RegistrarPagamento(Pagamento pagamento)
    {
        GarantirMembro(pagamento.De);
        GarantirMembro(pagamento.Para);
        if (pagamento.De == pagamento.Para)
            throw new ArgumentException("Não é possível registrar pagamento para si mesmo.", nameof(pagamento));
        if (!pagamento.Valor.EhPositivo)
            throw new ArgumentException("O valor do pagamento precisa ser maior que zero.", nameof(pagamento));
        _pagamentos.Add(pagamento);
    }

    /// <summary>
    /// Saldo de cada participante: positivo = tem a receber; negativo = deve.
    /// A soma de todos os saldos é sempre zero.
    /// </summary>
    public IReadOnlyDictionary<Participante, Dinheiro> CalcularSaldos()
    {
        var saldos = _participantes.ToDictionary(p => p, _ => Dinheiro.Zero);

        foreach (var despesa in _despesas)
        {
            saldos[despesa.PagoPor] += despesa.Valor;
            foreach (var (participante, parte) in despesa.Divisao)
                saldos[participante] -= parte;
        }

        foreach (var pagamento in _pagamentos)
        {
            saldos[pagamento.De] += pagamento.Valor;
            saldos[pagamento.Para] -= pagamento.Valor;
        }

        return saldos;
    }

    /// <summary>Transferências mínimas sugeridas para zerar o grupo.</summary>
    public IReadOnlyList<Transferencia> SugerirAcertos() => SimplificadorDeDividas.Simplificar(CalcularSaldos());

    public Dinheiro TotalGasto => _despesas.Select(d => d.Valor).Somar();

    private void GarantirMembro(Participante participante)
    {
        if (!_participantes.Contains(participante))
            throw new InvalidOperationException($"{participante.Nome} não faz parte do grupo {Nome}.");
    }
}
