using Domain.Bancos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Mappings
{
    public class BancoMapping : IEntityTypeConfiguration<Banco>
    {

        private static readonly List<Banco> Bancos =
        [
            new()
            {
                Nome = "Inter"
            },
            new()
            {
                Nome = "NuBank"
            },
            new()
            {
                Nome = "Caixa"
            },
            new()
            {
                Nome = "Mercado Pago"
            }
        ];

        public static List<Banco> GetBancos() => Bancos;

        public void Configure(EntityTypeBuilder<Banco> builder)
        {
            builder.ToTable("Bancos");
            builder.HasKey(k => k.Id);
            builder.Property(e => e.Id).HasColumnName("BancoId");
            builder.Property(e => e.Nome).HasMaxLength(60).IsRequired();

            builder.HasData(Bancos);
        }
    }
}