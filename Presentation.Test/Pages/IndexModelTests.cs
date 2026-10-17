using LumiaFoundation.Core.TimeService;
using Moq;
using Presentation.Movimentos;
using Presentation.Objetivos;
using Presentation.Pages;

namespace Presentation.Test.Pages;

public class IndexModelTests
{
    private readonly Mock<IObjetivoApi> _objetivoApi = new();
    private readonly Mock<IMovimentoApi> _movimentoApi = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    public IndexModelTests()
    {
        _movimentoApi
            .Setup(api => api.GetProximosVencimentosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task OnGetAsync_CalculaTotaisAPartirDosObjetivos()
    {
        var objetivos = new List<ResumoObjetivoDto>
        {
            CriarResumo(1, 10m, 8m),
            CriarResumo(3, 20m, 18m)
        };
        _objetivoApi.Setup(api => api.GetAllComValoresAsync(It.IsAny<CancellationToken>())).ReturnsAsync(objetivos);
        var modelo = CriarModelo();

        await modelo.OnGetAsync(CancellationToken.None);

        Assert.Same(objetivos, modelo.Objetivos);
        Assert.Equal(new TotaisObjetivos(4, 30m, 26m), modelo.Totais);
    }

    [Fact]
    public async Task OnGetAsync_SemObjetivos_TotaisZerados()
    {
        _objetivoApi.Setup(api => api.GetAllComValoresAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var modelo = CriarModelo();
        await modelo.OnGetAsync(CancellationToken.None);

        Assert.Equal(TotaisObjetivos.Vazio, modelo.Totais);
    }

    [Fact]
    public void Totais_AntesDoOnGet_EhVazio()
    {
        Assert.Equal(TotaisObjetivos.Vazio, CriarModelo().Totais);
    }

    private IndexModel CriarModelo() => new(_objetivoApi.Object, _movimentoApi.Object, _dateTimeProvider.Object);

    private static ResumoObjetivoDto CriarResumo(int movimentos, decimal aportado, decimal liquido) =>
        new(Guid.NewGuid(), "Objetivo", 0m, 0m, movimentos, aportado, liquido);
}
