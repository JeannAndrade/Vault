using LumiaFoundation.Http.Client.Services;
using Moq;
using Presentation.Dashboards;

namespace Presentation.Test.Dashboards;

public class RentabilidadeApiTests
{
    private readonly Mock<IApiConnection> _connection = new();
    private readonly RentabilidadeApi _api;

    public RentabilidadeApiTests() => _api = new RentabilidadeApi(_connection.Object);

    [Fact]
    public async Task GetRentabilidadesAsync_EnviaGetComParametrosPadrao()
    {
        var esperado = new List<RentabilidadeGrupoDto> { new() { TipoRendaNome = "CDB" } };
        _connection.Setup(c => c.SendAsync<List<RentabilidadeGrupoDto>>(
                HttpMethod.Get, "api/dashboards/rentabilidade?top=25&ordem=maiores", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperado);

        var resultado = await _api.GetRentabilidadesAsync(25, OrdemRentabilidade.Maiores, CancellationToken.None);

        Assert.Same(esperado, resultado);
    }

    [Fact]
    public async Task GetRentabilidadesAsync_EnviaOrdemMenoresEmMinusculo()
    {
        using var cts = new CancellationTokenSource();
        _connection.Setup(c => c.SendAsync<List<RentabilidadeGrupoDto>>(
                HttpMethod.Get, "api/dashboards/rentabilidade?top=10&ordem=menores", null, cts.Token))
            .ReturnsAsync([]);

        await _api.GetRentabilidadesAsync(10, OrdemRentabilidade.Menores, cts.Token);

        _connection.VerifyAll();
    }

    [Fact]
    public async Task GetRentabilidadesAsync_CorpoNuloRetornaListaVazia()
    {
        _connection.Setup(c => c.SendAsync<List<RentabilidadeGrupoDto>>(
                It.IsAny<HttpMethod>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<RentabilidadeGrupoDto>?)null);

        var resultado = await _api.GetRentabilidadesAsync(25, OrdemRentabilidade.Maiores, CancellationToken.None);

        Assert.Empty(resultado);
    }
}
