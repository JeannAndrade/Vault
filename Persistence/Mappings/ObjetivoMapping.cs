using Domain.Objetivos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Mappings;

public class ObjetivoMapping : IEntityTypeConfiguration<Objetivo>
{
    public void Configure(EntityTypeBuilder<Objetivo> builder)
    {
        builder.ToTable("Objetivos");
        builder.HasKey(k => k.Id);
        builder.Property(e => e.Id).HasColumnName("ObjetivoId");
        builder.Property(e => e.Nome).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Descricao).HasMaxLength(500);
        builder.Property(e => e.FontePagadora).HasMaxLength(100).IsRequired();
        builder.Property(e => e.OndeAplicar).HasMaxLength(100).IsRequired();
        builder.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
        builder.HasIndex(e => e.UserId);

        builder
            .HasOne(e => e.User)
            .WithMany(c => c.Objetivos)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
