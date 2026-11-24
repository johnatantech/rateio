namespace Rateio.Dominio;

public sealed record Despesa
{
    public Guid Id { get; }
    public string Descricao { get; }
    public Participante PagoPor { get; }
    public Dinheiro Valor { get; }
    public IReadOnlyDictionary<Participante, Dinheiro> Divisao { get; }
    public DateOnly Data { get; }
    public string? Categoria { get; }

    /// <param name="divisao">Quanto cada participante consumiu. Use os métodos de <see cref="Dividir"/>.</param>
    public Despesa(string descricao, Participante pagoPor, Dinheiro valor,
                   IReadOnlyDictionary<Participante, Dinheiro> divisao,
                   DateOnly? data = null, string? categoria = null, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("A descrição é obrigatória.", nameof(descricao));
        if (!valor.EhPositivo)
            throw new ArgumentException("O valor da despesa precisa ser maior que zero.", nameof(valor));
        if (divisao.Count == 0)
            throw new ArgumentException("A despesa precisa ser dividida entre ao menos um participante.", nameof(divisao));
        if (divisao.Values.Any(v => v.EhNegativo))
            throw new ArgumentException("Nenhuma parte da divisão pode ser negativa.", nameof(divisao));

        var soma = divisao.Values.Somar();
        if (soma != valor)
            throw new ArgumentException($"A divisão soma {soma}, mas a despesa é de {valor}.", nameof(divisao));

        Id = id ?? Guid.NewGuid();
        Descricao = descricao.Trim();
        PagoPor = pagoPor;
        Valor = valor;
        Divisao = divisao;
        Data = data ?? DateOnly.FromDateTime(DateTime.Today);
        Categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();
    }
}

/// <summary>Transferência já realizada entre membros do grupo (acerto pago).</summary>
public sealed record Pagamento(Participante De, Participante Para, Dinheiro Valor, DateOnly Data)
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Pagamento(Participante de, Participante para, Dinheiro valor) : this(de, para, valor, DateOnly.FromDateTime(DateTime.Today)) { }
}

/// <summary>Transferência sugerida para zerar os saldos.</summary>
public sealed record Transferencia(Participante De, Participante Para, Dinheiro Valor);
