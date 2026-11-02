using LumiaFoundation.AspNetCore.ClientAuthentication;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Http.Client.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Presentation.Corretoras;
using Presentation.Cotacoes;
using Presentation.Dashboards;
using Presentation.Emissores;
using Presentation.Movimentos;
using Presentation.Objetivos;
using Presentation.Produtos;
using Presentation.TiposRenda;

namespace Presentation.Extensions;

public static class PresentationExtensions
{
    public static void AddRazorPagesService(this IServiceCollection services)
    {
        services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizeFolder("/");
            options.Conventions.AllowAnonymousToPage("/Login");
            options.Conventions.AllowAnonymousToPage("/Register");
            options.Conventions.AllowAnonymousToPage("/Error");
        });
    }

    public static void AddSessionTokenStore(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(8);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        services.Configure<SessionTokenStoreOptions>(options => options.SessionKey = "Vault.ApiToken");
    }

    public static void AddApiSessionCookie(this IServiceCollection services)
    {
        services.AddScoped<ApiSessionCookieEvents>();
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Login";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = false;
                options.EventsType = typeof(ApiSessionCookieEvents);
                options.Cookie.Name = "Vault.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });
        services.AddAuthorization();
    }

    public static void AddApiResourceClientService(this IServiceCollection services)
    {
        services.AddApiResourceClient<ICorretoraApi, CorretoraApi>();
        services.AddApiResourceClient<IEmissorApi, EmissorApi>();
        services.AddApiResourceClient<IProdutoApi, ProdutoApi>();
        services.AddApiResourceClient<ITipoRendaApi, TipoRendaApi>();
        services.AddApiResourceClient<IObjetivoApi, ObjetivoApi>();
        services.AddApiResourceClient<IMovimentoApi, MovimentoApi>();
        services.AddApiResourceClient<IDistribuicaoValorLiquidoApi, DistribuicaoValorLiquidoApi>();
        services.AddApiResourceClient<ICotacaoApi, CotacaoApi>();
        services.AddApiResourceClient<IAtualizacaoPorCotacaoApi, AtualizacaoPorCotacaoApi>();
        services.AddApiResourceClient<IRentabilidadeApi, RentabilidadeApi>();
        services.AddApiResourceClient<IFluxoMensalApi, FluxoMensalApi>();
        services.AddApiResourceClient<IConsolidadoApi, ConsolidadoApi>();
    }

    public static void AddDataProviders(this IServiceCollection services)
    {
        services.AddScoped<IDateTimeProvider, UtcDateTimeProvider>();
    }

    public static void AddSupportScreenElements(this IServiceCollection services)
    {
        services.AddScoped<IMovimentoFormLookupData, MovimentoFormLookupDataProvider>();
    }
}
