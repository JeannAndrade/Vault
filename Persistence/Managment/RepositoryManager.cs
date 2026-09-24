
using LumiaFoundation.EFRepository.Repository;
using Persistence.Context;
using Persistence.Corretoras;
using Persistence.Emissores;
using Persistence.Movimentos;
using Persistence.Objetivos;
using Persistence.Produtos;
using Persistence.TiposRenda;

namespace Persistence.Managment;

public class RepositoryManager(VaultDbContext repositoryContext) : BaseRepositoryManager(repositoryContext), IRepositoryManager
{
    private readonly Lazy<ICorretoraRepository> _corretoraRepository = new(() => new CorretoraRepository(repositoryContext));
    private readonly Lazy<IEmissorRepository> _emissorRepository = new(() => new EmissorRepository(repositoryContext));
    private readonly Lazy<IProdutoRepository> _produtoRepository = new(() => new ProdutoRepository(repositoryContext));
    private readonly Lazy<IObjetivoRepository> _objetivoRepository = new(() => new ObjetivoRepository(repositoryContext));
    private readonly Lazy<ITipoRendaRepository> _tipoRendaRepository = new(() => new TipoRendaRepository(repositoryContext));
    private readonly Lazy<IMovimentoRepository> _movimentoRepository = new(() => new MovimentoRepository(repositoryContext));

    public ICorretoraRepository Corretora => _corretoraRepository.Value;
    public IEmissorRepository Emissor => _emissorRepository.Value;
    public IProdutoRepository Produto => _produtoRepository.Value;
    public IObjetivoRepository Objetivo => _objetivoRepository.Value;
    public ITipoRendaRepository TipoRenda => _tipoRendaRepository.Value;
    public IMovimentoRepository Movimento => _movimentoRepository.Value;
}
