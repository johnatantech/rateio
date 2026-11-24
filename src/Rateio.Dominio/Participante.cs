namespace Rateio.Dominio;

public sealed record Participante
{
    public Guid Id { get; }
    public string Nome { get; }
    public ChavePix? Chave { get; }

    public Participante(string nome, ChavePix? chave = null, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do participante é obrigatório.", nameof(nome));

        Id = id ?? Guid.NewGuid();
        Nome = nome.Trim();
        Chave = chave;
    }

    public override string ToString() => Nome;
}
