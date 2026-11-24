namespace Rateio.Dominio;

/// <summary>
/// Formas de dividir uma despesa. Todas garantem que a soma das partes é exatamente o total:
/// R$ 100,00 / 3 = R$ 33,34 + R$ 33,33 + R$ 33,33.
/// </summary>
public static class Dividir
{
    /// <summary>Partes iguais entre os participantes.</summary>
    public static IReadOnlyDictionary<Participante, Dinheiro> Igual(Dinheiro total, IReadOnlyList<Participante> participantes) =>
        PorCotas(total, participantes.ToDictionary(p => p, _ => 1));

    /// <summary>Proporcional a cotas inteiras (ex.: casal = 2, solteiro = 1).</summary>
    public static IReadOnlyDictionary<Participante, Dinheiro> PorCotas(Dinheiro total, IReadOnlyDictionary<Participante, int> cotas)
    {
        if (cotas.Values.Any(c => c < 0))
            throw new ArgumentException("Cotas não podem ser negativas.", nameof(cotas));
        return Proporcional(total, cotas.ToDictionary(c => c.Key, c => (decimal)c.Value));
    }

    /// <summary>Por percentual. A soma dos percentuais precisa ser exatamente 100.</summary>
    public static IReadOnlyDictionary<Participante, Dinheiro> PorPercentual(Dinheiro total, IReadOnlyDictionary<Participante, decimal> percentuais)
    {
        if (percentuais.Values.Any(p => p < 0))
            throw new ArgumentException("Percentuais não podem ser negativos.", nameof(percentuais));
        var soma = percentuais.Values.Sum();
        if (soma != 100m)
            throw new ArgumentException($"Os percentuais somam {soma}%, e precisam somar 100%.", nameof(percentuais));
        return Proporcional(total, percentuais);
    }

    /// <summary>Valores informados por pessoa. A soma precisa bater com o total da despesa.</summary>
    public static IReadOnlyDictionary<Participante, Dinheiro> PorValores(Dinheiro total, IReadOnlyDictionary<Participante, Dinheiro> valores)
    {
        if (valores.Values.Any(v => v.EhNegativo))
            throw new ArgumentException("Valores não podem ser negativos.", nameof(valores));
        var soma = valores.Values.Somar();
        if (soma != total)
            throw new ArgumentException($"Os valores somam {soma}, mas a despesa é de {total}.", nameof(valores));
        return valores;
    }

    /// <summary>
    /// Método dos maiores restos: cada um recebe a parte inteira em centavos,
    /// e os centavos que sobram vão para quem tem a maior fração. Empates seguem a ordem informada,
    /// o que torna o resultado determinístico.
    /// </summary>
    private static Dictionary<Participante, Dinheiro> Proporcional(Dinheiro total, IReadOnlyDictionary<Participante, decimal> pesos)
    {
        if (pesos.Count == 0)
            throw new ArgumentException("Informe ao menos um participante.", nameof(pesos));
        if (total.EhNegativo)
            throw new ArgumentException("O total não pode ser negativo.", nameof(total));

        var somaPesos = pesos.Values.Sum();
        if (somaPesos <= 0)
            throw new ArgumentException("A soma dos pesos precisa ser maior que zero.", nameof(pesos));

        var partes = pesos
            .Select((p, ordem) =>
            {
                var exato = total.Centavos * p.Value / somaPesos;
                var inteiro = (long)decimal.Floor(exato);
                return (p.Key, Inteiro: inteiro, Fracao: exato - inteiro, Ordem: ordem);
            })
            .ToList();

        var sobra = total.Centavos - partes.Sum(p => p.Inteiro);
        var recebemSobra = partes
            .OrderByDescending(p => p.Fracao)
            .ThenBy(p => p.Ordem)
            .Take((int)sobra)
            .Select(p => p.Key)
            .ToHashSet();

        return partes.ToDictionary(
            p => p.Key,
            p => new Dinheiro(p.Inteiro + (recebemSobra.Contains(p.Key) ? 1 : 0)));
    }
}
