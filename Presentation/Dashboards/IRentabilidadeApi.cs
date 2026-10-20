namespace Presentation.Dashboards;

public interface IRentabilidadeApi
{
    Task<List<RentabilidadeGrupoDto>> GetRentabilidadesAsync(
        int top,
        OrdemRentabilidade ordem,
        CancellationToken cancellationToken);
}
