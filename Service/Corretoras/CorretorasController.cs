using Application.Corretoras.Commands.CreateCorretora;
using Application.Corretoras.Commands.UpdateCorretora;
using Application.Corretoras.Queries.GetCorretora;
using Application.Corretoras.Queries.GetCorretoraList;
using LumiaFoundation.Abstractions.ErrorModel;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.Auth.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Corretoras.DTOs;

namespace Service.Corretoras;

[ApiController]
[Authorize]
[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]
[Route("api/corretoras")]
public class CorretorasController(
    IGetCorretorasListQuery getCorretorasListQuery,
    IGetCorretoraQuery getCorretoraQuery,
    ICreateCorretoraCommand createCorretoraCommand,
    IUpdateCorretoraCommand updateCorretoraCommand) : BaseApiController
{
    private readonly IGetCorretorasListQuery _getCorretorasListQuery = getCorretorasListQuery;
    private readonly IGetCorretoraQuery _getCorretoraQuery = getCorretoraQuery;
    private readonly ICreateCorretoraCommand _createCorretoraCommand = createCorretoraCommand;
    private readonly IUpdateCorretoraCommand _updateCorretoraCommand = updateCorretoraCommand;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CorretoraDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CorretoraDto>>> GetCorretoras()
    {
        var userId = GetCurrentUserId();
        var corretoras = await _getCorretorasListQuery.ExecuteAsync(userId);

        return Ok(corretoras);
    }

    [HttpGet("{id:guid}", Name = "CorretoraById")]
    [ProducesResponseType(typeof(CorretoraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CorretoraDto>> GetCorretora(Guid id)
    {
        var userId = GetCurrentUserId();
        var corretora = await _getCorretoraQuery.ExecuteAsync(userId, id);

        return Ok(corretora);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CorretoraDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CorretoraDto>> CreateCorretora([FromBody] CorretoraForCreationDto corretora)
    {
        var createdCorretora = await _createCorretoraCommand.ExecuteAsync(corretora.ToCreateCorretoraCommand(), GetCurrentUserId());

        return CreatedAtRoute("CorretoraById", new { id = createdCorretora.Id }, createdCorretora);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CorretoraDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CorretoraDto>> UpdateCorretora(Guid id, [FromBody] CorretoraForUpdateDto corretora)
    {
        _ = await _updateCorretoraCommand.ExecuteAsync(corretora.ToUpdateCorretoraCommand(), GetCurrentUserId(), id);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userId = HttpContext.Items["UserId"]?.ToString();

        if (Guid.TryParse(userId, out var currentUserId))
            return currentUserId;

        throw new InvalidOperationException("Não foi possível identificar o usuário autenticado.");
    }
}
