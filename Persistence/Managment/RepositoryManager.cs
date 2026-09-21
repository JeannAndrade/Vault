
using LumiaFoundation.EFRepository.Repository;
using Persistence.Bancos;
using Persistence.Context;
using Persistence.Corretoras;
using Persistence.Emissores;

namespace Persistence.Managment;

public class RepositoryManager(VaultDbContext repositoryContext) : BaseRepositoryManager(repositoryContext), IRepositoryManager
{
    private readonly Lazy<IBancoRepository> _bancoRepository = new(() => new BancoRepository(repositoryContext));
    private readonly Lazy<ICorretoraRepository> _corretoraRepository = new(() => new CorretoraRepository(repositoryContext));
    private readonly Lazy<IEmissorRepository> _emissorRepository = new(() => new EmissorRepository(repositoryContext));

    public IBancoRepository Banco => _bancoRepository.Value;
    public ICorretoraRepository Corretora => _corretoraRepository.Value;
    public IEmissorRepository Emissor => _emissorRepository.Value;
}
