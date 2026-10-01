using LumiaFoundation.Http.Client.Services;

namespace Presentation.Produtos;

public sealed class ProdutoApi(IApiConnection connection) : IProdutoApi
{
  private const string BasePath = "api/produtos";

  public async Task<List<ProdutoDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
      await connection.SendAsync<List<ProdutoDto>>(HttpMethod.Get, BasePath, ct: cancellationToken)
      ?? [];

  public async Task<ProdutoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
      await connection.SendAsync<ProdutoDto>(HttpMethod.Get, $"{BasePath}/{id}", ct: cancellationToken)
      ?? throw new InvalidOperationException("A API respondeu sem corpo para um produto existente.");

  public async Task<ProdutoDto> CreateAsync(ProdutoForCreationDto produto, CancellationToken cancellationToken = default) =>
      await connection.SendAsync<ProdutoDto>(HttpMethod.Post, BasePath, produto, cancellationToken)
      ?? throw new InvalidOperationException("A API respondeu sem corpo ao criar o produto.");

  public Task UpdateAsync(Guid id, ProdutoForUpdateDto produto, CancellationToken cancellationToken = default) =>
      connection.SendAsync(HttpMethod.Put, $"{BasePath}/{id}", produto, cancellationToken);

  public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
      connection.SendAsync(HttpMethod.Delete, $"{BasePath}/{id}", ct: cancellationToken);
}
