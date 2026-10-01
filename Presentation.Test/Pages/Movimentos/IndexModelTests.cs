using System.Net;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Movimentos;
using Presentation.Pages.Movimentos;

namespace Presentation.Test.Pages.Movimentos;

public class IndexModelTests
{
    [Fact]
    public async Task OnGetAsync_LoadsMovimentosFromApi()
    {
        var movimentoApi = new Mock<IMovimentoApi>();
        var expected = new List<MovimentoDto> { new() { Id = Guid.NewGuid() } };
        movimentoApi.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var model = new IndexModel(movimentoApi.Object);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Same(expected, model.Movimentos);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenApiThrows_ShowsErrorMessageAndReloadsList()
    {
        var movimentoApi = new Mock<IMovimentoApi>();
        movimentoApi
            .Setup(a => a.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, "erro"));
        movimentoApi.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var model = new IndexModel(movimentoApi.Object);

        var result = await model.OnPostDeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.ErrorMessage);
    }
}
