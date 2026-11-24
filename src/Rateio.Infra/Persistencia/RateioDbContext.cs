using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Rateio.Infra.Persistencia;

/// <summary>
/// Mesmo modelo para SQL Server (desenvolvimento e testes) e PostgreSQL (demo pública).
/// As migrations de cada banco ficam em projetos separados: Rateio.Infra e Rateio.Infra.Postgres.
/// </summary>
public sealed class RateioDbContext(DbContextOptions<RateioDbContext> options) : DbContext(options)
{
    internal bool EhPostgres => Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";

    /// <summary>Nome de coluna entre colchetes no SQL Server e entre aspas no PostgreSQL.</summary>
    private string Col(string nome) => EhPostgres ? $"\"{nome}\"" : $"[{nome}]";

    public DbSet<GrupoRegistro> Grupos => Set<GrupoRegistro>();
    public DbSet<ParticipanteRegistro> Participantes => Set<ParticipanteRegistro>();
    public DbSet<DespesaRegistro> Despesas => Set<DespesaRegistro>();
    public DbSet<DivisaoRegistro> Divisoes => Set<DivisaoRegistro>();
    public DbSet<PagamentoRegistro> Pagamentos => Set<PagamentoRegistro>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<GrupoRegistro>(e =>
        {
            e.ToTable("Grupos");
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).ValueGeneratedNever();
            e.Property(g => g.Nome).HasMaxLength(100).IsRequired();
            e.Property(g => g.Cidade).HasMaxLength(60).IsRequired();

            e.HasMany(g => g.Participantes).WithOne().HasForeignKey(p => p.GrupoId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(g => g.Despesas).WithOne().HasForeignKey(d => d.GrupoId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(g => g.Pagamentos).WithOne().HasForeignKey(p => p.GrupoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<ParticipanteRegistro>(e =>
        {
            e.ToTable("Participantes");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Nome).HasMaxLength(60).IsRequired();
            e.Property(p => p.TipoChavePix).HasMaxLength(20);
            e.Property(p => p.ChavePix).HasMaxLength(77);
            // Collation padrão do SQL Server não diferencia maiúsculas: "Ana" e "ana" colidem, como no domínio
            e.HasIndex(p => new { p.GrupoId, p.Nome }).IsUnique();
        });

        modelo.Entity<DespesaRegistro>(e =>
        {
            e.ToTable("Despesas", t => t.HasCheckConstraint("CK_Despesas_Valor", $"{Col(nameof(DespesaRegistro.ValorCentavos))} > 0"));
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).ValueGeneratedNever();
            e.Property(d => d.Descricao).HasMaxLength(120).IsRequired();
            e.Property(d => d.Categoria).HasMaxLength(40);
            e.HasIndex(d => new { d.GrupoId, d.Data });

            e.HasOne<ParticipanteRegistro>().WithMany().HasForeignKey(d => d.PagoPorId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(d => d.Divisao).WithOne().HasForeignKey(x => x.DespesaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<DivisaoRegistro>(e =>
        {
            e.ToTable("DivisoesDespesa", t => t.HasCheckConstraint("CK_DivisoesDespesa_Valor", $"{Col(nameof(DivisaoRegistro.ValorCentavos))} >= 0"));
            e.HasKey(x => new { x.DespesaId, x.ParticipanteId });
            e.HasOne<ParticipanteRegistro>().WithMany().HasForeignKey(x => x.ParticipanteId).OnDelete(DeleteBehavior.NoAction);
        });

        modelo.Entity<PagamentoRegistro>(e =>
        {
            e.ToTable("Pagamentos", t =>
            {
                t.HasCheckConstraint("CK_Pagamentos_Valor", $"{Col(nameof(PagamentoRegistro.ValorCentavos))} > 0");
                t.HasCheckConstraint("CK_Pagamentos_Pessoas", $"{Col(nameof(PagamentoRegistro.DeId))} <> {Col(nameof(PagamentoRegistro.ParaId))}");
            });
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();
            e.HasOne<ParticipanteRegistro>().WithMany().HasForeignKey(p => p.DeId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ParticipanteRegistro>().WithMany().HasForeignKey(p => p.ParaId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}

/// <summary>Cada banco gera um modelo próprio (as constraints usam sintaxe diferente), então o cache considera o provedor.</summary>
public sealed class ChaveDeModeloPorProvedor : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        (context.GetType(), context.Database.ProviderName, designTime);
}
