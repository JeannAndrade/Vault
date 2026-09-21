using Domain.Bancos;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Bancos;

public class BancoRepository(VaultDbContext repositoryContext) : BaseRepository<Banco>(repositoryContext), IBancoRepository
{
    public void CreateBanco(Banco banco) => Create(banco);

    public async Task<IEnumerable<Banco>> GetAllBancosAsync(Guid ownerId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.Nome).ToListAsync();

    public async Task<Banco?> GetBancoAsync(Guid ownerId, Guid bancoId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId && c.Id == bancoId, trackChanges).SingleOrDefaultAsync();

    public async Task DeleteBancoAsync(Guid ownerId, Guid bancoId)
    {
        var banco = await GetBancoAsync(ownerId, bancoId, false);

        if (banco is not null)
            Delete(banco);
    }
}
