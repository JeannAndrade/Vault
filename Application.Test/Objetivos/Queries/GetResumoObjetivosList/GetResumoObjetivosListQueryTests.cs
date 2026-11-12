using Application.Objetivos.Queries.GetResumoObjetivosList;
using Domain.Movimentos;
using Domain.Objetivos;
using LumiaFoundation.Core.TimeService;
using Moq;
using Application.Repositories;

namespace Application.Test.Objetivos.Queries.GetResumoObjetivosList;

public class GetResumoObjetivosListQueryTests
{
    [Fact]
    public async Task ExecuteAsync_MapsObjectiveSummaryFromActiveMovements()
    {
        var ownerId = Guid.NewGuid();
        var objetivo = new Objetivo
        {
            OwnerId = ownerId,
            Nome = "Reserva",
            Descricao = "Emergências",
            Meta = 12000m,
            FontePagadora = "Salário",
            AporteMensal = 600m,
            OndeAplicar = "Tesouro Selic",
            Movimentos =
            [
                new Movimento { EstaAtivo = true, ValorAporte = 1000m, ValorLiquidoAtual = 1050m },
                new Movimento { EstaAtivo = true, ValorAporte = 500m, ValorLiquidoAtual = 525m },
                new Movimento { EstaAtivo = false, ValorAporte = 200m, ValorLiquidoAtual = 250m }
            ]
        };
        var objetivos = new[] { objetivo };
        var objetivoRepository = new Mock<IObjetivoRepository>();
        objetivoRepository
            .Setup(repository => repository.GetAllWithRelatedEntitiesAsync(ownerId, CancellationToken.None))
            .ReturnsAsync(objetivos);
        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(repository => repository.Objetivo).Returns(objetivoRepository.Object);
        var query = new GetResumoObjetivosListQuery(repositoryManager.Object, Mock.Of<IDateTimeProvider>());

        var result = await query.ExecuteAsync(ownerId, CancellationToken.None);

        var model = Assert.Single(result);
        Assert.Equal(objetivo.Id, model.Id);
        Assert.Equal("Reserva", model.Nome);
        Assert.Equal(12000m, model.Meta);
        Assert.Equal(13.125m, model.PercentualMeta);
        Assert.Equal(2, model.QtdeMovimentos);
        Assert.Equal(1500m, model.TotalAportado);
        Assert.Equal(1575m, model.TotalLiquido);
        objetivoRepository.Verify(repository => repository.GetAllWithRelatedEntitiesAsync(ownerId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenObjectiveHasNoMovements_ReturnsZeroTotals()
    {
        var ownerId = Guid.NewGuid();
        var objetivo = new Objetivo { OwnerId = ownerId, Nome = "Novo objetivo", Meta = 5000m };
        var objetivoRepository = new Mock<IObjetivoRepository>();
        objetivoRepository
            .Setup(repository => repository.GetAllWithRelatedEntitiesAsync(ownerId, CancellationToken.None))
            .ReturnsAsync(new[] { objetivo });
        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(repository => repository.Objetivo).Returns(objetivoRepository.Object);
        var query = new GetResumoObjetivosListQuery(repositoryManager.Object, Mock.Of<IDateTimeProvider>());

        var result = await query.ExecuteAsync(ownerId, CancellationToken.None);

        var model = Assert.Single(result);
        Assert.Equal(0m, model.PercentualMeta);
        Assert.Equal(0, model.QtdeMovimentos);
        Assert.Equal(0m, model.TotalAportado);
        Assert.Equal(0m, model.TotalLiquido);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsOnlyActiveObjectives()
    {
        var ownerId = Guid.NewGuid();
        var objetivoAtivo = new Objetivo { OwnerId = ownerId, Nome = "Ativo", Meta = 100m, EstaAtivo = true };
        var objetivoInativo = new Objetivo { OwnerId = ownerId, Nome = "Inativo", Meta = 100m, EstaAtivo = false };
        var objetivoRepository = new Mock<IObjetivoRepository>();
        objetivoRepository
            .Setup(repository => repository.GetAllWithRelatedEntitiesAsync(ownerId, CancellationToken.None))
            .ReturnsAsync([objetivoAtivo, objetivoInativo]);
        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(repository => repository.Objetivo).Returns(objetivoRepository.Object);
        var query = new GetResumoObjetivosListQuery(repositoryManager.Object, Mock.Of<IDateTimeProvider>());

        var result = await query.ExecuteAsync(ownerId, CancellationToken.None);

        var resumo = Assert.Single(result);
        Assert.Equal(objetivoAtivo.Id, resumo.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryThrows_Rethrows()
    {
        var ownerId = Guid.NewGuid();
        var exception = new InvalidOperationException("Falha ao consultar objetivos");
        var objetivoRepository = new Mock<IObjetivoRepository>();
        objetivoRepository
            .Setup(repository => repository.GetAllWithRelatedEntitiesAsync(ownerId, CancellationToken.None))
            .ThrowsAsync(exception);
        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(repository => repository.Objetivo).Returns(objetivoRepository.Object);
        var query = new GetResumoObjetivosListQuery(repositoryManager.Object, Mock.Of<IDateTimeProvider>());

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => query.ExecuteAsync(ownerId, CancellationToken.None));

        Assert.Same(exception, actualException);
    }
}
