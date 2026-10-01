using Microsoft.AspNetCore.Mvc.Rendering;

namespace Presentation.Movimentos;

public sealed record MovimentoFormLookupData(
    List<SelectListItem> Objetivos,
    List<SelectListItem> TiposRenda,
    List<SelectListItem> Corretoras,
    List<SelectListItem> Produtos,
    List<SelectListItem> Emissores);

public interface IMovimentoFormLookupData
{
    Task<MovimentoFormLookupData> LoadAsync(CancellationToken cancellationToken = default);
}
