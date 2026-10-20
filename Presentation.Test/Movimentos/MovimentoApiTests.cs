using LumiaFoundation.Abstractions.Pagination;
using LumiaFoundation.Http.Client.Services;
using Moq;
using Presentation.Movimentos;

namespace Presentation.Test.Movimentos;

public class MovimentoApiTests
{
    private readonly Mock<IApiConnection> _connection = new();
    private readonly MovimentoApi _api;

    public MovimentoApiTests()
    {
        _api = new MovimentoApi(_connection.Object);
    }

    [Fact]
    public async Task GetPagedAsync_SendsGetWithPaginationQuery()
    {
        var expected = new PagedResponse<MovimentoDto>([new MovimentoDto { Id = Guid.NewGuid() }], 2, 20, 21, 2);
        _connection
            .Setup(c => c.SendAsync<PagedResponse<MovimentoDto>>(HttpMethod.Get, "api/movimentos?page=2&pageSize=20", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.GetPagedAsync(2, 20, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetPagedAsync_WithFilter_SendsGetWithFixedOrderAndOmittedNulls()
    {
        var objetivoId = Guid.NewGuid();
        var tipoRendaId = Guid.NewGuid();
        var corretoraId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var emissorId = Guid.NewGuid();
        var expected = new PagedResponse<MovimentoDto>([new MovimentoDto { Id = Guid.NewGuid() }], 3, 12, 25, 3);
        var filtro = new MovimentoFiltro
        {
            ObjetivoId = objetivoId,
            TipoRendaId = tipoRendaId,
            CorretoraId = corretoraId,
            ProdutoId = produtoId,
            EmissorId = emissorId,
            EstaAtivo = true
        };

        var url = FormattableString.Invariant(
            $"api/movimentos?page=3&pageSize=12&objetivoId={objetivoId}&tipoRendaId={tipoRendaId}&corretoraId={corretoraId}&produtoId={produtoId}&emissorId={emissorId}&estaAtivo=true");

        _connection
            .Setup(c => c.SendAsync<PagedResponse<MovimentoDto>>(HttpMethod.Get, url, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.GetPagedAsync(filtro, 3, 12, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetPagedAsync_WithFalseFlag_UsesLowercaseBoolean()
    {
        var expected = new PagedResponse<MovimentoDto>([], 1, 20, 0, 0);
        _connection
            .Setup(c => c.SendAsync<PagedResponse<MovimentoDto>>(HttpMethod.Get, "api/movimentos?page=1&pageSize=20&estaAtivo=false", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.GetPagedAsync(new MovimentoFiltro { EstaAtivo = false }, 1, 20, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetProximosVencimentosAsync_SendsGetToProximosVencimentosPath()
    {
        var expected = new List<ProximoVencimentoDto> { new() { Id = Guid.NewGuid() } };
        _connection
            .Setup(c => c.SendAsync<List<ProximoVencimentoDto>>(HttpMethod.Get, "api/movimentos/proximos-vencimentos", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.GetProximosVencimentosAsync(CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetDesatualizadosAsync_SendsGetToDesatualizadosPath()
    {
        var expected = new List<MovimentoDesatualizadoDto> { new() { Id = Guid.NewGuid() } };
        _connection
            .Setup(c => c.SendAsync<List<MovimentoDesatualizadoDto>>(HttpMethod.Get, "api/movimentos/desatualizados", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.GetDesatualizadosAsync(CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetPagedAsync_WhenApiReturnsNull_ReturnsEmptyPage()
    {
        _connection
            .Setup(c => c.SendAsync<PagedResponse<MovimentoDto>>(HttpMethod.Get, "api/movimentos?page=3&pageSize=20", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResponse<MovimentoDto>?)null);

        var result = await _api.GetPagedAsync(3, 20, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.Page);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetByIdAsync_SendsGetToMovimentoPath()
    {
        var id = Guid.NewGuid();
        var expected = new MovimentoDto { Id = id };
        _connection
            .Setup(c => c.SendAsync<MovimentoDto>(HttpMethod.Get, $"api/movimentos/{id}", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.GetByIdAsync(id, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task CreateAsync_SendsPostWithBody()
    {
        var body = new MovimentoForCreationDto();
        var expected = new MovimentoDto { Id = Guid.NewGuid() };
        _connection
            .Setup(c => c.SendAsync<MovimentoDto>(HttpMethod.Post, "api/movimentos", body, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.CreateAsync(body, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task UpdateAsync_SendsPutToMovimentoPathWithBody()
    {
        var id = Guid.NewGuid();
        var body = new MovimentoForUpdateDto();

        await _api.UpdateAsync(id, body, CancellationToken.None);

        _connection.Verify(c => c.SendAsync(HttpMethod.Put, $"api/movimentos/{id}", body, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateValorLiquidoAsync_SendsPatchToValorLiquidoPathWithBody()
    {
        var id = Guid.NewGuid();
        var body = new MovimentoForUpdateValorLiquidoDto { ValorLiquidoAtual = 1200m };

        await _api.UpdateValorLiquidoAsync(id, body, CancellationToken.None);

        _connection.Verify(c => c.SendAsync(HttpMethod.Patch, $"api/movimentos/{id}/valor-liquido", body, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteToMovimentoPath()
    {
        var id = Guid.NewGuid();

        await _api.DeleteAsync(id, CancellationToken.None);

        _connection.Verify(c => c.SendAsync(HttpMethod.Delete, $"api/movimentos/{id}", null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
