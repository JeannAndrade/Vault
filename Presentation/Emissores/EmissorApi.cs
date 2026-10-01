using LumiaFoundation.Http.Client.Services;

namespace Presentation.Emissores;

public sealed class EmissorApi(IApiConnection connection) : IEmissorApi
{
  private const string BasePath = "api/emissores";

  public async Task<List<EmissorDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
      await connection.SendAsync<List<EmissorDto>>(HttpMethod.Get, BasePath, ct: cancellationToken)
      ?? [];

  public async Task<EmissorDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
      await connection.SendAsync<EmissorDto>(HttpMethod.Get, $"{BasePath}/{id}", ct: cancellationToken)
      ?? throw new InvalidOperationException("A API respondeu sem corpo para um emissor existente.");

  public async Task<EmissorDto> CreateAsync(EmissorForCreationDto emissor, CancellationToken cancellationToken = default) =>
      await connection.SendAsync<EmissorDto>(HttpMethod.Post, BasePath, emissor, cancellationToken)
      ?? throw new InvalidOperationException("A API respondeu sem corpo ao criar o emissor.");

  public Task UpdateAsync(Guid id, EmissorForUpdateDto emissor, CancellationToken cancellationToken = default) =>
      connection.SendAsync(HttpMethod.Put, $"{BasePath}/{id}", emissor, cancellationToken);

  public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
      connection.SendAsync(HttpMethod.Delete, $"{BasePath}/{id}", ct: cancellationToken);
}
