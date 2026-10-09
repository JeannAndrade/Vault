using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Movimentos;
using Presentation.Pages.Movimentos;

namespace Presentation.Test.Pages.Movimentos;

public class EditModelTests
{
    private readonly Mock<IMovimentoApi> _movimentoApi = new();
    private readonly Mock<IMovimentoFormLookupData> _lookupData = new();
    private readonly Guid _id = Guid.NewGuid();

    public EditModelTests()
    {
        _lookupData
            .Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovimentoFormLookupData([], [], [], [], []));
    }

    [Fact]
    public async Task OnGetAsync_MapsMovimentoIntoInput()
    {
        var movimento = new MovimentoDto
        {
            Id = _id,
            ObjetivoId = Guid.NewGuid(),
            TipoRendaId = Guid.NewGuid(),
            CorretoraId = Guid.NewGuid(),
            ProdutoId = Guid.NewGuid(),
            EmissorId = Guid.NewGuid(),
            DataInvestimento = new DateTime(2025, 1, 10),
            DataVencimento = new DateTime(2030, 1, 10),
            ValorAporte = 500m,
            ValorLiquidoAtual = 520m,
            EstaAtivo = true
        };
        _movimentoApi.Setup(a => a.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(movimento);
        var model = new EditModel(_movimentoApi.Object, _lookupData.Object) { Id = _id };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(movimento.ObjetivoId, model.Input.ObjetivoId);
        Assert.Equal(movimento.DataVencimento, model.Input.DataVencimento);
        Assert.Equal(movimento.ValorLiquidoAtual, model.Input.ValorLiquidoAtual);
    }

    [Fact]
    public async Task OnPostAsync_WhenApiSucceeds_RedirectsToIndexKeepingOriginPage()
    {
        var model = new EditModel(_movimentoApi.Object, _lookupData.Object)
        {
            Id = _id,
            Pagina = 3,
            Input = new CreateModel.InputModel
            {
                ObjetivoId = Guid.NewGuid(),
                TipoRendaId = Guid.NewGuid(),
                CorretoraId = Guid.NewGuid(),
                ProdutoId = Guid.NewGuid(),
                EmissorId = Guid.NewGuid(),
                DataInvestimento = DateTime.Today,
                ValorAporte = 1000m,
                ValorLiquidoAtual = 1000m
            }
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);
        Assert.Equal(3, redirect.RouteValues!["pagina"]);
        _movimentoApi.Verify(a => a.UpdateAsync(_id, It.IsAny<MovimentoForUpdateDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
