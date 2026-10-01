using LumiaFoundation.Http.Client.Services;

namespace Presentation.Objetivos;

public sealed class ObjetivoApi(IApiConnection connection) : IObjetivoApi
{
  private const string BasePath = "api/objetivos";

  public async Task<List<ObjetivoDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
      await connection.SendAsync<List<ObjetivoDto>>(HttpMethod.Get, BasePath, ct: cancellationToken)
      ?? [];

  public async Task<ObjetivoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
      await connection.SendAsync<ObjetivoDto>(HttpMethod.Get, $"{BasePath}/{id}", ct: cancellationToken)
      ?? throw new InvalidOperationException("A API respondeu sem corpo para um objetivo existente.");

  public async Task<ObjetivoDto> CreateAsync(ObjetivoForCreationDto objetivo, CancellationToken cancellationToken = default) =>
      await connection.SendAsync<ObjetivoDto>(HttpMethod.Post, BasePath, objetivo, cancellationToken)
      ?? throw new InvalidOperationException("A API respondeu sem corpo ao criar o objetivo.");

  public Task UpdateAsync(Guid id, ObjetivoForUpdateDto objetivo, CancellationToken cancellationToken = default) =>
      connection.SendAsync(HttpMethod.Put, $"{BasePath}/{id}", objetivo, cancellationToken);

  public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
      connection.SendAsync(HttpMethod.Delete, $"{BasePath}/{id}", ct: cancellationToken);
}
