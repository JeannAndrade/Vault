using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Movimentos;
using Presentation.Pages.Movimentos;

namespace Presentation.Test.Pages.Movimentos;

public class CreateModelTests
{
    private readonly Mock<IMovimentoApi> _movimentoApi = new();
    private readonly Mock<IMovimentoFormLookupData> _lookupData = new();

    public CreateModelTests()
    {
        _lookupData
            .Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovimentoFormLookupData([], [], [], [], []));
    }

    private CreateModel.InputModel ValidInput() => new()
    {
        ObjetivoId = Guid.NewGuid(),
        TipoRendaId = Guid.NewGuid(),
        CorretoraId = Guid.NewGuid(),
        ProdutoId = Guid.NewGuid(),
        EmissorId = Guid.NewGuid(),
        DataInvestimento = DateTime.Today,
        ValorAporte = 1000m,
        ValorLiquidoAtual = 1000m
    };

    [Fact]
    public async Task OnGetAsync_DefaultsEstaAtivoToTrueAndDataInvestimentoToToday()
    {
        var model = new CreateModel(_movimentoApi.Object, _lookupData.Object);

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.Input.EstaAtivo);
        Assert.Equal(DateTime.Today, model.Input.DataInvestimento);
    }

    [Fact]
    public async Task OnPostAsync_WhenModelStateIsInvalid_ReturnsPageWithoutCallingApi()
    {
        var model = new CreateModel(_movimentoApi.Object, _lookupData.Object) { Input = ValidInput() };
        model.ModelState.AddModelError("Input.ObjetivoId", "Selecione o objetivo.");

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        _movimentoApi.Verify(a => a.CreateAsync(It.IsAny<MovimentoForCreationDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_WhenApiSucceeds_RedirectsToIndex()
    {
        var model = new CreateModel(_movimentoApi.Object, _lookupData.Object) { Input = ValidInput() };

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);
        Assert.Equal(1, redirect.RouteValues!["pagina"]);   // sem página de origem, volta para a primeira
    }

    [Fact]
    public async Task OnPostAsync_WhenApiSucceeds_RedirectsToIndexKeepingOriginPage()
    {
        var model = new CreateModel(_movimentoApi.Object, _lookupData.Object) { Input = ValidInput(), Pagina = 4 };

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);
        Assert.Equal(4, redirect.RouteValues!["pagina"]);
    }
}
