using LumiaFoundation.Http.Client.Services;
using Moq;
using Presentation.Produtos;

namespace Presentation.Test.Produtos;

public class ProdutoApiTests
{
  private readonly Mock<IApiConnection> _connection = new();
  private readonly ProdutoApi _api;

  public ProdutoApiTests()
  {
    _api = new ProdutoApi(_connection.Object);
  }

  [Fact]
  public async Task GetAllAsync_SendsGetToProdutosPath()
  {
    var expected = new List<ProdutoDto> { new(Guid.NewGuid(), "CDB") };
    _connection
        .Setup(c => c.SendAsync<List<ProdutoDto>>(HttpMethod.Get, "api/produtos", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.GetAllAsync();

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task GetAllAsync_WhenApiReturnsNull_ReturnsEmptyList()
  {
    _connection
        .Setup(c => c.SendAsync<List<ProdutoDto>>(HttpMethod.Get, "api/produtos", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync((List<ProdutoDto>?)null);

    var result = await _api.GetAllAsync();

    Assert.Empty(result);
  }

  [Fact]
  public async Task GetByIdAsync_SendsGetToProdutoPath()
  {
    var id = Guid.NewGuid();
    var expected = new ProdutoDto(id, "CDB");
    _connection
        .Setup(c => c.SendAsync<ProdutoDto>(HttpMethod.Get, $"api/produtos/{id}", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.GetByIdAsync(id);

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task CreateAsync_SendsPostWithBody()
  {
    var body = new ProdutoForCreationDto("CDB");
    var expected = new ProdutoDto(Guid.NewGuid(), "CDB");
    _connection
        .Setup(c => c.SendAsync<ProdutoDto>(HttpMethod.Post, "api/produtos", body, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.CreateAsync(body);

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task UpdateAsync_SendsPutToProdutoPathWithBody()
  {
    var id = Guid.NewGuid();
    var body = new ProdutoForUpdateDto("CDB pós-fixado");

    await _api.UpdateAsync(id, body);

    _connection.Verify(c => c.SendAsync(HttpMethod.Put, $"api/produtos/{id}", body, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task DeleteAsync_SendsDeleteToProdutoPath()
  {
    var id = Guid.NewGuid();

    await _api.DeleteAsync(id);

    _connection.Verify(c => c.SendAsync(HttpMethod.Delete, $"api/produtos/{id}", null, It.IsAny<CancellationToken>()), Times.Once);
  }
}
