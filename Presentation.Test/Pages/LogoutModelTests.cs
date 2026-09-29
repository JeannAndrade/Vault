using Microsoft.AspNetCore.Mvc;
using Moq;
using LumiaFoundation.AspNetCore.ClientAuthentication;
using Presentation.Pages;
using Presentation.Test.TestDoubles;

namespace Presentation.Test.Pages;

public class LogoutModelTests
{
    [Fact]
    public async Task OnPostAsync_SignsOutAndRedirectsToLogin()
    {
        // Arrange
        var signInService = new Mock<IApiSignInService>();
        var model = new LogoutModel(signInService.Object)
        {
            PageContext = PageModelTestHelper.CreatePageContext()
        };

        // Act
        var result = await model.OnPostAsync(CancellationToken.None);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Login", redirect.PageName);
        signInService.Verify(s => s.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void OnGet_RedirectsToHome()
    {
        // Arrange
        var model = new LogoutModel(new Mock<IApiSignInService>().Object);

        // Act
        var result = model.OnGet();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }
}
