using Application.Movimentos.Commands.DistribuirValorLiquido;
using Application.Movimentos.Queries.SimularDistribuicaoValorLiquido;
using LumiaFoundation.Abstractions.ErrorModel;
using LumiaFoundation.AspNetCore.ActionFilters;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.Auth.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Movimentos.DTOs;

namespace Service.Movimentos;

[ApiController]
[Authorize]
[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]
[RequestValidation]
[Route("api/movimentos/distribuicao-valor-liquido")]
public class DistribuicaoValorLiquidoController(
    ISimularDistribuicaoValorLiquidoQuery simulacao,
    IDistribuirValorLiquidoCommand distribuicao) : BaseApiController
{
    [HttpPost("simulacao")]
    [ProducesResponseType(typeof(DistribuicaoValorLiquidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DistribuicaoValorLiquidoDto>> Simular(
        [FromBody] DistribuicaoValorLiquidoSolicitacaoDto dto)
    {
        var model = await simulacao.ExecuteAsync(GetCurrentUserId(), dto.ToParameters(), HttpContext.RequestAborted);
        return Ok(DistribuicaoValorLiquidoDto.FromApplication(model));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Distribuir([FromBody] DistribuicaoValorLiquidoSolicitacaoDto dto)
    {
        await distribuicao.ExecuteAsync(dto.ToParameters(), GetCurrentUserId(), HttpContext.RequestAborted);
        return NoContent();
    }

}
