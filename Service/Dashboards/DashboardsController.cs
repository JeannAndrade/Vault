using Application.Dashboards.Queries.GetRentabilidadesPorTipoRenda;
using Application.Dashboards.Queries.GetRetornoMensal;
using Application.Dashboards.Queries.GetInvestidoMensal;
using Application.Dashboards.Queries.GetConsolidado;
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
    IGetRentabilidadesPorTipoRendaQuery getRentabilidadesPorTipoRendaQuery,
    IGetRetornoMensalQuery getRetornoMensalQuery,
    IGetInvestidoMensalQuery getInvestidoMensalQuery,
    IGetConsolidadoQuery getConsolidadoQuery) : BaseApiController
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

    [HttpGet("consolidado")]
    [ProducesResponseType(typeof(ConsolidadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConsolidadoDto>> GetConsolidado(
        [FromQuery] ConsolidadoFiltroDto filtro, CancellationToken cancellationToken)
    {
        filtro ??= new ConsolidadoFiltroDto();
        var modelo = await getConsolidadoQuery.ExecuteAsync(
            GetCurrentUserId(), filtro.ToParameters(), cancellationToken);
        return Ok(ConsolidadoDto.FromApplication(modelo));
    }

    [HttpGet("retorno-mensal")]
    [ProducesResponseType(typeof(List<RetornoMensalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<RetornoMensalDto>>> GetRetornoMensal(CancellationToken cancellationToken)
    {
        var modelos = await getRetornoMensalQuery.ExecuteAsync(GetCurrentUserId(), cancellationToken);
        return Ok(RetornoMensalDto.FromApplication(modelos));
    }

    [HttpGet("investido-mensal")]
    [ProducesResponseType(typeof(List<InvestidoMensalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<InvestidoMensalDto>>> GetInvestidoMensal(CancellationToken cancellationToken)
    {
        var modelos = await getInvestidoMensalQuery.ExecuteAsync(GetCurrentUserId(), cancellationToken);
        return Ok(InvestidoMensalDto.FromApplication(modelos));
    }
}
