using Microsoft.AspNetCore.Mvc.Rendering;
using Presentation.Corretoras;
using Presentation.Emissores;
using Presentation.Objetivos;
using Presentation.Produtos;
using Presentation.TiposRenda;

namespace Presentation.Movimentos;

public sealed class MovimentoFormLookupDataProvider(
    IObjetivoApi objetivoApi,
    ITipoRendaApi tipoRendaApi,
    ICorretoraApi corretoraApi,
    IProdutoApi produtoApi,
    IEmissorApi emissorApi) : IMovimentoFormLookupData
{
    public async Task<MovimentoFormLookupData> LoadAsync(CancellationToken cancellationToken = default)
    {
        var objetivosTask = objetivoApi.GetAllAsync(cancellationToken);
        var tiposRendaTask = tipoRendaApi.GetAllAsync(cancellationToken);
        var corretorasTask = corretoraApi.GetAllAsync(cancellationToken);
        var produtosTask = produtoApi.GetAllAsync(cancellationToken);
        var emissoresTask = emissorApi.GetAllAsync(cancellationToken);

        await Task.WhenAll(objetivosTask, tiposRendaTask, corretorasTask, produtosTask, emissoresTask);

        return new MovimentoFormLookupData(
            ToSelectList(objetivosTask.Result, o => o.Id, o => o.Nome),
            ToSelectList(tiposRendaTask.Result, t => t.Id, t => t.Nome),
            ToSelectList(corretorasTask.Result, c => c.Id, c => c.Nome),
            ToSelectList(produtosTask.Result, p => p.Id, p => p.Nome),
            ToSelectList(emissoresTask.Result, e => e.Id, e => e.Nome));
    }

    private static List<SelectListItem> ToSelectList<T>(
        List<T> items, Func<T, Guid> idSelector, Func<T, string> nomeSelector) =>
        [.. items
            .OrderBy(nomeSelector)
            .Select(item => new SelectListItem(nomeSelector(item), idSelector(item).ToString()))];
}
