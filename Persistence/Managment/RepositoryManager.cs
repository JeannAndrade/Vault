
using LumiaFoundation.EFRepository.Repository;
using Persistence.Bancos;
using Persistence.Context;

namespace Persistence.Managment
{
    public class RepositoryManager(VaultDbContext repositoryContext) : BaseRepositoryManager(repositoryContext), IRepositoryManager
    {
        private readonly Lazy<IBancoRepository> _bancoRepository = new(() => new BancoRepository(repositoryContext));

        public IBancoRepository Banco => _bancoRepository.Value;
    }
}