using LumiaFoundation.Http.Client.Services;

namespace Presentation.Corretoras;

public sealed class CorretoraApi(IApiConnection connection) : ICorretoraApi
{
    private const string BasePath = "api/corretoras";

    public async Task<List<CorretoraDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await connection.SendAsync<List<CorretoraDto>>(HttpMethod.Get, BasePath, ct: cancellationToken)
        ?? [];

    public async Task<CorretoraDto> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await connection.SendAsync<CorretoraDto>(HttpMethod.Get, $"{BasePath}/{id}", ct: cancellationToken)
        ?? throw new InvalidOperationException("A API respondeu sem corpo para uma corretora existente.");

    public async Task<CorretoraDto> CreateAsync(CorretoraForCreationDto corretora, CancellationToken cancellationToken) =>
        await connection.SendAsync<CorretoraDto>(HttpMethod.Post, BasePath, corretora, cancellationToken)
        ?? throw new InvalidOperationException("A API respondeu sem corpo ao criar a corretora.");

    public Task UpdateAsync(Guid id, CorretoraForUpdateDto corretora, CancellationToken cancellationToken) =>
        connection.SendAsync(HttpMethod.Put, $"{BasePath}/{id}", corretora, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        connection.SendAsync(HttpMethod.Delete, $"{BasePath}/{id}", ct: cancellationToken);
}
