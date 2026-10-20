using Application.Corretoras.Queries.GetCorretora;
using Application.Dashboards.Queries.GetRentabilidadesPorTipoRenda;
using Application.Movimentos.Commands.CreateMovimento;
using Application.Movimentos.Commands.DistribuirValorLiquido;
using Application.Movimentos.Commands.UpdateValorLiquidoMovimento;
using Application.Movimentos.Queries.GetMovimentosDesatualizados;
using Application.Movimentos.Queries.SimularDistribuicaoValorLiquido;
using LumiaFoundation.AspNetCore.Commons.Extensions;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Logger.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Persistence.Managment;

namespace Service.Test.Registro;

public class RegistroDeServicosTests
{
    private static readonly System.Reflection.Assembly AssemblyApplication = typeof(IGetCorretoraQuery).Assembly;

    // Reproduz o registro do Program.cs, trocando apenas as dependências de infraestrutura por mocks.
    private static ServiceCollection CriarServicos()
    {
        var services = new ServiceCollection();
        services.AddServicesFromAssembly(AssemblyApplication);
        services.AddSingleton(Mock.Of<IRepositoryManager>());
        services.AddSingleton(Mock.Of<ILoggerManager>());
        services.AddSingleton(Mock.Of<IDateTimeProvider>());
        return services;
    }

    private static ServiceProvider Construir(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    [Fact]
    public void Provider_ConstroiComValidateOnBuild()
    {
        using var provider = Construir(CriarServicos());

        Assert.NotNull(provider);
    }

    [Fact]
    public void NaoRegistraTiposDoFrameworkNemGeradosPeloCompilador()
    {
        var services = CriarServicos();

        var daConvencao = services.Where(d => d.ImplementationType?.Assembly == AssemblyApplication).ToList();

        Assert.NotEmpty(daConvencao);
        Assert.All(daConvencao, d =>
        {
            Assert.Equal(AssemblyApplication, d.ServiceType.Assembly);
            Assert.False(d.ImplementationType!.IsNested);
            Assert.DoesNotContain("<", d.ImplementationType.Name);
        });
    }

    [Fact]
    public void ResolveTodosOsServicosDaApplication()
    {
        var services = CriarServicos();
        using var provider = Construir(services);
        using var scope = provider.CreateScope();

        var tipos = services
            .Where(d => d.ServiceType.Assembly == AssemblyApplication)
            .Select(d => d.ServiceType)
            .Distinct()
            .ToList();

        Assert.NotEmpty(tipos);
        Assert.All(tipos, tipo => Assert.NotNull(scope.ServiceProvider.GetRequiredService(tipo)));
    }

    [Theory]
    [InlineData(typeof(ISimularDistribuicaoValorLiquidoQuery))]
    [InlineData(typeof(IDistribuirValorLiquidoCommand))]
    [InlineData(typeof(IGetMovimentosDesatualizadosQuery))]
    [InlineData(typeof(IUpdateValorLiquidoMovimentoCommand))]
    [InlineData(typeof(ICreateMovimentoCommand))]
    [InlineData(typeof(IGetRentabilidadesPorTipoRendaQuery))]
    public void ResolveServicosEsperados(Type servico)
    {
        using var provider = Construir(CriarServicos());
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService(servico));
    }
}
