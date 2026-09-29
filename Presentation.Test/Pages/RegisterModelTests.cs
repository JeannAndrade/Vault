using LumiaFoundation.AspNetCore.ClientAuthentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Pages;
using Presentation.Test.TestDoubles;

namespace Presentation.Test.Pages;

public class RegisterModelTests
{
    private readonly Mock<IApiRegistrationService> _registrationService = new();

    private RegisterModel CreateModel(RegisterModel.InputModel? input = null)
    {
        var actionContext = PageModelTestHelper.CreateActionContext();

        return new RegisterModel(_registrationService.Object)
        {
            PageContext = PageModelTestHelper.CreatePageContext(actionContext),
            Url = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(actionContext),
            Input = input ?? new RegisterModel.InputModel
            {
                FirstName = "Ana",
                LastName = "Silva",
                UserName = "ana",
                Email = "ana@exemplo.com",
                Password = "Senha@12345",
                ConfirmPassword = "Senha@12345"
            }
        };
    }

    [Fact]
    public async Task OnPostAsync_WhenModelStateIsInvalid_ReturnsPageWithoutCallingService()
    {
        var model = CreateModel();
        model.ModelState.AddModelError("Input.Email", "Informe um e-mail válido.");

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        _registrationService.Verify(
            s => s.RegisterAsync(It.IsAny<LumiaFoundation.Http.Client.Authentication.UserRegistration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_WhenSucceeded_RedirectsToLoginWithRegisteredFlag()
    {
        _registrationService
            .Setup(s => s.RegisterAsync(It.IsAny<LumiaFoundation.Http.Client.Authentication.UserRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RegistrationResult.Succeeded());
        var model = CreateModel();

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Login", redirect.PageName);
        Assert.Equal(true, redirect.RouteValues!["registered"]);
    }

    [Fact]
    public async Task OnPostAsync_WhenRejected_ShowsApiMessage()
    {
        _registrationService
            .Setup(s => s.RegisterAsync(It.IsAny<LumiaFoundation.Http.Client.Authentication.UserRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RegistrationResult.Rejected("Username 'ana' is already taken."));
        var model = CreateModel();

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Username 'ana' is already taken.", model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_WhenUnavailable_ShowsGenericMessage()
    {
        _registrationService
            .Setup(s => s.RegisterAsync(It.IsAny<LumiaFoundation.Http.Client.Authentication.UserRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RegistrationResult.Unavailable());
        var model = CreateModel();

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains("Tente novamente", model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_TrimsNameUserNameAndEmail()
    {
        LumiaFoundation.Http.Client.Authentication.UserRegistration? captured = null;
        _registrationService
            .Setup(s => s.RegisterAsync(It.IsAny<LumiaFoundation.Http.Client.Authentication.UserRegistration>(), It.IsAny<CancellationToken>()))
            .Callback<LumiaFoundation.Http.Client.Authentication.UserRegistration, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(RegistrationResult.Succeeded());
        var model = CreateModel(new RegisterModel.InputModel
        {
            FirstName = "  Ana  ",
            LastName = "  Silva  ",
            UserName = "  ana  ",
            Email = "  ana@exemplo.com  ",
            Password = "Senha@12345",
            ConfirmPassword = "Senha@12345"
        });

        await model.OnPostAsync(CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("Ana", captured!.FirstName);
        Assert.Equal("Silva", captured.LastName);
        Assert.Equal("ana", captured.UserName);
        Assert.Equal("ana@exemplo.com", captured.Email);
    }
}
