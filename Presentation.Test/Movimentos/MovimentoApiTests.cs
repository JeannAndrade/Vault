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
    public async Task GetAllAsync_SendsGetToMovimentosPath()
    {
        var expected = new List<MovimentoDto> { new() { Id = Guid.NewGuid() } };
        _connection
            .Setup(c => c.SendAsync<List<MovimentoDto>>(HttpMethod.Get, "api/movimentos", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.GetAllAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetAllAsync_WhenApiReturnsNull_ReturnsEmptyList()
    {
        _connection
            .Setup(c => c.SendAsync<List<MovimentoDto>>(HttpMethod.Get, "api/movimentos", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<MovimentoDto>?)null);

        var result = await _api.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_SendsGetToMovimentoPath()
    {
        var id = Guid.NewGuid();
        var expected = new MovimentoDto { Id = id };
        _connection
            .Setup(c => c.SendAsync<MovimentoDto>(HttpMethod.Get, $"api/movimentos/{id}", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.GetByIdAsync(id);

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

        var result = await _api.CreateAsync(body);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task UpdateAsync_SendsPutToMovimentoPathWithBody()
    {
        var id = Guid.NewGuid();
        var body = new MovimentoForUpdateDto();

        await _api.UpdateAsync(id, body);

        _connection.Verify(c => c.SendAsync(HttpMethod.Put, $"api/movimentos/{id}", body, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteToMovimentoPath()
    {
        var id = Guid.NewGuid();

        await _api.DeleteAsync(id);

        _connection.Verify(c => c.SendAsync(HttpMethod.Delete, $"api/movimentos/{id}", null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
