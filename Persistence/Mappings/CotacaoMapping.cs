using Domain.Cotacoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Mappings;

public class CotacaoMapping : IEntityTypeConfiguration<Cotacao>
{
    public void Configure(EntityTypeBuilder<Cotacao> builder)
    {
        builder.ToTable("Cotacoes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("CotacaoId");
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Papel).IsRequired().HasMaxLength(60);
        builder.Property(e => e.Valor).IsRequired().HasPrecision(18, 4);
        builder.Property(e => e.Data).IsRequired().HasColumnType("date");
        builder.HasIndex(e => new { e.UserId, e.Papel, e.Data }).IsUnique()
            .HasDatabaseName("IX_Cotacoes_UserId_Papel_Data");
    }
}
