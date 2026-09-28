using System.Text.Json;
using LumiaFoundation.Http.Client.Authentication;

namespace Presentation.Authentication;

/// <summary>
/// Guarda o par de tokens da API na sessão do usuário atual (<see cref="ISession"/>).
/// </summary>
/// <remarks>
/// Esta classe não guarda estado em campos: a sessão é buscada a cada chamada, a partir da
/// requisição em andamento. Isso é o que isola um usuário do outro, já que o
/// <see cref="BearerTokenHandler"/> é reutilizado entre requisições pelo HttpClientFactory.
/// </remarks>
public sealed class SessionTokenStore(IHttpContextAccessor httpContextAccessor) : ITokenStore
{
    private const string SessionKey = "Vault.ApiToken";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<AuthenticationToken?> GetAsync(CancellationToken cancellationToken = default)
    {
        var session = GetSession();
        await session.LoadAsync(cancellationToken);

        return session.TryGetValue(SessionKey, out var bytes)
            ? JsonSerializer.Deserialize<AuthenticationToken>(bytes, JsonOptions)
            : null;
    }

    public async ValueTask SetAsync(AuthenticationToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        var session = GetSession();
        await session.LoadAsync(cancellationToken);
        session.Set(SessionKey, JsonSerializer.SerializeToUtf8Bytes(token, JsonOptions));
        await session.CommitAsync(cancellationToken);
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        var session = GetSession();
        await session.LoadAsync(cancellationToken);
        session.Remove(SessionKey);
        await session.CommitAsync(cancellationToken);
    }

    private ISession GetSession() =>
        httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException(
            "Não há requisição HTTP com sessão disponível para acessar o token da API.");
}
