using Application.Dashboards.FluxoMensal;
using Domain.Movimentos.FluxoMensal;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Dashboards.Queries.GetRetornoMensal;

public sealed class GetRetornoMensalQuery(
    IRepositoryManager repository,
    ILoggerManager logger,
    IDateTimeProvider dateTimeProvider) : IGetRetornoMensalQuery
{
    public async Task<List<RetornoMensalModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        try
        {
            var janela = JanelaMensal.Proximos(dateTimeProvider.GetDateTime());
            var itens = await repository.Movimento.GetRetornoPrevistoAsync(ownerId, janela[^1].Fim, cancellationToken);
            var totais = new decimal[janela.Count];
            var quantidades = new int[janela.Count];

            foreach (var item in itens)
            {
                var indice = item.DataVencimento < janela[0].Fim
                    ? 0
                    : janela.Select((mes, index) => (mes, index)).Where(x => x.mes.Contem(item.DataVencimento)).Select(x => x.index).DefaultIfEmpty(-1).First();
                if (indice < 0 || indice >= janela.Count)
                    continue;
                totais[indice] += item.ValorLiquidoAtual;
                quantidades[indice]++;
            }

            return [.. janela.Select((mes, i) => new RetornoMensalModel(mes.Ano, mes.Mes, totais[i], quantidades[i]))];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError($"Algo deu errado no método de serviço {nameof(GetRetornoMensalQuery)}: {ex}");
            throw;
        }
    }
}
