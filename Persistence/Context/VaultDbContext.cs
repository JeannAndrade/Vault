using Domain.Bancos;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Context
{
    public class VaultDbContext(DbContextOptions options) : RepositoryContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(VaultDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Banco> Companies => Set<Banco>();
    }
}