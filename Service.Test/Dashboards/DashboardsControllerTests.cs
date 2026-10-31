using Application.Dashboards.Queries.GetRentabilidadesPorTipoRenda;
using Application.Dashboards.Queries.GetRetornoMensal;
using Application.Dashboards.Queries.GetInvestidoMensal;
using Application.Dashboards.Queries.GetConsolidado;
using Application.Dashboards.Rentabilidade;
using Application.Dashboards.Consolidado;
using Persistence.Movimentos;
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
    private readonly Mock<IGetConsolidadoQuery> _consolidadoQuery = new();
    private readonly DashboardsController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public DashboardsControllerTests()
    {
        _controller = new DashboardsController(_query.Object, _retornoQuery.Object, _investidoQuery.Object, _consolidadoQuery.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _controller.HttpContext.Items["UserId"] = _userId.ToString();
    }

    [Fact]
    public async Task GetConsolidado_RepassaUsuarioParametrosETokenEMapeia()
    {
        using var cts = new CancellationTokenSource();
        var tipoRendaId = Guid.NewGuid();
        var modelo = new ConsolidadoModel(
            new ConsolidadoLinhaModel(null, null, 2, 90m, 100m, 1m),
            [new ConsolidadoLinhaModel(Guid.NewGuid(), "XP", 2, 90m, 100m, 1m)]);
        _consolidadoQuery.Setup(q => q.ExecuteAsync(_userId,
            It.Is<ConsolidadoParameters>(p => p.TipoRendaId == tipoRendaId && p.AgruparPor == DimensaoConsolidado.Corretora), cts.Token))
            .ReturnsAsync(modelo);

        var result = await _controller.GetConsolidado(new ConsolidadoFiltroDto
        { TipoRendaId = tipoRendaId, AgruparPor = DimensaoConsolidado.Corretora }, cts.Token);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var resposta = Assert.IsType<ConsolidadoDto>(ok.Value);
        Assert.Equal(1m, resposta.Total.PercentualLiquido);
        Assert.Equal("XP", Assert.Single(resposta.Grupos).Nome);
        _consolidadoQuery.VerifyAll();
    }

    [Fact]
    public async Task GetConsolidado_SemFiltro_RepassaParametrosVazios()
    {
        _consolidadoQuery.Setup(q => q.ExecuteAsync(_userId,
            It.Is<ConsolidadoParameters>(p => p.TipoRendaId == null && p.AgruparPor == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsolidadoModel(new ConsolidadoLinhaModel(null, null, 0, 0, 0, 0), []));

        await _controller.GetConsolidado(null!, CancellationToken.None);
        _consolidadoQuery.VerifyAll();
    }

    [Fact]
    public async Task GetConsolidado_ExcecaoDaQueryNaoEhEngolida()
    {
        var erro = new LumiaFoundation.Core.Domain.Exceptions.CommandValidationException("inválido");
        _consolidadoQuery.Setup(q => q.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<ConsolidadoParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(erro);

        var lancada = await Assert.ThrowsAsync<LumiaFoundation.Core.Domain.Exceptions.CommandValidationException>(() =>
            _controller.GetConsolidado(new ConsolidadoFiltroDto(), CancellationToken.None));
        Assert.Same(erro, lancada);
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
