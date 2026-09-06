using Domain.Bancos;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Bancos
{
    public class BancoRepository(VaultDbContext repositoryContext) : BaseRepository<Banco>(repositoryContext), IBancoRepository
    {
        public void CreateBanco(Banco banco) => Create(banco);

        public async Task<IEnumerable<Banco>> GetAllBancosAsync(bool trackChanges) =>
            await FindAll(trackChanges).OrderBy(c => c.Nome).ToListAsync();

        public async Task<Banco?> GetBancoAsync(Guid bancoId, bool trackChanges) =>
            await FindByCondition(c => c.Id == bancoId, trackChanges).SingleOrDefaultAsync();
    }
}