using System.Net;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Corretoras;
using Presentation.Pages.Corretoras;
using Presentation.Test.TestDoubles;

namespace Presentation.Test.Pages.Corretoras;

public class CreateModelTests
{
    private readonly Mock<ICorretoraApi> _corretoraApi = new();

    private CreateModel BuildModel(string nome = "XP") =>
        new(_corretoraApi.Object)
        {
            PageContext = PageModelTestHelper.CreatePageContext(),
            Input = new CreateModel.InputModel { Nome = nome }
        };

    [Fact]
    public async Task OnPostAsync_WhenModelStateIsInvalid_ReturnsPageWithoutCallingApi()
    {
        var model = BuildModel();
        model.ModelState.AddModelError("Input.Nome", "Informe o nome.");

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        _corretoraApi.Verify(a => a.CreateAsync(It.IsAny<CorretoraForCreationDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_WhenApiSucceeds_RedirectsToIndex()
    {
        var model = BuildModel();

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_TrimsNome()
    {
        CorretoraForCreationDto? captured = null;
        _corretoraApi
            .Setup(a => a.CreateAsync(It.IsAny<CorretoraForCreationDto>(), It.IsAny<CancellationToken>()))
            .Callback<CorretoraForCreationDto, CancellationToken>((dto, _) => captured = dto)
            .ReturnsAsync(new CorretoraDto(Guid.NewGuid(), "XP"));
        var model = BuildModel(nome: "  XP  ");

        await model.OnPostAsync(CancellationToken.None);

        Assert.Equal("XP", captured!.Nome);
    }

    [Fact]
    public async Task OnPostAsync_WhenApiThrows_ShowsErrorMessage()
    {
        _corretoraApi
            .Setup(a => a.CreateAsync(It.IsAny<CorretoraForCreationDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, "erro"));
        var model = BuildModel();

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.ErrorMessage);
    }
}
