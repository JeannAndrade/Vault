using LumiaFoundation.AspNetCore.ClientAuthentication;
using LumiaFoundation.Http.Client.Authentication;
using LumiaFoundation.Http.Client.Extensions;
using LumiaFoundation.Logger.Extensions;
using LumiaFoundation.Logger.LoggerService;
using Microsoft.AspNetCore.Authentication.Cookies;
using Presentation.Corretoras;

var builder = WebApplication.CreateBuilder(args);

var vaultApiAddress = builder.Configuration["VaultApi:BaseAddress"]
    ?? throw new InvalidOperationException("Configuração 'VaultApi:BaseAddress' não encontrada.");

#region Log (NLog via Lumia.Foundation.Logger)
LoggerManager.LoadConfigurationFromFile(
    Path.Combine(builder.Environment.ContentRootPath, "nlog.config"));
builder.Services.ConfigureLoggerService();
#endregion

#region Razor Pages (tudo exige login, exceto /Login e /Error)
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Register");
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
builder.Services.Configure<SessionTokenStoreOptions>(o => o.SessionKey = "Vault.ApiToken");
#endregion

#region Autenticação do site (cookie) + validação contra a sessão da API
builder.Services.AddScoped<ApiSessionCookieEvents>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // alinhado ao refresh token da API
        options.SlidingExpiration = false;
        options.EventsType = typeof(ApiSessionCookieEvents);
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
builder.Services.AddScoped<IApiSignInService, ApiSignInService>();
builder.Services.AddScoped<IApiRegistrationService, ApiRegistrationService>();
builder.Services.AddApiResourceClient<ICorretoraApi, CorretoraApi>();
#endregion

#region Tratamento de exceções
builder.Services.AddExceptionHandler<ApiUnauthorizedExceptionHandler>();
#endregion

var app = builder.Build();

app.UseStaticFiles();

// UseSession antes de UseExceptionHandler: o handler de 401 precisa limpar a sessão.
app.UseSession();
app.UseExceptionHandler("/Error");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
