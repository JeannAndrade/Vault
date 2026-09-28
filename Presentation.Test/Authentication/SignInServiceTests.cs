using System.Net;
using System.Security.Claims;
using LumiaFoundation.Http.Client.Authentication;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Presentation.Authentication;
using AuthenticationToken = LumiaFoundation.Http.Client.Authentication.AuthenticationToken;

namespace Presentation.Test.Authentication;

public class SignInServiceTests
{
    private readonly Mock<IAuthenticationApi> _authenticationApi = new();
    private readonly Mock<ITokenStore> _tokenStore = new();
    private readonly Mock<IAuthenticationService> _cookieAuthentication = new();
    private readonly DefaultHttpContext _httpContext;
    private readonly SignInService _service;

    public SignInServiceTests()
    {
        _service = new SignInService(
            _authenticationApi.Object,
            _tokenStore.Object,
            NullLogger<SignInService>.Instance);

        _httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton(_cookieAuthentication.Object)
                .BuildServiceProvider()
        };
    }

    private void SetupLoginToReturn(AuthenticationToken token) =>
        _authenticationApi
            .Setup(a => a.LoginAsync(It.IsAny<UserCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

    private void SetupLoginToThrow(Exception exception) =>
        _authenticationApi
            .Setup(a => a.LoginAsync(It.IsAny<UserCredentials>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

    private void VerifyNothingWasStoredNorSignedIn()
    {
        _tokenStore.Verify(
            t => t.SetAsync(It.IsAny<AuthenticationToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _cookieAuthentication.Verify(
            a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string?>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties?>()),
            Times.Never);
    }

    [Fact]
    public async Task SignInAsync_WhenCredentialsAreValid_StoresTokenAndSignsInWithUserName()
    {
        // Arrange
        var token = new AuthenticationToken("access", "refresh");
        SetupLoginToReturn(token);

        // Act
        var outcome = await _service.SignInAsync(_httpContext, "ana", "senha");

        // Assert
        Assert.Equal(SignInOutcome.Succeeded, outcome);
        _authenticationApi.Verify(a => a.LoginAsync(
            It.Is<UserCredentials>(c => c.UserName == "ana" && c.Password == "senha"),
            It.IsAny<CancellationToken>()), Times.Once);
        _tokenStore.Verify(t => t.SetAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        _cookieAuthentication.Verify(a => a.SignInAsync(
            _httpContext,
            CookieAuthenticationDefaults.AuthenticationScheme,
            It.Is<ClaimsPrincipal>(p => p.Identity!.Name == "ana"),
            It.IsAny<AuthenticationProperties?>()), Times.Once);
    }

    [Fact]
    public async Task SignInAsync_WhenApiReturnsUnauthorized_ReturnsInvalidCredentials()
    {
        // Arrange
        SetupLoginToThrow(new ApiException(HttpStatusCode.Unauthorized, "não autorizado"));

        // Act
        var outcome = await _service.SignInAsync(_httpContext, "ana", "errada");

        // Assert
        Assert.Equal(SignInOutcome.InvalidCredentials, outcome);
        VerifyNothingWasStoredNorSignedIn();
    }

    [Fact]
    public async Task SignInAsync_WhenApiFails_ReturnsUnavailable()
    {
        // Arrange
        SetupLoginToThrow(new ApiException(HttpStatusCode.InternalServerError, "erro"));

        // Act
        var outcome = await _service.SignInAsync(_httpContext, "ana", "senha");

        // Assert
        Assert.Equal(SignInOutcome.Unavailable, outcome);
        VerifyNothingWasStoredNorSignedIn();
    }

    [Fact]
    public async Task SignInAsync_WhenApiIsUnreachable_ReturnsUnavailable()
    {
        // Arrange
        SetupLoginToThrow(new HttpRequestException("sem conexão"));

        // Act
        var outcome = await _service.SignInAsync(_httpContext, "ana", "senha");

        // Assert
        Assert.Equal(SignInOutcome.Unavailable, outcome);
        VerifyNothingWasStoredNorSignedIn();
    }

    [Fact]
    public async Task SignInAsync_WhenHttpClientTimesOut_ReturnsUnavailable()
    {
        // Arrange
        SetupLoginToThrow(new TaskCanceledException("timeout", new TimeoutException()));

        // Act
        var outcome = await _service.SignInAsync(_httpContext, "ana", "senha");

        // Assert
        Assert.Equal(SignInOutcome.Unavailable, outcome);
        VerifyNothingWasStoredNorSignedIn();
    }

    [Fact]
    public async Task SignInAsync_WhenRequestIsCancelledByCaller_PropagatesTheCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        SetupLoginToThrow(new TaskCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _service.SignInAsync(_httpContext, "ana", "senha", cts.Token));
    }

    [Fact]
    public async Task SignOutAsync_SignsOutTheCookieAndClearsTheToken()
    {
        // Act
        await _service.SignOutAsync(_httpContext);

        // Assert
        _cookieAuthentication.Verify(a => a.SignOutAsync(
            _httpContext,
            CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties?>()), Times.Once);
        _tokenStore.Verify(t => t.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
