using Domain.Produtos;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Produtos;

public class ProdutoRepository(VaultDbContext repositoryContext) : BaseRepository<Produto>(repositoryContext), IProdutoRepository
{
  public void CreateProduto(Produto produto) => Create(produto);

  public async Task<IEnumerable<Produto>> GetAllProdutosAsync(Guid ownerId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.Nome).ToListAsync();

  public async Task<Produto?> GetProdutoAsync(Guid ownerId, Guid produtoId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId && c.Id == produtoId, trackChanges).SingleOrDefaultAsync();

  public async Task DeleteProdutoAsync(Guid ownerId, Guid produtoId)
  {
    var produto = await GetProdutoAsync(ownerId, produtoId, false);

    if (produto is not null)
      Delete(produto);
  }
}
