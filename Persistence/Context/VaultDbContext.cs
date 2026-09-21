using Domain.Bancos;
using Domain.Corretoras;
using Domain.Emissores;
using Domain.Objetivos;
using Domain.Produtos;
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
    public DbSet<Emissor> Emissores => Set<Emissor>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Objetivo> Objetivos => Set<Objetivo>();
}
