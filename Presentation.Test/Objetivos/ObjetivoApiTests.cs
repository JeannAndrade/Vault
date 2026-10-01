using LumiaFoundation.Http.Client.Services;
using Moq;
using Presentation.Objetivos;

namespace Presentation.Test.Objetivos;

public class ObjetivoApiTests
{
  private readonly Mock<IApiConnection> _connection = new();
  private readonly ObjetivoApi _api;

  public ObjetivoApiTests()
  {
    _api = new ObjetivoApi(_connection.Object);
  }

  [Fact]
  public async Task GetAllAsync_SendsGetToObjetivosPath()
  {
    var expected = new List<ObjetivoDto> { CreateObjetivo() };
    _connection
        .Setup(c => c.SendAsync<List<ObjetivoDto>>(HttpMethod.Get, "api/objetivos", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.GetAllAsync();

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task GetAllAsync_WhenApiReturnsNull_ReturnsEmptyList()
  {
    _connection
        .Setup(c => c.SendAsync<List<ObjetivoDto>>(HttpMethod.Get, "api/objetivos", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync((List<ObjetivoDto>?)null);

    var result = await _api.GetAllAsync();

    Assert.Empty(result);
  }

  [Fact]
  public async Task GetByIdAsync_SendsGetToObjetivoPath()
  {
    var id = Guid.NewGuid();
    var expected = CreateObjetivo(id);
    _connection
        .Setup(c => c.SendAsync<ObjetivoDto>(HttpMethod.Get, $"api/objetivos/{id}", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.GetByIdAsync(id);

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task CreateAsync_SendsPostWithBody()
  {
    var body = CreateForCreation();
    var expected = CreateObjetivo();
    _connection
        .Setup(c => c.SendAsync<ObjetivoDto>(HttpMethod.Post, "api/objetivos", body, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expected);

    var result = await _api.CreateAsync(body);

    Assert.Same(expected, result);
  }

  [Fact]
  public async Task UpdateAsync_SendsPutToObjetivoPathWithBody()
  {
    var id = Guid.NewGuid();
    var body = CreateForUpdate();

    await _api.UpdateAsync(id, body);

    _connection.Verify(c => c.SendAsync(HttpMethod.Put, $"api/objetivos/{id}", body, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task DeleteAsync_SendsDeleteToObjetivoPath()
  {
    var id = Guid.NewGuid();

    await _api.DeleteAsync(id);

    _connection.Verify(c => c.SendAsync(HttpMethod.Delete, $"api/objetivos/{id}", null, It.IsAny<CancellationToken>()), Times.Once);
  }

  private static ObjetivoDto CreateObjetivo(Guid? id = null) => new(
      id ?? Guid.NewGuid(),
      "Reserva de emergência",
      "Manter uma reserva para imprevistos",
      10000,
      "Salário",
      500,
      "Tesouro Selic");

  private static ObjetivoForCreationDto CreateForCreation() => new(
      "Reserva de emergência",
      "Manter uma reserva para imprevistos",
      10000,
      "Salário",
      500,
      "Tesouro Selic");

  private static ObjetivoForUpdateDto CreateForUpdate() => new(
      "Reserva de emergência",
      "Manter uma reserva para imprevistos",
      12000,
      "Salário",
      600,
      "Tesouro Selic");
}
