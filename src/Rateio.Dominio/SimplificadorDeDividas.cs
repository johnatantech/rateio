namespace Rateio.Dominio;

/// <summary>
/// Reduz as dívidas de um grupo a poucas transferências.
///
/// Algoritmo guloso: casa repetidamente o maior devedor com o maior credor e transfere o menor
/// dos dois valores. A cada passo ao menos uma pessoa é zerada, então são no máximo n − 1
/// transferências para n pessoas com saldo. Encontrar o mínimo absoluto é NP-difícil
/// (equivale a particionar o grupo em subconjuntos de soma zero); o guloso é rápido,
/// determinístico e na prática chega ao ótimo ou muito perto dele.
/// </summary>
public static class SimplificadorDeDividas
{
    public static IReadOnlyList<Transferencia> Simplificar(IReadOnlyDictionary<Participante, Dinheiro> saldos)
    {
        var soma = saldos.Values.Somar();
        if (!soma.EhZero)
            throw new ArgumentException($"Os saldos precisam somar zero, mas somam {soma}.", nameof(saldos));

        // Ordem estável: em empate de valor, vale a ordem original dos participantes
        var ordem = saldos.Keys.Select((p, i) => (p, i)).ToDictionary(x => x.p, x => x.i);

        var credores = new PriorityQueue<Participante, (long, int)>();
        var devedores = new PriorityQueue<Participante, (long, int)>();
        var restante = new Dictionary<Participante, long>();

        foreach (var (participante, saldo) in saldos)
        {
            if (saldo.EhZero) continue;
            restante[participante] = Math.Abs(saldo.Centavos);
            var fila = saldo.EhPositivo ? credores : devedores;
            fila.Enqueue(participante, (-Math.Abs(saldo.Centavos), ordem[participante]));
        }

        var transferencias = new List<Transferencia>();
        while (credores.Count > 0 && devedores.Count > 0)
        {
            var credor = credores.Dequeue();
            var devedor = devedores.Dequeue();
            var valor = Math.Min(restante[credor], restante[devedor]);

            transferencias.Add(new Transferencia(devedor, credor, new Dinheiro(valor)));
            restante[credor] -= valor;
            restante[devedor] -= valor;

            if (restante[credor] > 0) credores.Enqueue(credor, (-restante[credor], ordem[credor]));
            if (restante[devedor] > 0) devedores.Enqueue(devedor, (-restante[devedor], ordem[devedor]));
        }

        return transferencias;
    }
}
