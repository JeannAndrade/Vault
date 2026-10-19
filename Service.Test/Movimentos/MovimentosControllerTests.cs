using Application.Movimentos;
using Application.Movimentos.Commands.CreateMovimento;
using Application.Movimentos.Commands.DeleteMovimento;
using Application.Movimentos.Commands.UpdateMovimento;
using Application.Movimentos.Commands.UpdateValorLiquidoMovimento;
using Application.Movimentos.Queries.GetMovimento;
using Application.Movimentos.Queries.GetMovimentoList;
using Application.Movimentos.Queries.GetMovimentosDesatualizados;
using Application.Movimentos.Queries.GetProximosVencimentos;
using Application.Pagination;
using LumiaFoundation.Abstractions.Pagination;
using LumiaFoundation.AspNetCore.ActionFilters;
using LumiaFoundation.Core.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Movimentos;
using Service.Movimentos.DTOs;

namespace Service.Test.Movimentos;

public class MovimentosControllerTests
{
    private readonly Mock<IGetMovimentosListQuery> _getMovimentosListQuery = new();
    private readonly Mock<IGetProximosVencimentosQuery> _getProximosVencimentosQuery = new();
    private readonly Mock<IGetMovimentosDesatualizadosQuery> _getMovimentosDesatualizadosQuery = new();
    private readonly Mock<IUpdateValorLiquidoMovimentoCommand> _updateValorLiquidoMovimentoCommand = new();
    private readonly MovimentosController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public MovimentosControllerTests()
    {
        _controller = new MovimentosController(
            _getMovimentosListQuery.Object,
            _getProximosVencimentosQuery.Object,
            new Mock<IGetMovimentoQuery>().Object,
            new Mock<ICreateMovimentoCommand>().Object,
            new Mock<IUpdateMovimentoCommand>().Object,
            new Mock<IDeleteMovimentoCommand>().Object,
            _getMovimentosDesatualizadosQuery.Object,
            _updateValorLiquidoMovimentoCommand.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _controller.HttpContext.Items["UserId"] = _userId.ToString();
    }

    [Fact]
    public async Task GetMovimentos_RepassaFiltroPaginacaoEMapeiaEnvelope()
    {
        using var cts = new CancellationTokenSource();
        var objetivoId = Guid.NewGuid();
        var pagina = new PagedList<MovimentoModel>(
            [new MovimentoModel { Id = Guid.NewGuid(), UserId = _userId }], page: 2, pageSize: 10, totalCount: 11);
        _getMovimentosListQuery
            .Setup(q => q.ExecuteAsync(
                _userId,
                It.Is<MovimentoFiltroParameters>(f =>
                    f.ObjetivoId == objetivoId &&
                    f.TipoRendaId == null &&
                    f.CorretoraId == null &&
                    f.ProdutoId == null &&
                    f.EmissorId == null &&
                    f.EstaAtivo == true),
                It.Is<PaginationParameters>(p => p.Page == 2 && p.PageSize == 10),
                cts.Token))
            .ReturnsAsync(pagina);

        var result = await _controller.GetMovimentos(
            new MovimentoFiltroDto { ObjetivoId = objetivoId, EstaAtivo = true },
            cts.Token,
            2,
            10);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var resposta = Assert.IsType<PagedResponse<MovimentoDto>>(ok.Value);
        Assert.Single(resposta.Items);
        Assert.Equal(2, resposta.Page);
        Assert.Equal(10, resposta.PageSize);
        Assert.Equal(11, resposta.TotalCount);
        Assert.Equal(2, resposta.TotalPages);
    }

    [Fact]
    public async Task GetProximosVencimentos_RespondeListaMapeada()
    {
        var movimento = new ProximoVencimentoModel
        {
            Id = Guid.NewGuid(),
            ObjetivoNome = "Aposentadoria",
            CorretoraNome = "XP",
            ProdutoNome = "CDB",
            EmissorNome = "Banco Y",
            DataVencimento = new DateTime(2026, 10, 20),
            ValorLiquidoAtual = 1234.56m
        };
        _getProximosVencimentosQuery
            .Setup(q => q.ExecuteAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([movimento]);

        var result = await _controller.GetProximosVencimentos(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var resposta = Assert.IsType<List<ProximoVencimentoDto>>(ok.Value);
        var item = Assert.Single(resposta);
        Assert.Equal(movimento.Id, item.Id);
        Assert.Equal(movimento.ObjetivoNome, item.ObjetivoNome);
        Assert.Equal(movimento.DataVencimento, item.DataVencimento);
        Assert.Equal(movimento.ValorLiquidoAtual, item.ValorLiquidoAtual);
    }

    [Fact]
    public void PossuiFiltroDeValidacaoDeRequisicao()
    {
        Assert.Contains(typeof(RequestValidationAttribute), typeof(MovimentosController).GetCustomAttributes(true).Select(a => a.GetType()));
    }

    [Fact]
    public async Task GetMovimentosDesatualizados_RepassaUsuarioETokenEMapeiaLista()
    {
        using var cts = new CancellationTokenSource();
        _controller.HttpContext.RequestAborted = cts.Token;
        var movimento = new MovimentoDesatualizadoModel
        {
            Id = Guid.NewGuid(),
            ProdutoNome = "CDB",
            ValorLiquidoAtual = 1234.56m
        };
        _getMovimentosDesatualizadosQuery
            .Setup(q => q.ExecuteAsync(_userId, cts.Token))
            .ReturnsAsync([movimento]);

        var result = await _controller.GetMovimentosDesatualizados(cts.Token);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var resposta = Assert.IsType<List<MovimentoDesatualizadoDto>>(ok.Value);
        var item = Assert.Single(resposta);
        Assert.Equal(movimento.Id, item.Id);
        Assert.Equal(movimento.ProdutoNome, item.ProdutoNome);
        Assert.Equal(movimento.ValorLiquidoAtual, item.ValorLiquidoAtual);
        _getMovimentosDesatualizadosQuery.Verify(q => q.ExecuteAsync(_userId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateValorLiquidoMovimento_RepassaUsuarioIdETokenERetornaNoContent()
    {
        using var cts = new CancellationTokenSource();
        _controller.HttpContext.RequestAborted = cts.Token;
        var id = Guid.NewGuid();
        var dto = new MovimentoForUpdateValorLiquidoDto { ValorLiquidoAtual = 987.65m };

        var result = await _controller.UpdateValorLiquidoMovimento(id, dto, cts.Token);

        Assert.IsType<NoContentResult>(result);
        _updateValorLiquidoMovimentoCommand.Verify(c => c.ExecuteAsync(
            It.Is<MovimentoModelForUpdateValorLiquido>(m => m.ValorLiquidoAtual == dto.ValorLiquidoAtual),
            _userId,
            id,
            cts.Token), Times.Once);
    }
}
