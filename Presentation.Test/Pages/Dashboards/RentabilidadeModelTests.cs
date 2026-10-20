using System.Net;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Dashboards;
using Presentation.Pages.Dashboards;

namespace Presentation.Test.Pages.Dashboards;

public class RentabilidadeModelTests
{
    private readonly Mock<IRentabilidadeApi> _api = new();
    private readonly List<RentabilidadeGrupoDto> _grupos = [new() { TipoRendaNome = "CDB" }];

    public RentabilidadeModelTests() =>
        _api.Setup(a => a.GetRentabilidadesAsync(It.IsAny<int>(), It.IsAny<OrdemRentabilidade>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_grupos);

    [Fact]
    public async Task OnGetAsync_UsaPadroesECarregaGrupos()
    {
        using var cts = new CancellationTokenSource();
        var modelo = new RentabilidadeModel(_api.Object);

        await modelo.OnGetAsync(cts.Token);

        Assert.Same(_grupos, modelo.Grupos);
        _api.Verify(a => a.GetRentabilidadesAsync(25, OrdemRentabilidade.Maiores, cts.Token), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_RepassaParametros()
    {
        var modelo = new RentabilidadeModel(_api.Object) { Top = 10, Ordem = OrdemRentabilidade.Menores };

        await modelo.OnGetAsync(CancellationToken.None);

        _api.Verify(a => a.GetRentabilidadesAsync(10, OrdemRentabilidade.Menores, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(500, 100)]
    public async Task OnGetAsync_NormalizaTop(int informado, int esperado)
    {
        var modelo = new RentabilidadeModel(_api.Object) { Top = informado };

        await modelo.OnGetAsync(CancellationToken.None);

        Assert.Equal(esperado, modelo.Top);
        _api.Verify(a => a.GetRentabilidadesAsync(esperado, It.IsAny<OrdemRentabilidade>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_QuandoApiFalha_MostraErroESemGrupos()
    {
        _api.Setup(a => a.GetRentabilidadesAsync(It.IsAny<int>(), It.IsAny<OrdemRentabilidade>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, "erro"));
        var modelo = new RentabilidadeModel(_api.Object);

        await modelo.OnGetAsync(CancellationToken.None);

        Assert.Equal("Não foi possível carregar as rentabilidades agora. Tente novamente em instantes.", modelo.ErrorMessage);
        Assert.Empty(modelo.Grupos);
    }

    [Fact]
    public void ObterTitulo_UsaTextoConformeOrdem()
    {
        var grupo = new RentabilidadeGrupoDto { TipoRendaNome = "CDB" };
        var modelo = new RentabilidadeModel(_api.Object);

        Assert.Equal("Campeões da renda CDB", modelo.ObterTitulo(grupo));
        modelo.Ordem = OrdemRentabilidade.Menores;
        Assert.Equal("Menores rentabilidades da renda CDB", modelo.ObterTitulo(grupo));
    }

    [Fact]
    public async Task OnGetAsync_ListaVaziaNaoDefineErro()
    {
        _api.Setup(a => a.GetRentabilidadesAsync(It.IsAny<int>(), It.IsAny<OrdemRentabilidade>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var modelo = new RentabilidadeModel(_api.Object);

        await modelo.OnGetAsync(CancellationToken.None);

        Assert.Empty(modelo.Grupos);
        Assert.Null(modelo.ErrorMessage);
    }
}
