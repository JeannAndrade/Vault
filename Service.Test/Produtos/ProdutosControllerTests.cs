using Application.Produtos;
using Application.Produtos.Commands.CreateProduto;
using Application.Produtos.Commands.DeleteProduto;
using Application.Produtos.Commands.UpdateProduto;
using Application.Produtos.Queries.GetProduto;
using Application.Produtos.Queries.GetProdutoList;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Produtos;
using Service.Produtos.DTOs;

namespace Service.Test.Produtos;

public class ProdutosControllerTests
{
    private readonly Mock<IGetProdutosListQuery> _getProdutosListQuery = new();
    private readonly Mock<IGetProdutoQuery> _getProdutoQuery = new();
    private readonly Mock<ICreateProdutoCommand> _createProdutoCommand = new();
    private readonly Mock<IUpdateProdutoCommand> _updateProdutoCommand = new();
    private readonly Mock<IDeleteProdutoCommand> _deleteProdutoCommand = new();
    private readonly ProdutosController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public ProdutosControllerTests()
    {
        _controller = new ProdutosController(
            _getProdutosListQuery.Object,
            _getProdutoQuery.Object,
            _createProdutoCommand.Object,
            _updateProdutoCommand.Object,
            _deleteProdutoCommand.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _controller.HttpContext.Items["UserId"] = _userId.ToString();
    }

    [Fact]
    public async Task GetProdutos_ReturnsDtosWithoutLeakingUserId()
    {
        var produtos = new List<ProdutoModel> { new() { Id = Guid.NewGuid(), Nome = "Tesouro Selic", UserId = _userId } };
        _getProdutosListQuery.Setup(q => q.ExecuteAsync(_userId)).ReturnsAsync(produtos);

        var result = await _controller.GetProdutos();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<List<ProdutoDto>>(ok.Value);
        Assert.Single(dtos);
        Assert.Equal("Tesouro Selic", dtos[0].Nome);
    }

    [Fact]
    public async Task GetProduto_ReturnsDtoMappedFromApplicationModel()
    {
        var produtoId = Guid.NewGuid();
        var produto = new ProdutoModel { Id = produtoId, Nome = "Tesouro Selic", UserId = _userId };
        _getProdutoQuery.Setup(q => q.ExecuteAsync(_userId, produtoId)).ReturnsAsync(produto);

        var result = await _controller.GetProduto(produtoId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProdutoDto>(ok.Value);
        Assert.Equal("Tesouro Selic", dto.Nome);
    }

    [Fact]
    public async Task CreateProduto_ReturnsDtoMappedFromApplicationModel()
    {
        var created = new ProdutoModel { Id = Guid.NewGuid(), Nome = "Tesouro Selic", UserId = _userId };
        _createProdutoCommand
            .Setup(c => c.ExecuteAsync(It.IsAny<ProdutoModelForCreation>()))
            .ReturnsAsync(created);

        var result = await _controller.CreateProduto(new ProdutoForCreationDto("Tesouro Selic"));

        var createdResult = Assert.IsType<CreatedAtRouteResult>(result.Result);
        var dto = Assert.IsType<ProdutoDto>(createdResult.Value);
        Assert.Equal("Tesouro Selic", dto.Nome);
    }

    [Fact]
    public async Task DeleteProduto_CallsCommandAndReturnsNoContent()
    {
        var produtoId = Guid.NewGuid();

        var result = await _controller.DeleteProduto(produtoId);

        Assert.IsType<NoContentResult>(result);
        _deleteProdutoCommand.Verify(c => c.ExecuteAsync(_userId, produtoId), Times.Once);
    }
}
