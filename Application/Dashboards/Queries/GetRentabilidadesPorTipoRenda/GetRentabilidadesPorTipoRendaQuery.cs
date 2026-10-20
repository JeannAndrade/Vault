using Application.Dashboards.Rentabilidade;
using Domain.Movimentos.Rentabilidade;
using LumiaFoundation.Core.Validators;
using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Dashboards.Queries.GetRentabilidadesPorTipoRenda;

public class GetRentabilidadesPorTipoRendaQuery(
    IRepositoryManager repositoryManager,
    ILoggerManager logger) : IGetRentabilidadesPorTipoRendaQuery
{
    public async Task<List<RentabilidadeGrupoModel>> ExecuteAsync(
        Guid ownerId,
        RentabilidadeParameters parametros,
        CancellationToken cancellationToken)
    {
        CommandValidator.Validate(parametros);

        try
        {
            var movimentos = await repositoryManager.Movimento
                .GetAtivosParaRankingAsync(ownerId, cancellationToken);

            var elegiveis = movimentos
                .Select(movimento =>
                {
                    var rentabilidade = RentabilidadeCalculador.Calcular(
                        movimento.ValorAporte,
                        movimento.ValorLiquidoAtual,
                        movimento.DataInvestimento,
                        movimento.DataAtualizacaoValorLiquido);
                    return (Movimento: movimento, Rentabilidade: rentabilidade);
                })
                .Where(item => item.Rentabilidade is not null && item.Rentabilidade.ElegivelParaRanking);

            return elegiveis
                .GroupBy(item => new { item.Movimento.TipoRendaId, Nome = item.Movimento.TipoRenda?.Nome })
                .Select(grupo => new RentabilidadeGrupoModel(
                    grupo.Key.TipoRendaId,
                    grupo.Key.Nome,
                    [.. (parametros.Ordem == OrdemRentabilidade.Maiores
                        ? grupo.OrderByDescending(item => item.Rentabilidade!.Anualizada)
                        : grupo.OrderBy(item => item.Rentabilidade!.Anualizada))
                        .ThenBy(item => item.Movimento.Id)
                        .Take(parametros.Top)
                        .Select(item => RentabilidadeItemModel.FromDomain(item.Movimento, item.Rentabilidade!))]))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError($"Algo deu errado no método de serviço {nameof(GetRentabilidadesPorTipoRendaQuery)}: {ex}");
            throw;
        }
    }
}
