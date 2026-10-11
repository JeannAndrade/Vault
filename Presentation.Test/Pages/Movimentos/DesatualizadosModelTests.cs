using System.Net;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Movimentos;
using Presentation.Pages.Movimentos;

namespace Presentation.Test.Pages.Movimentos;

public class DesatualizadosModelTests
{
    private readonly Mock<IMovimentoApi> _movimentoApi = new();
    private readonly List<MovimentoDesatualizadoDto> _movimentos = [new() { Id = Guid.NewGuid() }];

    public DesatualizadosModelTests() =>
        _movimentoApi.Setup(a => a.GetDesatualizadosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_movimentos);

    [Fact]
    public async Task OnGetAsync_LoadsMovimentos()
    {
        var model = new DesatualizadosModel(_movimentoApi.Object);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Same(_movimentos, model.Movimentos);
    }

    [Fact]
    public async Task OnPostAtualizarAsync_WhenModelStateIsInvalid_DoesNotCallApiMarksRowAndReloads()
    {
        var id = _movimentos[0].Id;
        var model = new DesatualizadosModel(_movimentoApi.Object);
        model.ModelState.AddModelError("Input.ValorLiquidoAtual", "Valor inválido.");

        var result = await model.OnPostAtualizarAsync(id, CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal(id, model.IdComErro);
        Assert.Same(_movimentos, model.Movimentos);
        _movimentoApi.Verify(a => a.UpdateValorLiquidoAsync(It.IsAny<Guid>(), It.IsAny<MovimentoForUpdateValorLiquidoDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAtualizarAsync_WhenApiSucceeds_UpdatesValueAndRedirects()
    {
        var id = _movimentos[0].Id;
        var model = new DesatualizadosModel(_movimentoApi.Object)
        {
            Input = new DesatualizadosModel.InputModel { ValorLiquidoAtual = 250m }
        };

        var result = await model.OnPostAtualizarAsync(id, CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        _movimentoApi.Verify(a => a.UpdateValorLiquidoAsync(id,
            It.Is<MovimentoForUpdateValorLiquidoDto>(dto => dto.ValorLiquidoAtual == 250m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAtualizarAsync_WhenApiThrows_ShowsErrorAndReturnsPage()
    {
        var id = _movimentos[0].Id;
        _movimentoApi
            .Setup(a => a.UpdateValorLiquidoAsync(id, It.IsAny<MovimentoForUpdateValorLiquidoDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, "erro"));
        var model = new DesatualizadosModel(_movimentoApi.Object)
        {
            Input = new DesatualizadosModel.InputModel { ValorLiquidoAtual = 250m }
        };

        var result = await model.OnPostAtualizarAsync(id, CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Não foi possível atualizar o valor líquido agora. Tente novamente em instantes.", model.ErrorMessage);
        Assert.Same(_movimentos, model.Movimentos);
    }
}
