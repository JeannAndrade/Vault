using Application.Movimentos.Commands.EncerrarMovimento;
using Application.Movimentos.Commands.ReativarMovimento;
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
[Route("api/movimentos/{id:guid}/encerramento")]
public class EncerramentoMovimentoController(
    IEncerrarMovimentoCommand encerrarMovimentoCommand,
    IReativarMovimentoCommand reativarMovimentoCommand) : BaseApiController
{
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Encerrar(
        Guid id,
        [FromBody] MovimentoForEncerramentoDto dto,
        CancellationToken cancellationToken)
    {
        await encerrarMovimentoCommand.ExecuteAsync(
            dto.ToEncerrarMovimentoCommand(),
            GetCurrentUserId(),
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Reativar(Guid id, CancellationToken cancellationToken)
    {
        await reativarMovimentoCommand.ExecuteAsync(GetCurrentUserId(), id, cancellationToken);

        return NoContent();
    }
}
