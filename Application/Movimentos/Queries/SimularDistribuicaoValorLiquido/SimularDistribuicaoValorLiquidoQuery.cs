using Application.Movimentos.DistribuicaoValorLiquido;
using Domain.Movimentos.Distribuicao;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Core.Validators;
using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Movimentos.Queries.SimularDistribuicaoValorLiquido;
public class SimularDistribuicaoValorLiquidoQuery(IRepositoryManager repository, ILoggerManager logger, IDateTimeProvider clock) : ISimularDistribuicaoValorLiquidoQuery
{
    public async Task<DistribuicaoValorLiquidoModel> ExecuteAsync(Guid ownerId, DistribuicaoValorLiquidoParameters parametros, CancellationToken cancellationToken)
    {
        CommandValidator.Validate(parametros);
        try
        {
            var hoje = clock.GetDateTime().Date;
            var movimentos = await repository.Movimento.GetAtivosDoTituloAsync(ownerId, parametros.ToTituloFiltro(), cancellationToken);
            var resultado = DistribuicaoValorLiquidoCalculador.Calcular(DistribuicaoAuxiliar.Aportes(movimentos), parametros.ValorLiquidoTotal, hoje);
            if (!resultado.Sucesso)
                return new([], 0, 0, parametros.ValorLiquidoTotal, null, DistribuicaoValorLiquidoModel.MensagemDoErro(resultado.Erro!.Value));
            var porId = DistribuicaoAuxiliar.PorId(movimentos);
            var aportes = resultado.Aportes!.Select(a => new AporteDistribuidoModel(a.Id, porId[a.Id].Objetivo?.Nome, porId[a.Id].DataInvestimento, a.ValorAporte, porId[a.Id].ValorLiquidoAtual, a.ValorLiquido, a.Ganho)).ToList();
            return new(aportes, resultado.TotalAportado, resultado.GanhoTotal, resultado.ValorLiquidoTotal, resultado.TaxaEfetivaAnual, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError($"Algo deu errado no método de serviço {nameof(SimularDistribuicaoValorLiquidoQuery)}: {ex}");
            throw;
        }
    }
}
