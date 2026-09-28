using System.Net;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Presentation.Authentication;

namespace Presentation.Test.Authentication;

public class ApiUnauthorizedExceptionHandlerTests
{
    private readonly Mock<ISignInService> _signInService = new();
    private readonly DefaultHttpContext _httpContext;
    private readonly ApiUnauthorizedExceptionHandler _handler =
        new(NullLogger<ApiUnauthorizedExceptionHandler>.Instance);

    public ApiUnauthorizedExceptionHandlerTests()
    {
        _httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton(_signInService.Object)
                .BuildServiceProvider()
        };
    }

    [Fact]
    public async Task TryHandleAsync_WhenExceptionIsApiUnauthorized_SignsOutAndRedirectsToLogin()
    {
        // Act
        var handled = await _handler.TryHandleAsync(
            _httpContext,
            new ApiException(HttpStatusCode.Unauthorized, "expirou"),
            CancellationToken.None);

        // Assert
        Assert.True(handled);
        _signInService.Verify(
            s => s.SignOutAsync(_httpContext, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(StatusCodes.Status302Found, _httpContext.Response.StatusCode);
        Assert.Equal("/Login?expired=true", _httpContext.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task TryHandleAsync_WhenApiExceptionIsNotUnauthorized_ReturnsFalse()
    {
        // Act
        var handled = await _handler.TryHandleAsync(
            _httpContext,
            new ApiException(HttpStatusCode.InternalServerError, "erro"),
            CancellationToken.None);

        // Assert
        Assert.False(handled);
        _signInService.Verify(
            s => s.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TryHandleAsync_WhenExceptionIsNotApiException_ReturnsFalse()
    {
        // Act
        var handled = await _handler.TryHandleAsync(
            _httpContext,
            new InvalidOperationException("boom"),
            CancellationToken.None);

        // Assert
        Assert.False(handled);
    }
}
