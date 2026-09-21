using Domain.Produtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Mappings;

public class ProdutoMapping : IEntityTypeConfiguration<Produto>
{
  public void Configure(EntityTypeBuilder<Produto> builder)
  {
    builder.ToTable("Produtos");
    builder.HasKey(k => k.Id);
    builder.Property(e => e.Id).HasColumnName("ProdutoId");
    builder.Property(e => e.Nome).HasMaxLength(60).IsRequired();
    builder.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
    builder.HasIndex(e => e.UserId);

    builder
        .HasOne(e => e.User)
        .WithMany(c => c.Produtos)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
