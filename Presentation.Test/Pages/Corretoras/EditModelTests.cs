using System.Net;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Corretoras;
using Presentation.Pages.Corretoras;
using Presentation.Test.TestDoubles;

namespace Presentation.Test.Pages.Corretoras;

public class EditModelTests
{
    private readonly Mock<ICorretoraApi> _corretoraApi = new();
    private readonly Guid _id = Guid.NewGuid();

    private EditModel BuildModel() =>
        new(_corretoraApi.Object)
        {
            PageContext = PageModelTestHelper.CreatePageContext(),
            Id = _id
        };

    [Fact]
    public async Task OnGetAsync_LoadsNomeFromApi()
    {
        _corretoraApi.Setup(a => a.GetByIdAsync(_id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CorretoraDto(_id, "XP"));
        var model = BuildModel();

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("XP", model.Input.Nome);
    }

    [Fact]
    public async Task OnPostAsync_WhenModelStateIsInvalid_ReturnsPageWithoutCallingApi()
    {
        var model = BuildModel();
        model.Input = new EditModel.InputModel { Nome = "XP" };
        model.ModelState.AddModelError("Input.Nome", "Informe o nome.");

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        _corretoraApi.Verify(a => a.UpdateAsync(It.IsAny<Guid>(), It.IsAny<CorretoraForUpdateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_WhenApiSucceeds_RedirectsToIndex()
    {
        var model = BuildModel();
        model.Input = new EditModel.InputModel { Nome = "XP Investimentos" };

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);
        _corretoraApi.Verify(a => a.UpdateAsync(
            _id, It.Is<CorretoraForUpdateDto>(d => d.Nome == "XP Investimentos"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_WhenApiThrows_ShowsErrorMessage()
    {
        _corretoraApi
            .Setup(a => a.UpdateAsync(It.IsAny<Guid>(), It.IsAny<CorretoraForUpdateDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, "erro"));
        var model = BuildModel();
        model.Input = new EditModel.InputModel { Nome = "XP" };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.ErrorMessage);
    }
}
