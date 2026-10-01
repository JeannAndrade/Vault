using LumiaFoundation.Http.Client.Services;
using Moq;
using Presentation.TiposRenda;

namespace Presentation.Test.TiposRenda;

public class TipoRendaApiTests
{
  private readonly Mock<IApiConnection> _connection = new();
  private readonly TipoRendaApi _api;

  public TipoRendaApiTests()
  {
    _api = new TipoRendaApi(_connection.Object);
  }

  [Fact]
  public async Task GetAllAsync_SendsGetToTiposRendaPath()
  {
    var expected = new List<TipoRendaDto> { new(Guid.NewGuid(), "Renda Fixa") };
    _connection
        .Setup(c => c.SendAsync<List<TipoRendaDto>>(HttpMethod.Get, "api/tiposrenda", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.GetAllAsync();

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task GetAllAsync_WhenApiReturnsNull_ReturnsEmptyList()
  {
    _connection
        .Setup(c => c.SendAsync<List<TipoRendaDto>>(HttpMethod.Get, "api/tiposrenda", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync((List<TipoRendaDto>?)null);

    var result = await _api.GetAllAsync();

    Assert.Empty(result);
  }

  [Fact]
  public async Task GetByIdAsync_SendsGetToTipoRendaPath()
  {
    var id = Guid.NewGuid();
    var expected = new TipoRendaDto(id, "Renda Fixa");
    _connection
        .Setup(c => c.SendAsync<TipoRendaDto>(HttpMethod.Get, $"api/tiposrenda/{id}", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.GetByIdAsync(id);

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task CreateAsync_SendsPostWithBody()
  {
    var body = new TipoRendaForCreationDto("Renda Fixa");
    var expected = new TipoRendaDto(Guid.NewGuid(), "Renda Fixa");
    _connection
        .Setup(c => c.SendAsync<TipoRendaDto>(HttpMethod.Post, "api/tiposrenda", body, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.CreateAsync(body);

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task UpdateAsync_SendsPutToTipoRendaPathWithBody()
  {
    var id = Guid.NewGuid();
    var body = new TipoRendaForUpdateDto("Renda Variável");

    await _api.UpdateAsync(id, body);

    _connection.Verify(c => c.SendAsync(HttpMethod.Put, $"api/tiposrenda/{id}", body, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task DeleteAsync_SendsDeleteToTipoRendaPath()
  {
    var id = Guid.NewGuid();

    await _api.DeleteAsync(id);

    _connection.Verify(c => c.SendAsync(HttpMethod.Delete, $"api/tiposrenda/{id}", null, It.IsAny<CancellationToken>()), Times.Once);
  }
}
