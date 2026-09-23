using Domain.Movimentos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Mappings;

public class MovimentoMapping : IEntityTypeConfiguration<Movimento>
{
    public void Configure(EntityTypeBuilder<Movimento> builder)
    {
        builder.ToTable("Movimentos");
        builder.HasKey(k => k.Id);
        builder.Property(e => e.Id).HasColumnName("MovimentoId");
        builder.Property(e => e.RentabilidadeContratada).HasMaxLength(60);
        builder.Property(e => e.CotacaoNaCompra).HasMaxLength(60);
        builder.Property(e => e.DataInvestimento).IsRequired();
        builder.Property(e => e.ValorAporte).IsRequired().HasPrecision(18, 2);
        builder.Property(e => e.EhReinvestimento).IsRequired();
        builder.Property(e => e.EstaAtivo).IsRequired();
        builder.Property(e => e.ValorLiquidoAtual).IsRequired().HasPrecision(18, 2);
        builder.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(e => e.ObjetivoId).HasColumnName("ObjetivoId").IsRequired();
        builder.Property(e => e.TipoRendaId).HasColumnName("TipoRendaId").IsRequired();
        builder.Property(e => e.CorretoraId).HasColumnName("CorretoraId").IsRequired();
        builder.Property(e => e.ProdutoId).HasColumnName("ProdutoId").IsRequired();
        builder.Property(e => e.EmissorId).HasColumnName("EmissorId").IsRequired();

        // Índices compostos — sempre começando por UserId
        builder.HasIndex(e => new { e.UserId, e.TipoRendaId });
        builder.HasIndex(e => new { e.UserId, e.CorretoraId });
        builder.HasIndex(e => new { e.UserId, e.ProdutoId });
        builder.HasIndex(e => new { e.UserId, e.EmissorId });
        builder.HasIndex(e => new { e.UserId, e.ObjetivoId });


        // ---------- Relacionamentos ----------

        builder
            .HasOne(e => e.User)
            .WithMany(c => c.Movimentos)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Objetivo)
            .WithMany(c => c.Movimentos)
            .HasForeignKey(e => e.ObjetivoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.TipoRenda)
            .WithMany(c => c.Movimentos)
            .HasForeignKey(e => e.TipoRendaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.Corretora)
            .WithMany(c => c.Movimentos)
            .HasForeignKey(e => e.CorretoraId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.Produto)
            .WithMany(c => c.Movimentos)
            .HasForeignKey(e => e.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.Emissor)
            .WithMany(c => c.Movimentos)
            .HasForeignKey(e => e.EmissorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
