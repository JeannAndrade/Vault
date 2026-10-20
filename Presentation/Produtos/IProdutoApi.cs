namespace Presentation.Produtos;

public interface IProdutoApi
{
  Task<List<ProdutoDto>> GetAllAsync(CancellationToken cancellationToken);
  Task<ProdutoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
  Task<ProdutoDto> CreateAsync(ProdutoForCreationDto produto, CancellationToken cancellationToken);
  Task UpdateAsync(Guid id, ProdutoForUpdateDto produto, CancellationToken cancellationToken);
  Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
