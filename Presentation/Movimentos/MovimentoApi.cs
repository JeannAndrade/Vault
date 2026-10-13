using LumiaFoundation.Abstractions.Pagination;
using LumiaFoundation.Http.Client.Services;

namespace Presentation.Movimentos;

public sealed class MovimentoApi(IApiConnection connection) : IMovimentoApi
{
    private const string BasePath = "api/movimentos";

    public Task<PagedResponse<MovimentoDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetPagedAsync(MovimentoFiltro.Vazio, page, pageSize, cancellationToken);

    public async Task<PagedResponse<MovimentoDto>> GetPagedAsync(
        MovimentoFiltro filtro,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        filtro ??= MovimentoFiltro.Vazio;

        var query = FormattableString.Invariant($"{BasePath}?page={page}&pageSize={pageSize}");

        if (filtro.ObjetivoId is Guid objetivoId)
            query += FormattableString.Invariant($"&objetivoId={objetivoId}");

        if (filtro.TipoRendaId is Guid tipoRendaId)
            query += FormattableString.Invariant($"&tipoRendaId={tipoRendaId}");

        if (filtro.CorretoraId is Guid corretoraId)
            query += FormattableString.Invariant($"&corretoraId={corretoraId}");

        if (filtro.ProdutoId is Guid produtoId)
            query += FormattableString.Invariant($"&produtoId={produtoId}");

        if (filtro.EmissorId is Guid emissorId)
            query += FormattableString.Invariant($"&emissorId={emissorId}");

        if (filtro.EstaAtivo is bool estaAtivo)
            query += FormattableString.Invariant($"&estaAtivo={estaAtivo.ToString().ToLowerInvariant()}");

        return await connection.SendAsync<PagedResponse<MovimentoDto>>(
            HttpMethod.Get, query, ct: cancellationToken)
        ?? new PagedResponse<MovimentoDto>([], page, pageSize, 0, 0);
    }

    public async Task<List<ProximoVencimentoDto>> GetProximosVencimentosAsync(CancellationToken cancellationToken = default) =>
        await connection.SendAsync<List<ProximoVencimentoDto>>(
            HttpMethod.Get, $"{BasePath}/proximos-vencimentos", ct: cancellationToken)
        ?? [];

    public async Task<List<MovimentoDesatualizadoDto>> GetDesatualizadosAsync(CancellationToken cancellationToken = default) =>
        await connection.SendAsync<List<MovimentoDesatualizadoDto>>(
            HttpMethod.Get, $"{BasePath}/desatualizados", ct: cancellationToken)
        ?? [];

    public async Task<MovimentoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await connection.SendAsync<MovimentoDto>(HttpMethod.Get, $"{BasePath}/{id}", ct: cancellationToken)
        ?? throw new InvalidOperationException("A API respondeu sem corpo para um movimento existente.");

    public async Task<MovimentoDto> CreateAsync(MovimentoForCreationDto movimento, CancellationToken cancellationToken = default) =>
        await connection.SendAsync<MovimentoDto>(HttpMethod.Post, BasePath, movimento, cancellationToken)
        ?? throw new InvalidOperationException("A API respondeu sem corpo ao criar o movimento.");

    public Task UpdateAsync(Guid id, MovimentoForUpdateDto movimento, CancellationToken cancellationToken = default) =>
        connection.SendAsync(HttpMethod.Put, $"{BasePath}/{id}", movimento, cancellationToken);

    public Task UpdateValorLiquidoAsync(Guid id, MovimentoForUpdateValorLiquidoDto movimento, CancellationToken cancellationToken = default) =>
        connection.SendAsync(HttpMethod.Patch, $"{BasePath}/{id}/valor-liquido", movimento, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        connection.SendAsync(HttpMethod.Delete, $"{BasePath}/{id}", ct: cancellationToken);
}
