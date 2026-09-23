using Domain.TiposRenda;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Mappings;

public class TipoRendaMapping : IEntityTypeConfiguration<TipoRenda>
{
  public void Configure(EntityTypeBuilder<TipoRenda> builder)
  {
    builder.ToTable("TiposRenda");
    builder.HasKey(k => k.Id);
    builder.Property(e => e.Id).HasColumnName("TipoRendaId");
    builder.Property(e => e.Nome).HasMaxLength(60).IsRequired();
    builder.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
    builder.HasIndex(e => e.UserId);

    builder
        .HasOne(e => e.User)
        .WithMany(c => c.TiposRenda)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
