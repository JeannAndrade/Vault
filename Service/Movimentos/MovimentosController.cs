using Application.Movimentos.Commands.CreateMovimento;
using Application.Movimentos.Commands.DeleteMovimento;
using Application.Movimentos.Commands.UpdateMovimento;
using Application.Movimentos.Queries.GetMovimento;
using Application.Movimentos.Queries.GetMovimentoList;
using LumiaFoundation.Abstractions.ErrorModel;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.Auth.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Movimentos.DTOs;

namespace Service.Movimentos;

[ApiController]
[Authorize]
[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]
[Route("api/movimentos")]
public class MovimentosController(
    IGetMovimentosListQuery getMovimentosListQuery,
    IGetMovimentoQuery getMovimentoQuery,
    ICreateMovimentoCommand createMovimentoCommand,
    IUpdateMovimentoCommand updateMovimentoCommand,
    IDeleteMovimentoCommand deleteMovimentoCommand) : BaseApiController
{
    private readonly IGetMovimentosListQuery _getMovimentosListQuery = getMovimentosListQuery;
    private readonly IGetMovimentoQuery _getMovimentoQuery = getMovimentoQuery;
    private readonly ICreateMovimentoCommand _createMovimentoCommand = createMovimentoCommand;
    private readonly IUpdateMovimentoCommand _updateMovimentoCommand = updateMovimentoCommand;
    private readonly IDeleteMovimentoCommand _deleteMovimentoCommand = deleteMovimentoCommand;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MovimentoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MovimentoDto>>> GetMovimentos()
    {
        var userId = GetCurrentUserId();
        var movimentos = await _getMovimentosListQuery.ExecuteAsync(userId);

        return Ok(MovimentoDto.FromApplication(movimentos));
    }

    [HttpGet("{id:guid}", Name = "MovimentoById")]
    [ProducesResponseType(typeof(MovimentoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MovimentoDto>> GetMovimento(Guid id)
    {
        var userId = GetCurrentUserId();
        var movimento = await _getMovimentoQuery.ExecuteAsync(userId, id);

        return Ok(MovimentoDto.FromApplication(movimento));
    }

    [HttpPost]
    [ProducesResponseType(typeof(MovimentoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MovimentoDto>> CreateMovimento([FromBody] MovimentoForCreationDto movimento)
    {
        var createdMovimento = await _createMovimentoCommand.ExecuteAsync(movimento.ToCreateMovimentoCommand(), GetCurrentUserId());

        return CreatedAtRoute("MovimentoById", new { id = createdMovimento.Id }, MovimentoDto.FromApplication(createdMovimento));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MovimentoDto), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MovimentoDto>> UpdateMovimento(Guid id, [FromBody] MovimentoForUpdateDto movimento)
    {
        _ = await _updateMovimentoCommand.ExecuteAsync(movimento.ToUpdateMovimentoCommand(), GetCurrentUserId(), id);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMovimento(Guid id)
    {
        await _deleteMovimentoCommand.ExecuteAsync(GetCurrentUserId(), id);

        return NoContent();
    }
}
