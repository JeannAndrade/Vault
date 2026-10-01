using LumiaFoundation.Http.Client.Services;
using Moq;
using Presentation.Emissores;

namespace Presentation.Test.Emissores;

public class EmissorApiTests
{
  private readonly Mock<IApiConnection> _connection = new();
  private readonly EmissorApi _api;

  public EmissorApiTests()
  {
    _api = new EmissorApi(_connection.Object);
  }

  [Fact]
  public async Task GetAllAsync_SendsGetToEmissoresPath()
  {
    var expected = new List<EmissorDto> { new(Guid.NewGuid(), "Tesouro Nacional") };
    _connection
        .Setup(c => c.SendAsync<List<EmissorDto>>(HttpMethod.Get, "api/emissores", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.GetAllAsync();

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task GetAllAsync_WhenApiReturnsNull_ReturnsEmptyList()
  {
    _connection
        .Setup(c => c.SendAsync<List<EmissorDto>>(HttpMethod.Get, "api/emissores", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync((List<EmissorDto>?)null);

    var result = await _api.GetAllAsync();

    Assert.Empty(result);
  }

  [Fact]
  public async Task GetByIdAsync_SendsGetToEmissorPath()
  {
    var id = Guid.NewGuid();
    var expected = new EmissorDto(id, "Tesouro Nacional");
    _connection
        .Setup(c => c.SendAsync<EmissorDto>(HttpMethod.Get, $"api/emissores/{id}", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.GetByIdAsync(id);

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task CreateAsync_SendsPostWithBody()
  {
    var body = new EmissorForCreationDto("Tesouro Nacional");
    var expected = new EmissorDto(Guid.NewGuid(), "Tesouro Nacional");
    _connection
        .Setup(c => c.SendAsync<EmissorDto>(HttpMethod.Post, "api/emissores", body, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.CreateAsync(body);

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task UpdateAsync_SendsPutToEmissorPathWithBody()
  {
    var id = Guid.NewGuid();
    var body = new EmissorForUpdateDto("Tesouro Nacional");

    await _api.UpdateAsync(id, body);

    _connection.Verify(c => c.SendAsync(HttpMethod.Put, $"api/emissores/{id}", body, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task DeleteAsync_SendsDeleteToEmissorPath()
  {
    var id = Guid.NewGuid();

    await _api.DeleteAsync(id);

    _connection.Verify(c => c.SendAsync(HttpMethod.Delete, $"api/emissores/{id}", null, It.IsAny<CancellationToken>()), Times.Once);
  }
}
