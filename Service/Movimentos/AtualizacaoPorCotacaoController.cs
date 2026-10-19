using Application.Movimentos.Commands.AtualizarPorCotacao;
using Application.Movimentos.Queries.GetAtualizacaoPorCotacao;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.Auth.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Extensions;
using Service.Movimentos.DTOs;

namespace Service.Movimentos;

[ApiController]
[Authorize]
[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]
[Route("api/movimentos/atualizacao-por-cotacao")]
public class AtualizacaoPorCotacaoController(
    IGetAtualizacaoPorCotacaoQuery query,
    IAtualizarPorCotacaoCommand command) : BaseApiController
{
    private readonly IGetAtualizacaoPorCotacaoQuery _query = query;
    private readonly IAtualizarPorCotacaoCommand _command = command;

    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken cancellationToken)
    {
        var papeis = await _query.ExecuteAsync(GetCurrentUserId(), cancellationToken);
        return Ok(PapelParaCotacaoDto.FromApplication(papeis));
    }

    [HttpPost]
    public async Task<ActionResult> Post([FromBody] AtualizacaoPorCotacaoSolicitacaoDto? dto, CancellationToken cancellationToken)
    {
        this.ValidarCorpo(dto);

        var resultado = await _command.ExecuteAsync(
            dto!.ToParameters(),
            GetCurrentUserId(),
            cancellationToken);

        return Ok(AtualizacaoPorCotacaoResultadoDto.FromApplication(resultado));
    }
}
