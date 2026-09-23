using Domain.Produtos;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Produtos;

public class ProdutoRepository(VaultDbContext repositoryContext) : BaseRepository<Produto>(repositoryContext), IProdutoRepository
{
  public void Create(Produto produto) => base.Create(produto);

  public async Task<IEnumerable<Produto>> GetAllAsync(Guid ownerId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.Nome).ToListAsync();

  public async Task<Produto?> GetAsync(Guid ownerId, Guid produtoId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId && c.Id == produtoId, trackChanges).SingleOrDefaultAsync();

  public async Task DeleteAsync(Guid ownerId, Guid produtoId)
  {
    var produto = await GetAsync(ownerId, produtoId, false);

    if (produto is not null)
      Delete(produto);
  }
}
