using Application.Objetivos;
using Application.Objetivos.Commands.CreateObjetivo;
using Application.Objetivos.Commands.DeleteObjetivo;
using Application.Objetivos.Commands.UpdateObjetivo;
using Application.Objetivos.Queries.GetObjetivo;
using Application.Objetivos.Queries.GetObjetivoComValoresList;
using Application.Objetivos.Queries.GetObjetivoList;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Objetivos;
using Service.Objetivos.DTOs;

namespace Service.Test.Objetivos;

public class ObjetivosControllerTests
{
    private readonly Mock<IGetObjetivosListQuery> _getObjetivosListQuery = new();
    private readonly Mock<IGetObjetivoComValoresListQuery> _getObjetivoComValoresListQuery = new();
    private readonly Mock<IGetObjetivoQuery> _getObjetivoQuery = new();
    private readonly Mock<ICreateObjetivoCommand> _createObjetivoCommand = new();
    private readonly Mock<IUpdateObjetivoCommand> _updateObjetivoCommand = new();
    private readonly Mock<IDeleteObjetivoCommand> _deleteObjetivoCommand = new();
    private readonly ObjetivosController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public ObjetivosControllerTests()
    {
        _controller = new ObjetivosController(
            _getObjetivosListQuery.Object,
            _getObjetivoComValoresListQuery.Object,
            _getObjetivoQuery.Object,
            _createObjetivoCommand.Object,
            _updateObjetivoCommand.Object,
            _deleteObjetivoCommand.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _controller.HttpContext.Items["UserId"] = _userId.ToString();
    }

    [Fact]
    public async Task GetObjetivos_ReturnsDtosWithoutLeakingUserId()
    {
        var objetivos = new List<ObjetivoModel>
        {
            new() { Id = Guid.NewGuid(), Nome = "Reserva de emergência", UserId = _userId }
        };
        _getObjetivosListQuery.Setup(q => q.ExecuteAsync(_userId)).ReturnsAsync(objetivos);

        var result = await _controller.GetObjetivos();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<List<ObjetivoDto>>(ok.Value);
        Assert.Single(dtos);
        Assert.Equal("Reserva de emergência", dtos[0].Nome);
    }

    [Fact]
    public async Task GetObjetivosComValores_ReturnsMappedDtosForCurrentUser()
    {
        var objetivoId = Guid.NewGuid();
        var objetivos = new List<ObjetivoComValoresModel>
        {
            new()
            {
                Id = objetivoId,
                Nome = "Reserva de emergência",
                Meta = 10000m,
                FontePagadora = "Salário",
                AporteMensal = 500m,
                OndeAplicar = "Tesouro Selic",
                ValorTotalInvestido = 1500m,
                ValorTotalLiquido = 1575m,
                UserId = _userId
            }
        };
        _getObjetivoComValoresListQuery
            .Setup(q => q.ExecuteAsync(_userId))
            .ReturnsAsync(objetivos);

        var result = await _controller.GetObjetivosComValores();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<List<ObjetivoComValoresDto>>(ok.Value);
        var dto = Assert.Single(dtos);
        Assert.Equal(objetivoId, dto.Id);
        Assert.Equal("Reserva de emergência", dto.Nome);
        Assert.Equal(10000m, dto.Meta);
        Assert.Equal("Salário", dto.FontePagadora);
        Assert.Equal(500m, dto.AporteMensal);
        Assert.Equal("Tesouro Selic", dto.OndeAplicar);
        Assert.Equal(1500m, dto.ValorTotalInvestido);
        Assert.Equal(1575m, dto.ValorTotalLiquido);
        _getObjetivoComValoresListQuery.Verify(q => q.ExecuteAsync(_userId), Times.Once);
    }

    [Fact]
    public async Task GetObjetivo_ReturnsDtoMappedFromApplicationModel()
    {
        var objetivoId = Guid.NewGuid();
        var objetivo = new ObjetivoModel { Id = objetivoId, Nome = "Reserva de emergência", UserId = _userId };
        _getObjetivoQuery.Setup(q => q.ExecuteAsync(_userId, objetivoId)).ReturnsAsync(objetivo);

        var result = await _controller.GetObjetivo(objetivoId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ObjetivoDto>(ok.Value);
        Assert.Equal("Reserva de emergência", dto.Nome);
    }

    [Fact]
    public async Task CreateObjetivo_ReturnsDtoMappedFromApplicationModel()
    {
        var created = new ObjetivoModel { Id = Guid.NewGuid(), Nome = "Reserva de emergência", UserId = _userId };
        _createObjetivoCommand
            .Setup(c => c.ExecuteAsync(It.IsAny<ObjetivoModelForCreation>()))
            .ReturnsAsync(created);

        var result = await _controller.CreateObjetivo(new ObjetivoForCreationDto(
            "Reserva de emergência", "Descrição", 10000m, "Salário", 500m, "Tesouro Selic"));

        var createdResult = Assert.IsType<CreatedAtRouteResult>(result.Result);
        var dto = Assert.IsType<ObjetivoDto>(createdResult.Value);
        Assert.Equal("Reserva de emergência", dto.Nome);
    }

    [Fact]
    public async Task DeleteObjetivo_CallsCommandAndReturnsNoContent()
    {
        var objetivoId = Guid.NewGuid();

        var result = await _controller.DeleteObjetivo(objetivoId);

        Assert.IsType<NoContentResult>(result);
        _deleteObjetivoCommand.Verify(c => c.ExecuteAsync(_userId, objetivoId), Times.Once);
    }
}
