namespace Rateio.Dominio.Tests;

public class SimplificadorDeDividasTests
{
    [Fact]
    public void Ciclo_de_dividas_some()
    {
        // Ana deve 10 para Bia, Bia deve 10 para Caio, Caio deve 10 para Ana: ninguém precisa pagar nada
        var grupo = new Grupo("Ciclo");
        var ana = grupo.AdicionarParticipante("Ana");
        var bia = grupo.AdicionarParticipante("Bia");
        var caio = grupo.AdicionarParticipante("Caio");
        var dez = new Dinheiro(1000);

        grupo.AdicionarDespesa(new Despesa("a", bia, dez, new Dictionary<Participante, Dinheiro> { [ana] = dez }));
        grupo.AdicionarDespesa(new Despesa("b", caio, dez, new Dictionary<Participante, Dinheiro> { [bia] = dez }));
        grupo.AdicionarDespesa(new Despesa("c", ana, dez, new Dictionary<Participante, Dinheiro> { [caio] = dez }));

        Assert.Empty(grupo.SugerirAcertos());
    }

    [Fact]
    public void Saldos_que_nao_somam_zero_sao_rejeitados()
    {
        var saldos = new Dictionary<Participante, Dinheiro> { [new("Ana")] = new(100), [new("Bia")] = new(-90) };
        Assert.Throws<ArgumentException>(() => SimplificadorDeDividas.Simplificar(saldos));
    }

    /// <summary>
    /// Teste por propriedade: gera centenas de grupos aleatórios (com semente fixa, para ser reproduzível)
    /// e verifica as regras que precisam valer para qualquer grupo.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sementes))]
    public void Propriedades_valem_para_grupos_aleatorios(int semente)
    {
        var aleatorio = new Random(semente);
        var grupo = new Grupo($"Aleatório {semente}");
        var pessoas = aleatorio.Next(2, 12);
        for (var i = 0; i < pessoas; i++)
            grupo.AdicionarParticipante($"Pessoa {i}");

        for (var d = 0; d < aleatorio.Next(1, 25); d++)
        {
            var pagador = grupo.Participantes[aleatorio.Next(pessoas)];
            var envolvidos = grupo.Participantes.Where(_ => aleatorio.NextDouble() < 0.7).ToList();
            if (envolvidos.Count == 0) envolvidos.Add(pagador);
            var valor = new Dinheiro(aleatorio.Next(1, 500_000));
            grupo.AdicionarDespesa(new Despesa($"Despesa {d}", pagador, valor, Dividir.Igual(valor, envolvidos)));
        }

        var saldos = grupo.CalcularSaldos();
        var acertos = grupo.SugerirAcertos();

        // 1. Os saldos sempre somam zero
        Assert.True(saldos.Values.Somar().EhZero);

        // 2. Nunca mais que n − 1 transferências (n = pessoas com saldo diferente de zero)
        var comSaldo = saldos.Values.Count(s => !s.EhZero);
        Assert.True(acertos.Count <= Math.Max(0, comSaldo - 1), $"{acertos.Count} transferências para {comSaldo} pessoas");

        // 3. Toda transferência é positiva e entre pessoas diferentes
        Assert.All(acertos, t => { Assert.True(t.Valor.EhPositivo); Assert.NotEqual(t.De, t.Para); });

        // 4. Quem deve só paga e quem tem a receber só recebe
        Assert.All(acertos, t => { Assert.True(saldos[t.De].EhNegativo); Assert.True(saldos[t.Para].EhPositivo); });

        // 5. Depois de pagar os acertos, todos ficam zerados
        foreach (var t in acertos)
            grupo.RegistrarPagamento(new Pagamento(t.De, t.Para, t.Valor));
        Assert.All(grupo.CalcularSaldos().Values, s => Assert.True(s.EhZero));
    }

    public static TheoryData<int> Sementes()
    {
        var dados = new TheoryData<int>();
        foreach (var semente in Enumerable.Range(1, 300))
            dados.Add(semente);
        return dados;
    }
}
