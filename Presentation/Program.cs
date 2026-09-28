using LumiaFoundation.Http.Client.Authentication;
using LumiaFoundation.Http.Client.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Presentation.Authentication;

var builder = WebApplication.CreateBuilder(args);

var vaultApiAddress = builder.Configuration["VaultApi:BaseAddress"]
    ?? throw new InvalidOperationException("Configuração 'VaultApi:BaseAddress' não encontrada.");

#region Razor Pages (tudo exige login, exceto /Login e /Error)
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Error");
});
#endregion

#region Sessão (guarda o par de tokens da API por usuário)
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
#endregion

#region Autenticação do site (cookie)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // alinhado ao refresh token da API
        options.SlidingExpiration = false;
        options.Cookie.Name = "Vault.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();
#endregion

#region Cliente HTTP da Vault.Api
builder.Services.AddSingleton<ITokenStore, SessionTokenStore>();
builder.Services.AddLumiaApiClient(
    vaultApiAddress,
    configureAuthentication: options => options.RefreshPath = "/api/token/refresh");
builder.Services.AddScoped<ISignInService, SignInService>();
#endregion

#region Tratamento de exceções
builder.Services.AddExceptionHandler<ApiUnauthorizedExceptionHandler>();
#endregion

var app = builder.Build();

app.UseStaticFiles();

// A ordem importa: o UseExceptionHandler fica DEPOIS do UseSession. O middleware de sessão
// descarta a sessão da requisição quando uma exceção passa por ele; se o handler de exceções
// ficasse antes, ele não conseguiria mais limpar o token da sessão.
app.UseSession();
app.UseExceptionHandler("/Error");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
