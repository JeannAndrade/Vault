using Domain.Produtos;

namespace Persistence.Produtos;

public interface IProdutoRepository
{
    Task<IEnumerable<Produto>> GetAllAsync(Guid ownerId, bool trackChanges);
    Task<Produto?> GetAsync(Guid ownerId, Guid produtoId, bool trackChanges);
    Task DeleteAsync(Guid ownerId, Guid produtoId);
    void Create(Produto produto);
    void Update(Produto produto);
}
