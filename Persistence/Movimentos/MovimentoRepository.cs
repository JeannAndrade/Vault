using Domain.Movimentos;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Movimentos;

public class MovimentoRepository(VaultDbContext repositoryContext) : BaseRepository<Movimento>(repositoryContext), IMovimentoRepository
{
    public async Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.DataInvestimento).ToListAsync();

    public async Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId && c.Id == movimentoId, trackChanges).SingleOrDefaultAsync();

    public async Task DeleteAsync(Guid ownerId, Guid movimentoId)
    {
        var movimento = await GetAsync(ownerId, movimentoId, true);

        if (movimento is not null)
            Delete(movimento);
    }
}
