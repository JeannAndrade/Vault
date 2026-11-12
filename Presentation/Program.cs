using LumiaFoundation.AspNetCore.Web.ClientAuthentication;
using LumiaFoundation.AspNetCore.Web.Commons.Extensions;
using LumiaFoundation.Http.Client.Extensions;
using LumiaFoundation.Logger.Extensions;
using LumiaFoundation.Logger.LoggerService;
using Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);

var vaultApiAddress = builder.Configuration["VaultApi:BaseAddress"]
    ?? throw new InvalidOperationException("Configuração 'VaultApi:BaseAddress' não encontrada.");

#region Log (NLog via Lumia.Foundation.Logger)
LoggerManager.LoadConfigurationFromFile(
    Path.Combine(builder.Environment.ContentRootPath, "nlog.config"));
builder.Services.ConfigureLoggerService();
#endregion

builder.Services.AddRazorPagesService();
builder.Services.AddSessionTokenStore();
builder.Services.AddApiSessionCookie();

#region Cliente HTTP da Vault.Api

builder.Services.AddLumiaApiClient(
    vaultApiAddress,
    configureAuthentication: options => options.RefreshPath = "/api/token/refresh");
builder.Services.AddApiSignInService();
builder.Services.AddApiRegistrationService();
builder.Services.AddSupportScreenElements();
builder.Services.AddApiResourceClientService();
builder.Services.AddDataProviders();
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
