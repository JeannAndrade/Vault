using Application.Objetivos.Queries.GetObjetivoComValoresList;
using Domain.Movimentos;
using Domain.Objetivos;
using LumiaFoundation.Logger.Contracts;
using Moq;
using Persistence.Managment;
using Persistence.Objetivos;

namespace Application.Test.Objetivos.Queries.GetObjetivoComValoresList;

public class GetObjetivoComValoresListQueryTests
{
    [Fact]
    public async Task ExecuteAsync_MapsObjectiveAndTotalsFromMovements()
    {
        var ownerId = Guid.NewGuid();
        var objetivo = new Objetivo
        {
            UserId = ownerId,
            Nome = "Reserva",
            Descricao = "Emergências",
            Meta = 12000m,
            FontePagadora = "Salário",
            AporteMensal = 600m,
            OndeAplicar = "Tesouro Selic",
            Movimentos =
            [
                new Movimento { ValorAporte = 1000m, ValorLiquidoAtual = 1050m },
                new Movimento { ValorAporte = 500m, ValorLiquidoAtual = 525m }
            ]
        };
        var objetivos = new[] { objetivo };
        var objetivoRepository = new Mock<IObjetivoRepository>();
        objetivoRepository
            .Setup(repository => repository.GetAllWithRelatedEntitiesAsync(ownerId))
            .ReturnsAsync(objetivos);
        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(repository => repository.Objetivo).Returns(objetivoRepository.Object);
        var logger = new Mock<ILoggerManager>();
        var query = new GetObjetivoComValoresListQuery(repositoryManager.Object, logger.Object);

        var result = await query.ExecuteAsync(ownerId);

        var model = Assert.Single(result);
        Assert.Equal(objetivo.Id, model.Id);
        Assert.Equal("Reserva", model.Nome);
        Assert.Equal(12000m, model.Meta);
        Assert.Equal("Salário", model.FontePagadora);
        Assert.Equal(600m, model.AporteMensal);
        Assert.Equal("Tesouro Selic", model.OndeAplicar);
        Assert.Equal(1500m, model.ValorTotalInvestido);
        Assert.Equal(1575m, model.ValorTotalLiquido);
        Assert.Equal(ownerId, model.UserId);
        objetivoRepository.Verify(repository => repository.GetAllWithRelatedEntitiesAsync(ownerId), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenObjectiveHasNoMovements_ReturnsZeroTotals()
    {
        var ownerId = Guid.NewGuid();
        var objetivo = new Objetivo { UserId = ownerId, Nome = "Novo objetivo" };
        var objetivoRepository = new Mock<IObjetivoRepository>();
        objetivoRepository
            .Setup(repository => repository.GetAllWithRelatedEntitiesAsync(ownerId))
            .ReturnsAsync(new[] { objetivo });
        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(repository => repository.Objetivo).Returns(objetivoRepository.Object);
        var query = new GetObjetivoComValoresListQuery(repositoryManager.Object, Mock.Of<ILoggerManager>());

        var result = await query.ExecuteAsync(ownerId);

        var model = Assert.Single(result);
        Assert.Equal(0m, model.ValorTotalInvestido);
        Assert.Equal(0m, model.ValorTotalLiquido);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryThrows_LogsAndRethrows()
    {
        var ownerId = Guid.NewGuid();
        var exception = new InvalidOperationException("Falha ao consultar objetivos");
        var objetivoRepository = new Mock<IObjetivoRepository>();
        objetivoRepository
            .Setup(repository => repository.GetAllWithRelatedEntitiesAsync(ownerId))
            .ThrowsAsync(exception);
        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(repository => repository.Objetivo).Returns(objetivoRepository.Object);
        var logger = new Mock<ILoggerManager>();
        var query = new GetObjetivoComValoresListQuery(repositoryManager.Object, logger.Object);

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => query.ExecuteAsync(ownerId));

        Assert.Same(exception, actualException);
        logger.Verify(
            log => log.LogError(It.Is<string>(message => message.Contains(nameof(GetObjetivoComValoresListQuery)))),
            Times.Once);
    }
}
