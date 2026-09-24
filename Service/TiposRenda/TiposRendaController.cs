using Application.TiposRenda.Commands.CreateTipoRenda;
using Application.TiposRenda.Commands.UpdateTipoRenda;
using Application.TiposRenda.Queries.GetTipoRenda;
using Application.TiposRenda.Queries.GetTiposRendaList;
using LumiaFoundation.Abstractions.ErrorModel;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.Auth.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.TiposRenda.DTOs;

namespace Service.TiposRenda;

[ApiController]
[Authorize]
[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]
[Route("api/tiposrenda")]
public class TiposRendaController(
    IGetTiposRendaListQuery getTiposRendaListQuery,
    IGetTipoRendaQuery getTipoRendaQuery,
    ICreateTipoRendaCommand createTipoRendaCommand,
    IUpdateTipoRendaCommand updateTipoRendaCommand) : BaseApiController
{
    private readonly IGetTiposRendaListQuery _getTiposRendaListQuery = getTiposRendaListQuery;
    private readonly IGetTipoRendaQuery _getTipoRendaQuery = getTipoRendaQuery;
    private readonly ICreateTipoRendaCommand _createTipoRendaCommand = createTipoRendaCommand;
    private readonly IUpdateTipoRendaCommand _updateTipoRendaCommand = updateTipoRendaCommand;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TipoRendaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<TipoRendaDto>>> GetTiposRenda()
    {
        var userId = GetCurrentUserId();
        var tiposRenda = await _getTiposRendaListQuery.ExecuteAsync(userId);

        return Ok(tiposRenda);
    }

    [HttpGet("{id:guid}", Name = "TipoRendaById")]
    [ProducesResponseType(typeof(TipoRendaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TipoRendaDto>> GetTipoRenda(Guid id)
    {
        var userId = GetCurrentUserId();
        var tipoRenda = await _getTipoRendaQuery.ExecuteAsync(userId, id);

        return Ok(tipoRenda);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TipoRendaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TipoRendaDto>> CreateTipoRenda([FromBody] TipoRendaForCreationDto tipoRenda)
    {
        var createdTipoRenda = await _createTipoRendaCommand.ExecuteAsync(tipoRenda.ToCreateTipoRendaCommand(GetCurrentUserId()));

        return CreatedAtRoute("TipoRendaById", new { id = createdTipoRenda.Id }, createdTipoRenda);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TipoRendaDto), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TipoRendaDto>> UpdateTipoRenda(Guid id, [FromBody] TipoRendaForUpdateDto tipoRenda)
    {
        _ = await _updateTipoRendaCommand.ExecuteAsync(tipoRenda.ToUpdateTipoRendaCommand(), GetCurrentUserId(), id);

        return NoContent();
    }
}
