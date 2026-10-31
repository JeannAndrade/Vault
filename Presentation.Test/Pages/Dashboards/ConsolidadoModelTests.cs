using System.Net;
using LumiaFoundation.Http.Client.Exceptions;
using Moq;
using Presentation.Dashboards;
using Presentation.Movimentos;
using Presentation.Pages.Dashboards;

namespace Presentation.Test.Pages.Dashboards;

public class ConsolidadoModelTests
{
    private readonly Mock<IConsolidadoApi> _api = new();
    private readonly Mock<IMovimentoFormLookupData> _lookup = new();
    private readonly MovimentoFormLookupData _dados = new([], [], [], [], []);

    public ConsolidadoModelTests() => _lookup.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_dados);

    [Fact]
    public async Task SemCriterioCarregaLookupENaoChamaApi() {
        var modelo = new ConsolidadoModel(_api.Object, _lookup.Object);
        await modelo.OnGetAsync(CancellationToken.None);
        Assert.Same(_dados, modelo.Lookup);
        Assert.Null(modelo.Resultado);
        _api.Verify(a => a.GetConsolidadoAsync(It.IsAny<ConsolidadoFiltro>(), It.IsAny<DimensaoConsolidado?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ComFiltroEGrupoRepassaArgumentosEResultado() {
        var resultado = new ConsolidadoDto { Total = new() { Quantidade = 1 }, Grupos = [new() { Nome = "XP" }] };
        using var cts = new CancellationTokenSource();
        _api.Setup(a => a.GetConsolidadoAsync(It.Is<ConsolidadoFiltro>(f => f.CorretoraId != null), DimensaoConsolidado.Corretora, cts.Token)).ReturnsAsync(resultado);
        var modelo = new ConsolidadoModel(_api.Object, _lookup.Object) { Filtro = new() { CorretoraId = Guid.NewGuid() }, AgruparPor = DimensaoConsolidado.Corretora };
        await modelo.OnGetAsync(cts.Token);
        Assert.Same(resultado, modelo.Resultado);
        Assert.True(modelo.TemGrupos);
    }

    [Fact]
    public async Task FalhaApiMostraMensagemSemLancar() {
        _api.Setup(a => a.GetConsolidadoAsync(It.IsAny<ConsolidadoFiltro>(), It.IsAny<DimensaoConsolidado?>(), It.IsAny<CancellationToken>())).ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, "erro"));
        var modelo = new ConsolidadoModel(_api.Object, _lookup.Object) { AgruparPor = DimensaoConsolidado.Produto };
        await modelo.OnGetAsync(CancellationToken.None);
        Assert.Null(modelo.Resultado);
        Assert.Contains("Não foi possível carregar", modelo.ErrorMessage);
    }
}
