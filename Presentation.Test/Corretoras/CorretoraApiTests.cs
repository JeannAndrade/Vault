using LumiaFoundation.Http.Client.Services;
using Moq;
using Presentation.Corretoras;

namespace Presentation.Test.Corretoras;

public class CorretoraApiTests
{
    private readonly Mock<IApiConnection> _connection = new();
    private readonly CorretoraApi _api;

    public CorretoraApiTests()
    {
        _api = new CorretoraApi(_connection.Object);
    }

    [Fact]
    public async Task GetAllAsync_SendsGetToCorretorasPath()
    {
        var expected = new List<CorretoraDto> { new(Guid.NewGuid(), "XP") };
        _connection
            .Setup(c => c.SendAsync<List<CorretoraDto>>(HttpMethod.Get, "api/corretoras", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.GetAllAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetAllAsync_WhenApiReturnsNull_ReturnsEmptyList()
    {
        _connection
            .Setup(c => c.SendAsync<List<CorretoraDto>>(HttpMethod.Get, "api/corretoras", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<CorretoraDto>?)null);

        var result = await _api.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_SendsGetToCorretoraPath()
    {
        var id = Guid.NewGuid();
        var expected = new CorretoraDto(id, "XP");
        _connection
            .Setup(c => c.SendAsync<CorretoraDto>(HttpMethod.Get, $"api/corretoras/{id}", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.GetByIdAsync(id);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task CreateAsync_SendsPostWithBody()
    {
        var body = new CorretoraForCreationDto("XP");
        var expected = new CorretoraDto(Guid.NewGuid(), "XP");
        _connection
            .Setup(c => c.SendAsync<CorretoraDto>(HttpMethod.Post, "api/corretoras", body, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _api.CreateAsync(body);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task UpdateAsync_SendsPutToCorretoraPathWithBody()
    {
        var id = Guid.NewGuid();
        var body = new CorretoraForUpdateDto("XP Investimentos");

        await _api.UpdateAsync(id, body);

        _connection.Verify(c => c.SendAsync(HttpMethod.Put, $"api/corretoras/{id}", body, It.IsAny<CancellationToken>()), Times.Once);
    }
}
