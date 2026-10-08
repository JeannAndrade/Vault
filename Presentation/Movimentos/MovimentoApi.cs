using LumiaFoundation.Abstractions.Pagination;
using LumiaFoundation.Http.Client.Services;

namespace Presentation.Movimentos;

public sealed class MovimentoApi(IApiConnection connection) : IMovimentoApi
{
    private const string BasePath = "api/movimentos";

    public async Task<PagedResponse<MovimentoDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        await connection.SendAsync<PagedResponse<MovimentoDto>>(
            HttpMethod.Get, FormattableString.Invariant($"{BasePath}?page={page}&pageSize={pageSize}"), ct: cancellationToken)
        ?? new PagedResponse<MovimentoDto>([], page, pageSize, 0, 0);

    public async Task<MovimentoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await connection.SendAsync<MovimentoDto>(HttpMethod.Get, $"{BasePath}/{id}", ct: cancellationToken)
        ?? throw new InvalidOperationException("A API respondeu sem corpo para um movimento existente.");

    public async Task<MovimentoDto> CreateAsync(MovimentoForCreationDto movimento, CancellationToken cancellationToken = default) =>
        await connection.SendAsync<MovimentoDto>(HttpMethod.Post, BasePath, movimento, cancellationToken)
        ?? throw new InvalidOperationException("A API respondeu sem corpo ao criar o movimento.");

    public Task UpdateAsync(Guid id, MovimentoForUpdateDto movimento, CancellationToken cancellationToken = default) =>
        connection.SendAsync(HttpMethod.Put, $"{BasePath}/{id}", movimento, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        connection.SendAsync(HttpMethod.Delete, $"{BasePath}/{id}", ct: cancellationToken);
}
