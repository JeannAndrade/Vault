using System.Net;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Corretoras;
using Presentation.Pages.Corretoras;

namespace Presentation.Test.Pages.Corretoras;

public class IndexModelTests
{
    [Fact]
    public async Task OnGetAsync_LoadsCorretorasFromApi()
    {
        var corretoraApi = new Mock<ICorretoraApi>();
        var expected = new List<CorretoraDto> { new(Guid.NewGuid(), "XP") };
        corretoraApi.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var model = new IndexModel(corretoraApi.Object);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Same(expected, model.Corretoras);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenApiSucceeds_ReloadsListWithoutError()
    {
        var corretoraApi = new Mock<ICorretoraApi>();
        corretoraApi.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var model = new IndexModel(corretoraApi.Object);

        var result = await model.OnPostDeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Null(model.ErrorMessage);
        corretoraApi.Verify(a => a.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenApiThrows_ShowsErrorMessageAndReloadsList()
    {
        var corretoraApi = new Mock<ICorretoraApi>();
        corretoraApi
            .Setup(a => a.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, "erro"));
        corretoraApi.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var model = new IndexModel(corretoraApi.Object);

        var result = await model.OnPostDeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.ErrorMessage);
    }
}
