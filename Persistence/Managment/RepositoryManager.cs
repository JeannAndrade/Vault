
using LumiaFoundation.EFRepository.Repository;
using Persistence.Bancos;
using Persistence.Context;
using Persistence.Corretoras;
using Persistence.Emissores;
using Persistence.Objetivos;
using Persistence.Produtos;

namespace Persistence.Managment;

public class RepositoryManager(VaultDbContext repositoryContext) : BaseRepositoryManager(repositoryContext), IRepositoryManager
{
    private readonly Lazy<IBancoRepository> _bancoRepository = new(() => new BancoRepository(repositoryContext));
    private readonly Lazy<ICorretoraRepository> _corretoraRepository = new(() => new CorretoraRepository(repositoryContext));
    private readonly Lazy<IEmissorRepository> _emissorRepository = new(() => new EmissorRepository(repositoryContext));
    private readonly Lazy<IProdutoRepository> _produtoRepository = new(() => new ProdutoRepository(repositoryContext));
    private readonly Lazy<IObjetivoRepository> _objetivoRepository = new(() => new ObjetivoRepository(repositoryContext));

    public IBancoRepository Banco => _bancoRepository.Value;
    public ICorretoraRepository Corretora => _corretoraRepository.Value;
    public IEmissorRepository Emissor => _emissorRepository.Value;
    public IProdutoRepository Produto => _produtoRepository.Value;
    public IObjetivoRepository Objetivo => _objetivoRepository.Value;
}
