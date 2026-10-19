using Application.Cotacoes.Commands.CreateCotacao;
using Application.Cotacoes.Commands.DeleteCotacao;
using Application.Cotacoes.Commands.UpdateCotacao;
using Application.Cotacoes.Queries.GetCotacao;
using Application.Cotacoes.Queries.GetCotacaoList;
using Application.Cotacoes.Queries.GetPapeisCotados;
using Application.Cotacoes.Queries.GetSerieCotacao;
using Application.Pagination;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.AspNetCore.Commons.Exceptions;
using LumiaFoundation.AspNetCore.Pagination;
using LumiaFoundation.Auth.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Cotacoes.DTOs;
using Service.Extensions;

namespace Service.Cotacoes;

[ApiController]
[Authorize]
[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]
[Route("api/cotacoes")]
public class CotacoesController(
    IGetCotacaoListQuery getCotacaoListQuery,
    IGetCotacaoQuery getCotacaoQuery,
    IGetPapeisCotadosQuery getPapeisCotadosQuery,
    IGetSerieCotacaoQuery getSerieCotacaoQuery,
    ICreateCotacaoCommand createCotacaoCommand,
    IUpdateCotacaoCommand updateCotacaoCommand,
    IDeleteCotacaoCommand deleteCotacaoCommand) : BaseApiController
{
    private readonly IGetCotacaoListQuery _getCotacaoListQuery = getCotacaoListQuery;
    private readonly IGetCotacaoQuery _getCotacaoQuery = getCotacaoQuery;
    private readonly IGetPapeisCotadosQuery _getPapeisCotadosQuery = getPapeisCotadosQuery;
    private readonly IGetSerieCotacaoQuery _getSerieCotacaoQuery = getSerieCotacaoQuery;
    private readonly ICreateCotacaoCommand _createCotacaoCommand = createCotacaoCommand;
    private readonly IUpdateCotacaoCommand _updateCotacaoCommand = updateCotacaoCommand;
    private readonly IDeleteCotacaoCommand _deleteCotacaoCommand = deleteCotacaoCommand;

    [HttpGet]
    public async Task<ActionResult> Get(
        [FromQuery] string? papel,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationParameters.PageSizeDefault)
    {
        if (!ModelState.IsValid)
            throw new HttpBaseException(422, "Parâmetros inválidos.");

        var cotacoes = await _getCotacaoListQuery.ExecuteAsync(
            GetCurrentUserId(),
            papel,
            new PaginationParameters { Page = page, PageSize = pageSize },
            cancellationToken);

        return Ok(cotacoes.Map(CotacaoDto.FromApplication).ToPagedResponse());
    }

    [HttpGet("papeis")]
    public async Task<ActionResult> GetPapeis(CancellationToken cancellationToken)
    {
        var papeis = await _getPapeisCotadosQuery.ExecuteAsync(GetCurrentUserId(), cancellationToken);
        return Ok(papeis);
    }

    [HttpGet("serie")]
    public async Task<ActionResult> GetSerie([FromQuery] string papel, CancellationToken cancellationToken)
    {
        var serie = await _getSerieCotacaoQuery.ExecuteAsync(GetCurrentUserId(), papel, cancellationToken);
        return Ok(CotacaoPontoDto.FromApplication(serie));
    }

    [HttpGet("{id:guid}", Name = "CotacaoById")]
    public async Task<ActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var cotacao = await _getCotacaoQuery.ExecuteAsync(GetCurrentUserId(), id, cancellationToken);
        return Ok(CotacaoDto.FromApplication(cotacao));
    }

    [HttpPost]
    public async Task<ActionResult> Post([FromBody] CotacaoForCreationDto? dto, CancellationToken cancellationToken)
    {
        this.ValidarCorpo(dto);

        var cotacaoCriada = await _createCotacaoCommand.ExecuteAsync(
            dto!.ToCreateCotacaoCommand(),
            GetCurrentUserId(),
            cancellationToken);

        return CreatedAtRoute("CotacaoById", new { id = cotacaoCriada.Id }, CotacaoDto.FromApplication(cotacaoCriada));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] CotacaoForUpdateDto? dto, CancellationToken cancellationToken)
    {
        this.ValidarCorpo(dto);

        await _updateCotacaoCommand.ExecuteAsync(
            dto!.ToUpdateCotacaoCommand(),
            GetCurrentUserId(),
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _deleteCotacaoCommand.ExecuteAsync(GetCurrentUserId(), id, cancellationToken);
        return NoContent();
    }
}
