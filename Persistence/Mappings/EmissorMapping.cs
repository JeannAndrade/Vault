using Domain.Emissores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Mappings;

public class EmissorMapping : IEntityTypeConfiguration<Emissor>
{
  public void Configure(EntityTypeBuilder<Emissor> builder)
  {
    builder.ToTable("Emissores");
    builder.HasKey(k => k.Id);
    builder.Property(e => e.Id).HasColumnName("EmissorId");
    builder.Property(e => e.Nome).HasMaxLength(60).IsRequired();
    builder.Property(e => e.UserId).HasColumnName("UserId").IsRequired();

    builder
        .HasOne(e => e.User)
        .WithMany(c => c.Emissores)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
