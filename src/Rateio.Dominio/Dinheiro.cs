using System.Globalization;

namespace Rateio.Dominio;

/// <summary>
/// Valor monetário em centavos inteiros. Nunca usa ponto flutuante:
/// em double, 0,1 + 0,2 não é exatamente 0,3, e centavo perdido em conta dividida gera briga.
/// </summary>
public readonly record struct Dinheiro(long Centavos) : IComparable<Dinheiro>
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static readonly Dinheiro Zero = new(0);

    /// <summary>Cria a partir de reais. Rejeita frações de centavo (ex.: 10,005).</summary>
    public static Dinheiro DeReais(decimal reais)
    {
        var centavos = reais * 100m;
        if (centavos != decimal.Truncate(centavos))
            throw new ArgumentException($"Valor com fração de centavo: {reais}.", nameof(reais));
        return new Dinheiro((long)centavos);
    }

    public decimal Reais => Centavos / 100m;

    public bool EhPositivo => Centavos > 0;
    public bool EhNegativo => Centavos < 0;
    public bool EhZero => Centavos == 0;

    public Dinheiro Abs() => new(Math.Abs(Centavos));

    public static Dinheiro operator +(Dinheiro a, Dinheiro b) => new(checked(a.Centavos + b.Centavos));
    public static Dinheiro operator -(Dinheiro a, Dinheiro b) => new(checked(a.Centavos - b.Centavos));
    public static Dinheiro operator -(Dinheiro a) => new(-a.Centavos);
    public static bool operator <(Dinheiro a, Dinheiro b) => a.Centavos < b.Centavos;
    public static bool operator >(Dinheiro a, Dinheiro b) => a.Centavos > b.Centavos;
    public static bool operator <=(Dinheiro a, Dinheiro b) => a.Centavos <= b.Centavos;
    public static bool operator >=(Dinheiro a, Dinheiro b) => a.Centavos >= b.Centavos;

    public int CompareTo(Dinheiro outro) => Centavos.CompareTo(outro.Centavos);

    /// <summary>Formato brasileiro: R$ 1.234,56.</summary>
    public override string ToString() => Reais.ToString("C2", PtBr);

    /// <summary>Formato do campo de valor do Pix: ponto decimal, sem separador de milhar (1234.56).</summary>
    public string ParaPix() => Reais.ToString("0.00", CultureInfo.InvariantCulture);
}

public static class DinheiroExtensions
{
    public static Dinheiro Somar(this IEnumerable<Dinheiro> valores) =>
        valores.Aggregate(Dinheiro.Zero, (total, valor) => total + valor);
}
