using Application.Movimentos;
using Application.Movimentos.Commands.CreateMovimento;
using Application.Movimentos.Commands.DeleteMovimento;
using Application.Movimentos.Commands.UpdateMovimento;
using Application.Movimentos.Queries.GetMovimento;
using Application.Movimentos.Queries.GetMovimentoList;
using Application.Movimentos.Queries.GetProximosVencimentos;
using Application.Pagination;
using LumiaFoundation.Abstractions.Pagination;
using LumiaFoundation.AspNetCore.Commons.Exceptions;
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
            new Mock<IDeleteMovimentoCommand>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _controller.HttpContext.Items["UserId"] = _userId.ToString();
    }

    [Fact]
    public async Task GetMovimentos_RepassaPaginacaoEMapeiaEnvelope()
    {
        using var cts = new CancellationTokenSource();
        var pagina = new PagedList<MovimentoModel>(
            [new MovimentoModel { Id = Guid.NewGuid(), UserId = _userId }], page: 2, pageSize: 10, totalCount: 11);
        _getMovimentosListQuery
            .Setup(q => q.ExecuteAsync(_userId, It.Is<PaginationParameters>(p => p.Page == 2 && p.PageSize == 10), cts.Token))
            .ReturnsAsync(pagina);

        var result = await _controller.GetMovimentos(2, 10, cts.Token);

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
            .Setup(q => q.ExecuteAsync(_userId))
            .ReturnsAsync([movimento]);

        var result = await _controller.GetProximosVencimentos();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var resposta = Assert.IsType<List<ProximoVencimentoDto>>(ok.Value);
        var item = Assert.Single(resposta);
        Assert.Equal(movimento.Id, item.Id);
        Assert.Equal(movimento.ObjetivoNome, item.ObjetivoNome);
        Assert.Equal(movimento.DataVencimento, item.DataVencimento);
        Assert.Equal(movimento.ValorLiquidoAtual, item.ValorLiquidoAtual);
    }

    [Fact]
    public async Task GetMovimentos_ComParametroNaoNumerico_Lanca422SemConsultar()
    {
        _controller.ModelState.AddModelError("page", "valor inválido");

        var exception = await Assert.ThrowsAsync<HttpBaseException>(() => _controller.GetMovimentos(1, 20));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, exception.StatusCode);
        Assert.Contains("page", exception.Message);
        _getMovimentosListQuery.Verify(
            q => q.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
