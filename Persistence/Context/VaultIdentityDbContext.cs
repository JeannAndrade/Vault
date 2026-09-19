using LumiaFoundation.Auth.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Context;

public class VaultIdentityDbContext(DbContextOptions<VaultIdentityDbContext> options) : IdentityContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new RoleConfiguration());
    }
}
