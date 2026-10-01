namespace Presentation.Produtos;

public interface IProdutoApi
{
  Task<List<ProdutoDto>> GetAllAsync(CancellationToken cancellationToken = default);
  Task<ProdutoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<ProdutoDto> CreateAsync(ProdutoForCreationDto produto, CancellationToken cancellationToken = default);
  Task UpdateAsync(Guid id, ProdutoForUpdateDto produto, CancellationToken cancellationToken = default);
  Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
