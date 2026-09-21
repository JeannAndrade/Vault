using Domain.Bancos;
using Domain.Corretoras;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Context;

public class VaultDbContext(DbContextOptions<VaultDbContext> options) : RepositoryContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VaultDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public DbSet<Banco> Bancos => Set<Banco>();
    public DbSet<Corretora> Corretoras => Set<Corretora>();
}
