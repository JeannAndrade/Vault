using Application.Produtos.Commands.CreateProduto;
using Application.Produtos.Commands.UpdateProduto;
using Application.Produtos.Queries.GetProduto;
using Application.Produtos.Queries.GetProdutoList;
using LumiaFoundation.Abstractions.ErrorModel;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.Auth.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Produtos.DTOs;

namespace Service.Produtos;

[ApiController]
[Authorize]
[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]
[Route("api/produtos")]
public class ProdutosController(
    IGetProdutosListQuery getProdutosListQuery,
    IGetProdutoQuery getProdutoQuery,
    ICreateProdutoCommand createProdutoCommand,
    IUpdateProdutoCommand updateProdutoCommand) : BaseApiController
{
    private readonly IGetProdutosListQuery _getProdutosListQuery = getProdutosListQuery;
    private readonly IGetProdutoQuery _getProdutoQuery = getProdutoQuery;
    private readonly ICreateProdutoCommand _createProdutoCommand = createProdutoCommand;
    private readonly IUpdateProdutoCommand _updateProdutoCommand = updateProdutoCommand;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProdutoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ProdutoDto>>> GetProdutos()
    {
        var userId = GetCurrentUserId();
        var produtos = await _getProdutosListQuery.ExecuteAsync(userId);

        return Ok(produtos);
    }

    [HttpGet("{id:guid}", Name = "ProdutoById")]
    [ProducesResponseType(typeof(ProdutoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProdutoDto>> GetProduto(Guid id)
    {
        var userId = GetCurrentUserId();
        var produto = await _getProdutoQuery.ExecuteAsync(userId, id);

        return Ok(produto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProdutoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProdutoDto>> CreateProduto([FromBody] ProdutoForCreationDto produto)
    {
        var createdProduto = await _createProdutoCommand.ExecuteAsync(produto.ToCreateProdutoCommand(GetCurrentUserId()));

        return CreatedAtRoute("ProdutoById", new { id = createdProduto.Id }, createdProduto);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProdutoDto), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProdutoDto>> UpdateProduto(Guid id, [FromBody] ProdutoForUpdateDto produto)
    {
        _ = await _updateProdutoCommand.ExecuteAsync(produto.ToUpdateProdutoCommand(), GetCurrentUserId(), id);

        return NoContent();
    }
}
