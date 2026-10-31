using LumiaFoundation.Http.Client.Services;
using Moq;
using Presentation.Dashboards;

namespace Presentation.Test.Dashboards;

public class ConsolidadoApiTests
{
    [Fact]
    public async Task EnviaSomenteFiltroInformado()
    {
        var connection = new Mock<IApiConnection>();
        connection.Setup(c => c.SendAsync<ConsolidadoDto>(HttpMethod.Get,
            "api/dashboards/consolidado?corretoraId=11111111-1111-1111-1111-111111111111", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsolidadoDto());
        var api = new ConsolidadoApi(connection.Object);

        await api.GetConsolidadoAsync(new ConsolidadoFiltro { CorretoraId = Guid.Parse("11111111-1111-1111-1111-111111111111") }, null, CancellationToken.None);
        connection.VerifyAll();
    }

    [Fact]
    public async Task EnviaTodosOsParametrosEmOrdemFixaEEnumMinusculo()
    {
        var connection = new Mock<IApiConnection>();
        var token = new CancellationTokenSource().Token;
        const string path = "api/dashboards/consolidado?tipoRendaId=11111111-1111-1111-1111-111111111111&corretoraId=22222222-2222-2222-2222-222222222222&produtoId=33333333-3333-3333-3333-333333333333&emissorId=44444444-4444-4444-4444-444444444444&agruparPor=tiporenda";
        connection.Setup(c => c.SendAsync<ConsolidadoDto>(HttpMethod.Get, path, null, token)).ReturnsAsync(new ConsolidadoDto());
        var api = new ConsolidadoApi(connection.Object);
        await api.GetConsolidadoAsync(new ConsolidadoFiltro
        {
            TipoRendaId = Guid.Parse("11111111-1111-1111-1111-111111111111"), CorretoraId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProdutoId = Guid.Parse("33333333-3333-3333-3333-333333333333"), EmissorId = Guid.Parse("44444444-4444-4444-4444-444444444444")
        }, DimensaoConsolidado.TipoRenda, token);
        connection.VerifyAll();
    }

    [Fact]
    public async Task SemParametrosNaoAdicionaInterrogacaoECorpoNuloViraZero()
    {
        var connection = new Mock<IApiConnection>();
        connection.Setup(c => c.SendAsync<ConsolidadoDto>(HttpMethod.Get, "api/dashboards/consolidado", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsolidadoDto?)null);
        var resultado = await new ConsolidadoApi(connection.Object).GetConsolidadoAsync(new(), null, CancellationToken.None);
        Assert.Equal(0, resultado.Total.Quantidade);
        Assert.Empty(resultado.Grupos);
    }
}
