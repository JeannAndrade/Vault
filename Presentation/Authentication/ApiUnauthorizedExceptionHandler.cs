using System.Net;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Presentation.Authentication;

/// <summary>
/// Quando a API responde 401 e a sessão não pôde ser renovada, encerra a sessão do site
/// e leva o usuário de volta ao login.
/// </summary>
public sealed class ApiUnauthorizedExceptionHandler(ILogger<ApiUnauthorizedExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ApiException { StatusCode: HttpStatusCode.Unauthorized })
            return false;

        logger.LogInformation("A API respondeu 401; encerrando a sessão do usuário e redirecionando para o login.");

        // O handler é singleton; o ISignInService é scoped, então é obtido do escopo da requisição.
        var signInService = httpContext.RequestServices.GetRequiredService<ISignInService>();
        await signInService.SignOutAsync(httpContext, cancellationToken);

        httpContext.Response.Redirect("/Login?expired=true");

        return true;
    }
}
