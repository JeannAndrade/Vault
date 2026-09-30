using Application.Emissores;
using Application.Emissores.Commands.CreateEmissor;
using Application.Emissores.Commands.DeleteEmissor;
using Application.Emissores.Commands.UpdateEmissor;
using Application.Emissores.Queries.GetEmissor;
using Application.Emissores.Queries.GetEmissorList;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Emissores;
using Service.Emissores.DTOs;

namespace Service.Test.Emissores;

public class EmissoresControllerTests
{
    private readonly Mock<IGetEmissoresListQuery> _getEmissoresListQuery = new();
    private readonly Mock<IGetEmissorQuery> _getEmissorQuery = new();
    private readonly Mock<ICreateEmissorCommand> _createEmissorCommand = new();
    private readonly Mock<IUpdateEmissorCommand> _updateEmissorCommand = new();
    private readonly Mock<IDeleteEmissorCommand> _deleteEmissorCommand = new();
    private readonly EmissoresController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public EmissoresControllerTests()
    {
        _controller = new EmissoresController(
            _getEmissoresListQuery.Object,
            _getEmissorQuery.Object,
            _createEmissorCommand.Object,
            _updateEmissorCommand.Object,
            _deleteEmissorCommand.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _controller.HttpContext.Items["UserId"] = _userId.ToString();
    }

    [Fact]
    public async Task GetEmissores_ReturnsDtosWithoutLeakingUserId()
    {
        var emissores = new List<EmissorModel> { new() { Id = Guid.NewGuid(), Nome = "Tesouro Nacional", UserId = _userId } };
        _getEmissoresListQuery.Setup(q => q.ExecuteAsync(_userId)).ReturnsAsync(emissores);

        var result = await _controller.GetEmissores();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<List<EmissorDto>>(ok.Value);
        Assert.Single(dtos);
        Assert.Equal("Tesouro Nacional", dtos[0].Nome);
    }

    [Fact]
    public async Task GetEmissor_ReturnsDtoMappedFromApplicationModel()
    {
        var emissorId = Guid.NewGuid();
        var emissor = new EmissorModel { Id = emissorId, Nome = "Tesouro Nacional", UserId = _userId };
        _getEmissorQuery.Setup(q => q.ExecuteAsync(_userId, emissorId)).ReturnsAsync(emissor);

        var result = await _controller.GetEmissor(emissorId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<EmissorDto>(ok.Value);
        Assert.Equal("Tesouro Nacional", dto.Nome);
    }

    [Fact]
    public async Task CreateEmissor_ReturnsDtoMappedFromApplicationModel()
    {
        var created = new EmissorModel { Id = Guid.NewGuid(), Nome = "Tesouro Nacional", UserId = _userId };
        _createEmissorCommand
            .Setup(c => c.ExecuteAsync(It.IsAny<EmissorModelForCreation>()))
            .ReturnsAsync(created);

        var result = await _controller.CreateEmissor(new EmissorForCreationDto("Tesouro Nacional"));

        var createdResult = Assert.IsType<CreatedAtRouteResult>(result.Result);
        var dto = Assert.IsType<EmissorDto>(createdResult.Value);
        Assert.Equal("Tesouro Nacional", dto.Nome);
    }

    [Fact]
    public async Task DeleteEmissor_CallsCommandAndReturnsNoContent()
    {
        var emissorId = Guid.NewGuid();

        var result = await _controller.DeleteEmissor(emissorId);

        Assert.IsType<NoContentResult>(result);
        _deleteEmissorCommand.Verify(c => c.ExecuteAsync(_userId, emissorId), Times.Once);
    }
}
