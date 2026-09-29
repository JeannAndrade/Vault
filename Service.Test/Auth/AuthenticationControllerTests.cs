using LumiaFoundation.Auth.DTO;
using LumiaFoundation.Auth.Persistence;
using LumiaFoundation.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using LumiaFoundation.AspNetCore.Commons.Exceptions;

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

    [Fact]
    public async Task RegisterUser_WhenCreationFails_ThrowsHttpBaseExceptionWithJoinedMessages()
    {
        // Arrange
        var errors = new[]
        {
        new IdentityError { Code = "DuplicateUserName", Description = "Username 'ana' is already taken." },
        new IdentityError { Code = "PasswordTooShort", Description = "Passwords must be at least 10 characters." }
    };
        _authenticationService
            .Setup(s => s.RegisterUser(It.IsAny<UserForRegistrationDto>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var exception = await Assert.ThrowsAsync<HttpBaseException>(
            () => _controller.RegisterUser(CreateValidRegistrationDto()));

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, exception.StatusCode);
        Assert.Contains("already taken", exception.Message);
        Assert.Contains("at least 10 characters", exception.Message);
    }

    [Fact]
    public async Task RegisterUser_WhenSucceeds_ReturnsCreated()
    {
        // Arrange
        _authenticationService
            .Setup(s => s.RegisterUser(It.IsAny<UserForRegistrationDto>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.RegisterUser(CreateValidRegistrationDto());

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status201Created, statusResult.StatusCode);
    }

    private static UserForRegistrationDto CreateValidRegistrationDto() => new()
    {
        FirstName = "Ana",
        LastName = "Silva",
        UserName = "ana",
        Password = "Senha@12345",
        Email = "ana@exemplo.com"
    };
}
