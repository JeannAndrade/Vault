using Application.Emissores.Commands.CreateEmissor;
using Application.Emissores.Commands.UpdateEmissor;
using Application.Emissores.Queries.GetEmissor;
using Application.Emissores.Queries.GetEmissorList;
using LumiaFoundation.Abstractions.ErrorModel;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.Auth.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Emissores.DTOs;

namespace Service.Emissores;

[ApiController]
[Authorize]
[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]
[Route("api/emissores")]
public class EmissoresController(
    IGetEmissoresListQuery getEmissoresListQuery,
    IGetEmissorQuery getEmissorQuery,
    ICreateEmissorCommand createEmissorCommand,
    IUpdateEmissorCommand updateEmissorCommand) : BaseApiController
{
    private readonly IGetEmissoresListQuery _getEmissoresListQuery = getEmissoresListQuery;
    private readonly IGetEmissorQuery _getEmissorQuery = getEmissorQuery;
    private readonly ICreateEmissorCommand _createEmissorCommand = createEmissorCommand;
    private readonly IUpdateEmissorCommand _updateEmissorCommand = updateEmissorCommand;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmissorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<EmissorDto>>> GetEmissores()
    {
        var userId = GetCurrentUserId();
        var emissores = await _getEmissoresListQuery.ExecuteAsync(userId);

        return Ok(emissores);
    }

    [HttpGet("{id:guid}", Name = "EmissorById")]
    [ProducesResponseType(typeof(EmissorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmissorDto>> GetEmissor(Guid id)
    {
        var userId = GetCurrentUserId();
        var emissor = await _getEmissorQuery.ExecuteAsync(userId, id);

        return Ok(emissor);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmissorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmissorDto>> CreateEmissor([FromBody] EmissorForCreationDto emissor)
    {
        var createdEmissor = await _createEmissorCommand.ExecuteAsync(emissor.ToCreateEmissorCommand(GetCurrentUserId()));

        return CreatedAtRoute("EmissorById", new { id = createdEmissor.Id }, createdEmissor);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EmissorDto), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmissorDto>> UpdateEmissor(Guid id, [FromBody] EmissorForUpdateDto emissor)
    {
        _ = await _updateEmissorCommand.ExecuteAsync(emissor.ToUpdateEmissorCommand(), GetCurrentUserId(), id);

        return NoContent();
    }
}
