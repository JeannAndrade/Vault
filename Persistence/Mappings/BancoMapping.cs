using Domain.Bancos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Mappings;

public class BancoMapping : IEntityTypeConfiguration<Banco>
{
    public void Configure(EntityTypeBuilder<Banco> builder)
    {
        builder.ToTable("Bancos");
        builder.HasKey(k => k.Id);
        builder.Property(e => e.Id).HasColumnName("BancoId");
        builder.Property(e => e.Nome).HasMaxLength(60).IsRequired();
        builder.Property(e => e.UserId).HasColumnName("UserId");

        builder
                .HasOne(e => e.User)
                .WithMany(c => c.Bancos)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
    }
}
