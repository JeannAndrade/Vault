using Domain.Produtos;

namespace Persistence.Produtos;

public interface IProdutoRepository
{
  Task<IEnumerable<Produto>> GetAllProdutosAsync(Guid ownerId, bool trackChanges);
  Task<Produto?> GetProdutoAsync(Guid ownerId, Guid produtoId, bool trackChanges);
  Task DeleteProdutoAsync(Guid ownerId, Guid produtoId);
  void CreateProduto(Produto produto);
}
