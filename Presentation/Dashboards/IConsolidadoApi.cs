namespace Presentation.Dashboards;

public interface IConsolidadoApi
{
    Task<ConsolidadoDto> GetConsolidadoAsync(
        ConsolidadoFiltro filtro, DimensaoConsolidado? agruparPor, CancellationToken cancellationToken);
}
