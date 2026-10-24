using Application.Dashboards.FluxoMensal;
using Domain.Movimentos.FluxoMensal;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Dashboards.Queries.GetInvestidoMensal;

public sealed class GetInvestidoMensalQuery(
    IRepositoryManager repository,
    ILoggerManager logger,
    IDateTimeProvider dateTimeProvider) : IGetInvestidoMensalQuery
{
    public async Task<List<InvestidoMensalModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        try
        {
            var janela = JanelaMensal.Anteriores(dateTimeProvider.GetDateTime());
            var itens = await repository.Movimento.GetInvestidoAsync(ownerId, janela[0].Inicio, janela[^1].Fim, cancellationToken);
            var novos = new decimal[janela.Count];
            var reinvestimentos = new decimal[janela.Count];

            foreach (var item in itens)
            {
                var indice = janela.Select((mes, index) => (mes, index)).Where(x => x.mes.Contem(item.DataInvestimento)).Select(x => x.index).DefaultIfEmpty(-1).First();
                if (indice < 0 || indice >= janela.Count)
                    continue;
                if (item.EhReinvestimento) reinvestimentos[indice] += item.ValorAporte;
                else novos[indice] += item.ValorAporte;
            }

            return [.. janela.Select((mes, i) => new InvestidoMensalModel(mes.Ano, mes.Mes, novos[i], reinvestimentos[i]))];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError($"Algo deu errado no método de serviço {nameof(GetInvestidoMensalQuery)}: {ex}");
            throw;
        }
    }
}
