using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context;

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    // Id e ConcurrencyStamp precisam ser fixos: o IdentityRole os gera aleatoriamente a cada
    // instância, e o EF trataria o seed como "novo" em toda migration (DeleteData + InsertData).
    // No contexto de identidade isso apagaria a role e, por cascata (AspNetUserRoles), os
    // vínculos dela com os usuários. Estes são os valores já gravados no snapshot de identidade
    // (migration TabelasIdentity): NÃO os altere.
    public const string AdministratorId = "a6555a52-0a06-44c6-977b-3183faa171d9";
    public const string AdministratorConcurrencyStamp = "7729cb8f-9cd8-4292-8f0d-67810bd24e5c";

    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        /* builder.HasData(new IdentityRole
        {
            Id = AdministratorId,
            Name = "Administrator",
            NormalizedName = "ADMINISTRATOR",
            ConcurrencyStamp = AdministratorConcurrencyStamp
        }); */
    }
}

