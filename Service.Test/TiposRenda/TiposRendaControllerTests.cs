using Application.TiposRenda;
using Application.TiposRenda.Commands.CreateTipoRenda;
using Application.TiposRenda.Commands.DeleteTipoRenda;
using Application.TiposRenda.Commands.UpdateTipoRenda;
using Application.TiposRenda.Queries.GetTipoRenda;
using Application.TiposRenda.Queries.GetTiposRendaList;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.TiposRenda;
using Service.TiposRenda.DTOs;

namespace Service.Test.TiposRenda;

public class TiposRendaControllerTests
{
    private readonly Mock<IGetTiposRendaListQuery> _getTiposRendaListQuery = new();
    private readonly Mock<IGetTipoRendaQuery> _getTipoRendaQuery = new();
    private readonly Mock<ICreateTipoRendaCommand> _createTipoRendaCommand = new();
    private readonly Mock<IUpdateTipoRendaCommand> _updateTipoRendaCommand = new();
    private readonly Mock<IDeleteTipoRendaCommand> _deleteTipoRendaCommand = new();
    private readonly TiposRendaController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public TiposRendaControllerTests()
    {
        _controller = new TiposRendaController(
            _getTiposRendaListQuery.Object,
            _getTipoRendaQuery.Object,
            _createTipoRendaCommand.Object,
            _updateTipoRendaCommand.Object,
            _deleteTipoRendaCommand.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _controller.HttpContext.Items["UserId"] = _userId.ToString();
    }

    [Fact]
    public async Task GetTiposRenda_ReturnsDtosWithoutLeakingUserId()
    {
        var tiposRenda = new List<TipoRendaModel> { new() { Id = Guid.NewGuid(), Nome = "CDB", UserId = _userId } };
        _getTiposRendaListQuery.Setup(q => q.ExecuteAsync(_userId)).ReturnsAsync(tiposRenda);

        var result = await _controller.GetTiposRenda();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<List<TipoRendaDto>>(ok.Value);
        Assert.Single(dtos);
        Assert.Equal("CDB", dtos[0].Nome);
    }

    [Fact]
    public async Task GetTipoRenda_ReturnsDtoMappedFromApplicationModel()
    {
        var tipoRendaId = Guid.NewGuid();
        var tipoRenda = new TipoRendaModel { Id = tipoRendaId, Nome = "CDB", UserId = _userId };
        _getTipoRendaQuery.Setup(q => q.ExecuteAsync(_userId, tipoRendaId)).ReturnsAsync(tipoRenda);

        var result = await _controller.GetTipoRenda(tipoRendaId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<TipoRendaDto>(ok.Value);
        Assert.Equal("CDB", dto.Nome);
    }

    [Fact]
    public async Task CreateTipoRenda_ReturnsDtoMappedFromApplicationModel()
    {
        var created = new TipoRendaModel { Id = Guid.NewGuid(), Nome = "CDB", UserId = _userId };
        _createTipoRendaCommand
            .Setup(c => c.ExecuteAsync(It.IsAny<TipoRendaModelForCreation>()))
            .ReturnsAsync(created);

        var result = await _controller.CreateTipoRenda(new TipoRendaForCreationDto("CDB"));

        var createdResult = Assert.IsType<CreatedAtRouteResult>(result.Result);
        var dto = Assert.IsType<TipoRendaDto>(createdResult.Value);
        Assert.Equal("CDB", dto.Nome);
    }

    [Fact]
    public async Task DeleteTipoRenda_CallsCommandAndReturnsNoContent()
    {
        var tipoRendaId = Guid.NewGuid();

        var result = await _controller.DeleteTipoRenda(tipoRendaId);

        Assert.IsType<NoContentResult>(result);
        _deleteTipoRendaCommand.Verify(c => c.ExecuteAsync(_userId, tipoRendaId), Times.Once);
    }
}
