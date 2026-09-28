using System.Net;
using System.Security.Claims;
using LumiaFoundation.Http.Client.Authentication;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using AuthenticationToken = LumiaFoundation.Http.Client.Authentication.AuthenticationToken;

namespace Presentation.Authentication;

public sealed class SignInService(
    IAuthenticationApi authenticationApi,
    ITokenStore tokenStore,
    ILogger<SignInService> logger) : ISignInService
{
    public async Task<SignInOutcome> SignInAsync(
        HttpContext httpContext,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        AuthenticationToken token;

        try
        {
            token = await authenticationApi.LoginAsync(new UserCredentials(userName, password), cancellationToken);
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogWarning("Credenciais recusadas pela API para o usuário {UserName}.", userName);
            return SignInOutcome.InvalidCredentials;
        }
        catch (ApiException exception)
        {
            logger.LogError(exception, "A API respondeu {StatusCode} ao autenticar o usuário.", (int)exception.StatusCode);
            return SignInOutcome.Unavailable;
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Não foi possível comunicar com a API de autenticação.");
            return SignInOutcome.Unavailable;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout do HttpClient (o usuário não cancelou a requisição).
            logger.LogError(exception, "A API de autenticação não respondeu a tempo.");
            return SignInOutcome.Unavailable;
        }

        await tokenStore.SetAsync(token, cancellationToken);

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, userName) },
            CookieAuthenticationDefaults.AuthenticationScheme);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return SignInOutcome.Succeeded;
    }

    public async Task SignOutAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        // O cookie sai primeiro: o usuário precisa ser deslogado do site mesmo que a limpeza da sessão falhe.
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await tokenStore.ClearAsync(cancellationToken);
    }
}
