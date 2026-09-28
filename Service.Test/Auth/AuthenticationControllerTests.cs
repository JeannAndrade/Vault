using LumiaFoundation.Auth.DTO;
using LumiaFoundation.Auth.Persistence;
using LumiaFoundation.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Auth;

namespace Service.Test.Auth;

public class AuthenticationControllerTests
{
    private readonly Mock<IAuthenticationService> _authenticationService = new();
    private readonly AuthenticationController _controller;

    public AuthenticationControllerTests()
    {
        var serviceManager = new Mock<IServiceManager>();
        serviceManager
            .SetupGet(s => s.AuthenticationService)
            .Returns(_authenticationService.Object);

        _controller = new AuthenticationController(serviceManager.Object);
    }

    [Fact]
    public async Task Authenticate_WhenCredentialsAreValid_ReturnsTokenDtoDirectly()
    {
        // Arrange
        var token = new TokenDto("access-token", "refresh-token");
        _authenticationService
            .Setup(s => s.ValidateUser(It.IsAny<UserForAuthenticationDto>()))
            .ReturnsAsync(true);
        _authenticationService
            .Setup(s => s.CreateToken(true))
            .ReturnsAsync(token);

        // Act
        var result = await _controller.Authenticate(
            new UserForAuthenticationDto { UserName = "ana", Password = "senha" });

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(token, ok.Value);
    }

    [Fact]
    public async Task Authenticate_WhenCredentialsAreInvalid_ReturnsUnauthorizedWithoutCreatingToken()
    {
        // Arrange
        _authenticationService
            .Setup(s => s.ValidateUser(It.IsAny<UserForAuthenticationDto>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Authenticate(
            new UserForAuthenticationDto { UserName = "ana", Password = "errada" });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        _authenticationService.Verify(s => s.CreateToken(It.IsAny<bool>()), Times.Never);
    }
}
