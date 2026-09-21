
using LumiaFoundation.EFRepository.Repository;
using Persistence.Bancos;
using Persistence.Context;
using Persistence.Corretoras;

namespace Persistence.Managment;

public class RepositoryManager(VaultDbContext repositoryContext) : BaseRepositoryManager(repositoryContext), IRepositoryManager
{
    private readonly Lazy<IBancoRepository> _bancoRepository = new(() => new BancoRepository(repositoryContext));
    private readonly Lazy<ICorretoraRepository> _corretoraRepository = new(() => new CorretoraRepository(repositoryContext));

    public IBancoRepository Banco => _bancoRepository.Value;
    public ICorretoraRepository Corretora => _corretoraRepository.Value;
}
