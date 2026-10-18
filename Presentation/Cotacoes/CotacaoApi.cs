using LumiaFoundation.Abstractions.Pagination;
using LumiaFoundation.Http.Client.Services;

namespace Presentation.Cotacoes;

public sealed class CotacaoApi(IApiConnection connection) : ICotacaoApi
{
    private const string BasePath = "api/cotacoes";

    public async Task<PagedResponse<CotacaoDto>> GetPagedAsync(
        string? papel,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var path = FormattableString.Invariant($"{BasePath}?page={page}&pageSize={pageSize}");

        if (!string.IsNullOrWhiteSpace(papel))
            path += $"&papel={Uri.EscapeDataString(papel)}";

        return await connection.SendAsync<PagedResponse<CotacaoDto>>(HttpMethod.Get, path, ct: cancellationToken)
            ?? new PagedResponse<CotacaoDto>([], page, pageSize, 0, 0);
    }

    public async Task<List<string>> GetPapeisAsync(CancellationToken cancellationToken = default) =>
        await connection.SendAsync<List<string>>(HttpMethod.Get, $"{BasePath}/papeis", ct: cancellationToken) ?? [];

    public async Task<List<CotacaoPontoDto>> GetSerieAsync(
        string papel,
        CancellationToken cancellationToken = default)
    {
        var path = $"{BasePath}/serie?papel={Uri.EscapeDataString(papel)}";
        return await connection.SendAsync<List<CotacaoPontoDto>>(HttpMethod.Get, path, ct: cancellationToken) ?? [];
    }

    public async Task<CotacaoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await connection.SendAsync<CotacaoDto>(HttpMethod.Get, $"{BasePath}/{id}", ct: cancellationToken)
        ?? throw new InvalidOperationException("A API respondeu sem corpo para uma cotação existente.");

    public async Task<CotacaoDto> CreateAsync(
        CotacaoForCreationDto cotacao,
        CancellationToken cancellationToken = default)
    {
        return await connection.SendAsync<CotacaoDto>(HttpMethod.Post, BasePath, cotacao, cancellationToken)
            ?? throw new InvalidOperationException("A API respondeu sem corpo ao criar a cotação.");
    }

    public Task UpdateAsync(
        Guid id,
        CotacaoForUpdateDto cotacao,
        CancellationToken cancellationToken = default)
    {
        return connection.SendAsync(HttpMethod.Put, $"{BasePath}/{id}", cotacao, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        connection.SendAsync(HttpMethod.Delete, $"{BasePath}/{id}", ct: cancellationToken);
}
