using LumiaFoundation.Http.Client.Services;

namespace Presentation.TiposRenda;

public sealed class TipoRendaApi(IApiConnection connection) : ITipoRendaApi
{
  private const string BasePath = "api/tiposrenda";

  public async Task<List<TipoRendaDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
      await connection.SendAsync<List<TipoRendaDto>>(HttpMethod.Get, BasePath, ct: cancellationToken)
      ?? [];

  public async Task<TipoRendaDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
      await connection.SendAsync<TipoRendaDto>(HttpMethod.Get, $"{BasePath}/{id}", ct: cancellationToken)
      ?? throw new InvalidOperationException("A API respondeu sem corpo para um tipo de renda existente.");

  public async Task<TipoRendaDto> CreateAsync(TipoRendaForCreationDto tipoRenda, CancellationToken cancellationToken = default) =>
      await connection.SendAsync<TipoRendaDto>(HttpMethod.Post, BasePath, tipoRenda, cancellationToken)
      ?? throw new InvalidOperationException("A API respondeu sem corpo ao criar o tipo de renda.");

  public Task UpdateAsync(Guid id, TipoRendaForUpdateDto tipoRenda, CancellationToken cancellationToken = default) =>
      connection.SendAsync(HttpMethod.Put, $"{BasePath}/{id}", tipoRenda, cancellationToken);

  public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
      connection.SendAsync(HttpMethod.Delete, $"{BasePath}/{id}", ct: cancellationToken);
}
