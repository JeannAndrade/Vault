using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using LumiaFoundation.AspNetCore.ClientAuthentication;
using Presentation.Pages;
using Presentation.Test.TestDoubles;

namespace Presentation.Test.Pages;

public class LoginModelTests
{
    private readonly Mock<IApiSignInService> _signInService = new();

    private LoginModel CreateModel(string userName = "ana", string password = "senha", string? returnUrl = null)
    {
        var actionContext = PageModelTestHelper.CreateActionContext();

        return new LoginModel(_signInService.Object)
        {
            PageContext = PageModelTestHelper.CreatePageContext(actionContext),
            Url = new UrlHelper(actionContext),
            ReturnUrl = returnUrl,
            Input = new LoginModel.InputModel { UserName = userName, Password = password }
        };
    }

    private void SetupOutcome(string userName, SignInOutcome outcome) =>
        _signInService
            .Setup(s => s.SignInAsync(
                It.IsAny<HttpContext>(), userName, "senha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(outcome);

    [Fact]
    public async Task OnPostAsync_WhenModelStateIsInvalid_ReturnsPageWithoutCallingService()
    {
        // Arrange
        var model = CreateModel();
        model.ModelState.AddModelError("Input.UserName", "Informe o usuário.");

        // Act
        var result = await model.OnPostAsync(CancellationToken.None);

        // Assert
        Assert.IsType<PageResult>(result);
        _signInService.Verify(s => s.SignInAsync(
            It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_WhenSucceededWithLocalReturnUrl_RedirectsToIt()
    {
        // Arrange
        SetupOutcome("ana", SignInOutcome.Succeeded);
        var model = CreateModel(returnUrl: "/Corretoras");

        // Act
        var result = await model.OnPostAsync(CancellationToken.None);

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/Corretoras", redirect.Url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://site-malicioso.example")]
    [InlineData("//site-malicioso.example")]
    public async Task OnPostAsync_WhenSucceededWithoutSafeReturnUrl_RedirectsToHome(string? returnUrl)
    {
        // Arrange
        SetupOutcome("ana", SignInOutcome.Succeeded);
        var model = CreateModel(returnUrl: returnUrl);

        // Act
        var result = await model.OnPostAsync(CancellationToken.None);

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_WhenUserNameHasSpaces_SendsTrimmedUserName()
    {
        // Arrange
        SetupOutcome("ana", SignInOutcome.Succeeded);
        var model = CreateModel(userName: "  ana  ");

        // Act
        await model.OnPostAsync(CancellationToken.None);

        // Assert
        _signInService.Verify(s => s.SignInAsync(
            It.IsAny<HttpContext>(), "ana", "senha", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_WhenCredentialsAreInvalid_ShowsGenericMessage()
    {
        // Arrange
        SetupOutcome("ana", SignInOutcome.InvalidCredentials);
        var model = CreateModel();

        // Act
        var result = await model.OnPostAsync(CancellationToken.None);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Usuário ou senha inválidos.", model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_WhenApiIsUnavailable_ShowsUnavailableMessage()
    {
        // Arrange
        SetupOutcome("ana", SignInOutcome.Unavailable);
        var model = CreateModel();

        // Act
        var result = await model.OnPostAsync(CancellationToken.None);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Contains("Tente novamente", model.ErrorMessage);
    }
}
