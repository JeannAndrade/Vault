using Domain.Produtos;

namespace Persistence.Produtos;

public interface IProdutoRepository
{
    Task<IEnumerable<Produto>> GetAllAsync(Guid ownerId, bool trackChanges, CancellationToken cancellationToken);
    Task DeleteAsync(Guid ownerId, Guid produtoId, CancellationToken cancellationToken);
    Task<Produto?> GetAsync(Guid ownerId, Guid produtoId, bool trackChanges, CancellationToken cancellationToken);
    void Delete(Produto produto);
    void Create(Produto produto);
    void Update(Produto produto);
}
