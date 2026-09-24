using Application.Objetivos.Commands.CreateObjetivo;
using Application.Objetivos.Commands.UpdateObjetivo;
using Application.Objetivos.Queries.GetObjetivo;
using Application.Objetivos.Queries.GetObjetivoList;
using LumiaFoundation.Abstractions.ErrorModel;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.Auth.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Objetivos.DTOs;

namespace Service.Objetivos;

[ApiController]
[Authorize]
[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]
[Route("api/objetivos")]
public class ObjetivosController(
    IGetObjetivosListQuery getObjetivosListQuery,
    IGetObjetivoQuery getObjetivoQuery,
    ICreateObjetivoCommand createObjetivoCommand,
    IUpdateObjetivoCommand updateObjetivoCommand) : BaseApiController
{
    private readonly IGetObjetivosListQuery _getObjetivosListQuery = getObjetivosListQuery;
    private readonly IGetObjetivoQuery _getObjetivoQuery = getObjetivoQuery;
    private readonly ICreateObjetivoCommand _createObjetivoCommand = createObjetivoCommand;
    private readonly IUpdateObjetivoCommand _updateObjetivoCommand = updateObjetivoCommand;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ObjetivoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ObjetivoDto>>> GetObjetivos()
    {
        var userId = GetCurrentUserId();
        var objetivos = await _getObjetivosListQuery.ExecuteAsync(userId);

        return Ok(objetivos);
    }

    [HttpGet("{id:guid}", Name = "ObjetivoById")]
    [ProducesResponseType(typeof(ObjetivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ObjetivoDto>> GetObjetivo(Guid id)
    {
        var userId = GetCurrentUserId();
        var objetivo = await _getObjetivoQuery.ExecuteAsync(userId, id);

        return Ok(objetivo);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ObjetivoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ObjetivoDto>> CreateObjetivo([FromBody] ObjetivoForCreationDto objetivo)
    {
        var createdObjetivo = await _createObjetivoCommand.ExecuteAsync(objetivo.ToCreateObjetivoCommand(GetCurrentUserId()));

        return CreatedAtRoute("ObjetivoById", new { id = createdObjetivo.Id }, createdObjetivo);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ObjetivoDto), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ObjetivoDto>> UpdateObjetivo(Guid id, [FromBody] ObjetivoForUpdateDto objetivo)
    {
        _ = await _updateObjetivoCommand.ExecuteAsync(objetivo.ToUpdateObjetivoCommand(), GetCurrentUserId(), id);

        return NoContent();
    }
}
