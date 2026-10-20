using Application.Dashboards.Queries.GetRentabilidadesPorTipoRenda;
using LumiaFoundation.Abstractions.ErrorModel;
using LumiaFoundation.AspNetCore.ActionFilters;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.Auth.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Dashboards.DTOs;

namespace Service.Dashboards;

[ApiController]
[Authorize]
[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]
[RequestValidation]
[Route("api/dashboards")]
public class DashboardsController(
    IGetRentabilidadesPorTipoRendaQuery getRentabilidadesPorTipoRendaQuery) : BaseApiController
{
    [HttpGet("rentabilidade")]
    [ProducesResponseType(typeof(List<RentabilidadeGrupoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<RentabilidadeGrupoDto>>> GetRentabilidade(
        [FromQuery] RentabilidadeFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        filtro ??= new RentabilidadeFiltroDto();

        var grupos = await getRentabilidadesPorTipoRendaQuery.ExecuteAsync(
            GetCurrentUserId(), filtro.ToParameters(), cancellationToken);

        return Ok(RentabilidadeGrupoDto.FromApplication(grupos));
    }
}
