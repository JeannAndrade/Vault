using Application.Dashboards.Queries.GetRentabilidadesPorTipoRenda;
using Application.Dashboards.Queries.GetRetornoMensal;
using Application.Dashboards.Queries.GetInvestidoMensal;
using Application.Dashboards.Rentabilidade;
using LumiaFoundation.AspNetCore.ActionFilters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Dashboards;
using Service.Dashboards.DTOs;

namespace Service.Test.Dashboards;

public class DashboardsControllerTests
{
    private readonly Mock<IGetRentabilidadesPorTipoRendaQuery> _query = new();
    private readonly Mock<IGetRetornoMensalQuery> _retornoQuery = new();
    private readonly Mock<IGetInvestidoMensalQuery> _investidoQuery = new();
    private readonly DashboardsController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public DashboardsControllerTests()
    {
        _controller = new DashboardsController(_query.Object, _retornoQuery.Object, _investidoQuery.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _controller.HttpContext.Items["UserId"] = _userId.ToString();
    }

    [Fact]
    public void PossuiFiltroDeValidacaoDeRequisicao() =>
        Assert.Contains(typeof(RequestValidationAttribute), typeof(DashboardsController)
            .GetCustomAttributes(true).Select(a => a.GetType()));

    [Fact]
    public async Task GetRentabilidade_RepassaUsuarioParametrosETokenEMapeia()
    {
        using var cts = new CancellationTokenSource();
        var grupo = new RentabilidadeGrupoModel(
            Guid.NewGuid(), "CDB", [new RentabilidadeItemModel(
                Guid.NewGuid(), "Produto", "Emissor", "Objetivo", "Corretora",
                1000, 1100, 365, 0.1, 0.000261, 0.1)]);
        _query.Setup(q => q.ExecuteAsync(
                _userId,
                It.Is<RentabilidadeParameters>(p => p.Top == 10 && p.Ordem == OrdemRentabilidade.Menores),
                cts.Token))
            .ReturnsAsync([grupo]);

        var result = await _controller.GetRentabilidade(
            new RentabilidadeFiltroDto { Top = 10, Ordem = OrdemRentabilidade.Menores }, cts.Token);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var resposta = Assert.IsType<List<RentabilidadeGrupoDto>>(ok.Value);
        var item = Assert.Single(Assert.Single(resposta).Itens);
        Assert.Equal(grupo.Itens[0].Id, item.Id);
        Assert.Equal(0.1, item.Anualizada);
        _query.VerifyAll();
    }

    [Fact]
    public async Task GetRentabilidade_SemFiltro_UsaPadroes()
    {
        _query.Setup(q => q.ExecuteAsync(
                _userId,
                It.Is<RentabilidadeParameters>(p => p.Top == 25 && p.Ordem == OrdemRentabilidade.Maiores),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _controller.GetRentabilidade(null!, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        _query.VerifyAll();
    }

    [Fact]
    public async Task GetRentabilidade_ListaVazia_RetornaOkComListaVazia()
    {
        _query.Setup(q => q.ExecuteAsync(_userId, It.IsAny<RentabilidadeParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _controller.GetRentabilidade(new RentabilidadeFiltroDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty(Assert.IsType<List<RentabilidadeGrupoDto>>(ok.Value));
    }

    [Fact]
    public async Task GetRentabilidade_UsaOUsuarioDoToken()
    {
        _query.Setup(q => q.ExecuteAsync(_userId, It.IsAny<RentabilidadeParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _controller.GetRentabilidade(new RentabilidadeFiltroDto(), CancellationToken.None);

        _query.Verify(q => q.ExecuteAsync(_userId, It.IsAny<RentabilidadeParameters>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
