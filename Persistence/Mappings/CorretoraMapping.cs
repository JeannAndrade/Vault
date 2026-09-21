using Domain.Corretoras;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Mappings;

public class CorretoraMapping : IEntityTypeConfiguration<Corretora>
{
    public void Configure(EntityTypeBuilder<Corretora> builder)
    {
        builder.ToTable("Corretoras");
        builder.HasKey(k => k.Id);
        builder.Property(e => e.Id).HasColumnName("CorretoraId");
        builder.Property(e => e.Nome).HasMaxLength(60).IsRequired();
        builder.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
        builder.HasIndex(e => e.UserId);

        builder
            .HasOne(e => e.User)
            .WithMany(c => c.Corretoras)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
