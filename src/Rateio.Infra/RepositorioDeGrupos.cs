using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Rateio.Dominio;
using Rateio.Infra.Persistencia;

namespace Rateio.Infra;

/// <summary>Grupo reconstruído do banco, com os dados que só existem na persistência.</summary>
public sealed record GrupoCarregado(Grupo Grupo, string Cidade, DateTimeOffset CriadoEm);

/// <summary>
/// Ponte entre o domínio e o banco. Toda alteração segue o mesmo caminho:
/// carrega o grupo, aplica a operação no domínio (que valida as regras) e só então grava.
/// Assim nenhuma regra de negócio fica duplicada em SQL ou na API.
/// </summary>
public sealed class RepositorioDeGrupos(RateioDbContext db, TimeProvider relogio)
{
    public async Task<GrupoCarregado?> CarregarAsync(Guid grupoId, CancellationToken ct = default)
    {
        var registro = await db.Grupos
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Participantes)
            .Include(g => g.Despesas).ThenInclude(d => d.Divisao)
            .Include(g => g.Pagamentos)
            .FirstOrDefaultAsync(g => g.Id == grupoId, ct);

        return registro is null ? null : Reconstruir(registro);
    }

    public async Task<GrupoCarregado> CriarAsync(string nome, string cidade, CancellationToken ct = default)
    {
        var grupo = new Grupo(nome);
        if (string.IsNullOrWhiteSpace(cidade))
            throw new ArgumentException("A cidade é obrigatória (ela vai no Pix copia e cola).", nameof(cidade));

        var registro = new GrupoRegistro { Id = grupo.Id, Nome = grupo.Nome, Cidade = cidade.Trim(), CriadoEm = relogio.GetUtcNow() };
        db.Grupos.Add(registro);
        await db.SaveChangesAsync(ct);
        return new GrupoCarregado(grupo, registro.Cidade, registro.CriadoEm);
    }

    public async Task<Participante> AdicionarParticipanteAsync(GrupoCarregado carregado, string nome, ChavePix? chave, CancellationToken ct = default)
    {
        var participante = carregado.Grupo.AdicionarParticipante(nome, chave);

        db.Participantes.Add(new ParticipanteRegistro
        {
            Id = participante.Id,
            GrupoId = carregado.Grupo.Id,
            Nome = participante.Nome,
            TipoChavePix = participante.Chave?.Tipo.ToString(),
            ChavePix = participante.Chave?.Valor,
            Ordem = carregado.Grupo.Participantes.Count,
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException erro) when (ViolouUnicidade(erro))
        {
            // Duas requisições simultâneas com o mesmo nome: o índice único do banco é a última barreira
            throw new InvalidOperationException($"Já existe um participante chamado {participante.Nome} no grupo.");
        }
        return participante;
    }

    public async Task AdicionarDespesaAsync(GrupoCarregado carregado, Despesa despesa, CancellationToken ct = default)
    {
        carregado.Grupo.AdicionarDespesa(despesa);

        db.Despesas.Add(new DespesaRegistro
        {
            Id = despesa.Id,
            GrupoId = carregado.Grupo.Id,
            Descricao = despesa.Descricao,
            PagoPorId = despesa.PagoPor.Id,
            ValorCentavos = despesa.Valor.Centavos,
            Data = despesa.Data,
            Categoria = despesa.Categoria,
            CriadoEm = relogio.GetUtcNow(),
            Divisao = despesa.Divisao
                .Select(d => new DivisaoRegistro { DespesaId = despesa.Id, ParticipanteId = d.Key.Id, ValorCentavos = d.Value.Centavos })
                .ToList(),
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> RemoverDespesaAsync(Guid grupoId, Guid despesaId, CancellationToken ct = default) =>
        await db.Despesas.Where(d => d.GrupoId == grupoId && d.Id == despesaId).ExecuteDeleteAsync(ct) > 0;

    public async Task RegistrarPagamentoAsync(GrupoCarregado carregado, Pagamento pagamento, CancellationToken ct = default)
    {
        carregado.Grupo.RegistrarPagamento(pagamento);

        db.Pagamentos.Add(new PagamentoRegistro
        {
            Id = pagamento.Id,
            GrupoId = carregado.Grupo.Id,
            DeId = pagamento.De.Id,
            ParaId = pagamento.Para.Id,
            ValorCentavos = pagamento.Valor.Centavos,
            Data = pagamento.Data,
            CriadoEm = relogio.GetUtcNow(),
        });
        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------ reconstrução do domínio

    private static GrupoCarregado Reconstruir(GrupoRegistro registro)
    {
        var grupo = new Grupo(registro.Nome, registro.Id);

        foreach (var p in registro.Participantes.OrderBy(p => p.Ordem))
        {
            var chave = p.ChavePix is not null && Enum.TryParse<TipoChavePix>(p.TipoChavePix, out var tipo)
                ? ChavePix.Criar(tipo, p.ChavePix)
                : null;
            grupo.AdicionarParticipante(new Participante(p.Nome, chave, p.Id));
        }

        var porId = grupo.Participantes.ToDictionary(p => p.Id);

        foreach (var d in registro.Despesas.OrderBy(d => d.Data).ThenBy(d => d.CriadoEm))
        {
            // Mantém a ordem dos participantes do grupo na divisão
            var divisao = d.Divisao
                .OrderBy(x => grupo.Participantes.ToList().FindIndex(p => p.Id == x.ParticipanteId))
                .ToDictionary(x => porId[x.ParticipanteId], x => new Dinheiro(x.ValorCentavos));
            grupo.AdicionarDespesa(new Despesa(d.Descricao, porId[d.PagoPorId], new Dinheiro(d.ValorCentavos),
                divisao, d.Data, d.Categoria, d.Id));
        }

        foreach (var p in registro.Pagamentos.OrderBy(p => p.CriadoEm))
            grupo.RegistrarPagamento(new Pagamento(porId[p.DeId], porId[p.ParaId], new Dinheiro(p.ValorCentavos), p.Data) { Id = p.Id });

        return new GrupoCarregado(grupo, registro.Cidade, registro.CriadoEm);
    }

    private static bool ViolouUnicidade(DbUpdateException erro) =>
        erro.InnerException is SqlException { Number: 2601 or 2627 };
}
