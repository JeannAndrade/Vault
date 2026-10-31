using Application.Dashboards.Consolidado;
using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Validators;
using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;
using Persistence.Movimentos;

namespace Application.Dashboards.Queries.GetConsolidado;

public class GetConsolidadoQuery(
    IRepositoryManager repositoryManager,
    ILoggerManager logger) : IGetConsolidadoQuery
{
    public async Task<ConsolidadoModel> ExecuteAsync(
        Guid ownerId, ConsolidadoParameters parametros, CancellationToken cancellationToken)
    {
        CommandValidator.Validate(parametros);
        var filtro = parametros.ToFiltro();
        if (!filtro.TemFiltro)
            throw new CommandValidationException("Informe ao menos um filtro.");

        try
        {
            if (parametros.AgruparPor is null)
            {
                var agregado = await repositoryManager.Movimento.GetConsolidadoAsync(ownerId, filtro, cancellationToken);
                return new ConsolidadoModel(CriarLinha(null, null, agregado, agregado.TotalLiquido == 0 ? 0 : 1), []);
            }

            var grupos = await repositoryManager.Movimento.GetConsolidadoAgrupadoAsync(
                ownerId, filtro, parametros.AgruparPor.Value, cancellationToken);
            var quantidade = grupos.Sum(g => g.Quantidade);
            var aportado = grupos.Sum(g => g.TotalAportado);
            var liquido = grupos.Sum(g => g.TotalLiquido);
            var linhas = grupos.Select(g => new ConsolidadoLinhaModel(
                g.Id, g.Nome, g.Quantidade, g.TotalAportado, g.TotalLiquido,
                liquido == 0 ? 0 : g.TotalLiquido / liquido)).ToList();

            return new ConsolidadoModel(
                new ConsolidadoLinhaModel(null, null, quantidade, aportado, liquido, liquido == 0 ? 0 : 1), linhas);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError($"Algo deu errado no método de serviço {nameof(GetConsolidadoQuery)}: {ex}");
            throw;
        }
    }

    private static ConsolidadoLinhaModel CriarLinha(
        Guid? id, string? nome, ConsolidadoAgregado agregado, decimal percentual) =>
        new(id, nome, agregado.Quantidade, agregado.TotalAportado, agregado.TotalLiquido, percentual);
}
