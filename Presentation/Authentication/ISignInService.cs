namespace Presentation.Authentication;

public interface ISignInService
{
    /// <summary>
    /// Autentica na Vault.Api, guarda o par de tokens na sessão e emite o cookie de autenticação do site.
    /// </summary>
    Task<SignInOutcome> SignInAsync(
        HttpContext httpContext,
        string userName,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Encerra o cookie de autenticação do site e remove o par de tokens da sessão.
    /// </summary>
    Task SignOutAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
