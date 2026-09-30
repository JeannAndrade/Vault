using Application.Corretoras;
using Application.Corretoras.Commands.CreateCorretora;
using Application.Corretoras.Commands.DeleteCorretora;
using Application.Corretoras.Commands.UpdateCorretora;
using Application.Corretoras.Queries.GetCorretora;
using Application.Corretoras.Queries.GetCorretoraList;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Corretoras;
using Service.Corretoras.DTOs;

namespace Service.Test.Corretoras;

public class CorretorasControllerTests
{
    private readonly Mock<IGetCorretorasListQuery> _getCorretorasListQuery = new();
    private readonly Mock<IGetCorretoraQuery> _getCorretoraQuery = new();
    private readonly Mock<ICreateCorretoraCommand> _createCorretoraCommand = new();
    private readonly Mock<IUpdateCorretoraCommand> _updateCorretoraCommand = new();
    private readonly Mock<IDeleteCorretoraCommand> _deleteCorretoraCommand = new();
    private readonly CorretorasController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public CorretorasControllerTests()
    {
        _controller = new CorretorasController(
            _getCorretorasListQuery.Object,
            _getCorretoraQuery.Object,
            _createCorretoraCommand.Object,
            _updateCorretoraCommand.Object,
            _deleteCorretoraCommand.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _controller.HttpContext.Items["UserId"] = _userId.ToString();
    }

    [Fact]
    public async Task GetCorretoras_ReturnsDtosWithoutLeakingUserId()
    {
        // Arrange: CorretoraModel tem UserId; CorretoraDto não — o teste prova a conversão.
        var corretoras = new List<CorretoraModel> { new() { Id = Guid.NewGuid(), Nome = "XP", UserId = _userId } };
        _getCorretorasListQuery.Setup(q => q.ExecuteAsync(_userId)).ReturnsAsync(corretoras);

        // Act
        var result = await _controller.GetCorretoras();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<List<CorretoraDto>>(ok.Value);
        Assert.Single(dtos);
        Assert.Equal("XP", dtos[0].Nome);
    }

    [Fact]
    public async Task GetCorretora_ReturnsDtoMappedFromApplicationModel()
    {
        // Arrange
        var corretoraId = Guid.NewGuid();
        var corretora = new CorretoraModel { Id = corretoraId, Nome = "XP", UserId = _userId };
        _getCorretoraQuery.Setup(q => q.ExecuteAsync(_userId, corretoraId)).ReturnsAsync(corretora);

        // Act
        var result = await _controller.GetCorretora(corretoraId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<CorretoraDto>(ok.Value);
        Assert.Equal("XP", dto.Nome);
    }

    [Fact]
    public async Task CreateCorretora_ReturnsDtoMappedFromApplicationModel()
    {
        // Arrange
        var created = new CorretoraModel { Id = Guid.NewGuid(), Nome = "XP", UserId = _userId };
        _createCorretoraCommand
            .Setup(c => c.ExecuteAsync(It.IsAny<CorretoraModelForCreation>(), _userId))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.CreateCorretora(new CorretoraForCreationDto("XP"));

        // Assert
        var createdResult = Assert.IsType<CreatedAtRouteResult>(result.Result);
        var dto = Assert.IsType<CorretoraDto>(createdResult.Value);
        Assert.Equal("XP", dto.Nome);
    }
}
