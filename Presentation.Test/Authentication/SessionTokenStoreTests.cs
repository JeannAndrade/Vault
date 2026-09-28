using LumiaFoundation.Http.Client.Authentication;
using Microsoft.AspNetCore.Http;
using Presentation.Authentication;
using Presentation.Test.TestDoubles;

namespace Presentation.Test.Authentication;

public class SessionTokenStoreTests
{
    private static HttpContextAccessor CreateAccessor(FakeSession? session = null) =>
        new() { HttpContext = new DefaultHttpContext { Session = session ?? new FakeSession() } };

    [Fact]
    public async Task GetAsync_WhenSessionHasNoToken_ReturnsNull()
    {
        // Arrange
        var store = new SessionTokenStore(CreateAccessor());

        // Act
        var result = await store.GetAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsSameToken()
    {
        // Arrange
        var store = new SessionTokenStore(CreateAccessor());
        var token = new AuthenticationToken("access", "refresh");

        // Act
        await store.SetAsync(token);
        var result = await store.GetAsync();

        // Assert
        Assert.Equal(token, result);
    }

    [Fact]
    public async Task SetAsync_CommitsTheSession()
    {
        // Arrange
        var session = new FakeSession();
        var store = new SessionTokenStore(CreateAccessor(session));

        // Act
        await store.SetAsync(new AuthenticationToken("access", "refresh"));

        // Assert
        Assert.Equal(1, session.CommitCount);
    }

    [Fact]
    public async Task ClearAsync_RemovesTheToken()
    {
        // Arrange
        var store = new SessionTokenStore(CreateAccessor());
        await store.SetAsync(new AuthenticationToken("access", "refresh"));

        // Act
        await store.ClearAsync();

        // Assert
        Assert.Null(await store.GetAsync());
    }

    [Fact]
    public async Task SameStoreInstance_WhenRequestChanges_IsolatesTokensBetweenUsers()
    {
        // Arrange: uma única instância do store, como o HttpClientFactory reutiliza o handler
        var accessor = CreateAccessor();
        var store = new SessionTokenStore(accessor);
        var tokenA = new AuthenticationToken("access-A", "refresh-A");
        var tokenB = new AuthenticationToken("access-B", "refresh-B");

        // Act: usuário A grava; a requisição passa a ser do usuário B, com outra sessão
        var contextA = accessor.HttpContext!;
        await store.SetAsync(tokenA);

        accessor.HttpContext = new DefaultHttpContext { Session = new FakeSession() };
        var visibleToB = await store.GetAsync();
        await store.SetAsync(tokenB);

        accessor.HttpContext = contextA;
        var visibleToA = await store.GetAsync();

        // Assert
        Assert.Null(visibleToB);
        Assert.Equal(tokenA, visibleToA);
    }

    [Fact]
    public async Task GetAsync_WhenThereIsNoHttpContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var store = new SessionTokenStore(new HttpContextAccessor());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await store.GetAsync());
    }
}
