using AiDevs.Infrastructure.Services;
using AiDevs.Solutions.Task01;
using Moq;

namespace AiDevs.Tests.UnitTests;

/// <summary>
/// Example unit tests for Task01Solution
/// </summary>
public class Task01SolutionTests
{
    [Fact]
    public void TaskId_ShouldReturn1()
    {
        // Arrange
        var mockOpenRouter = new Mock<IOpenRouterService>();
        var solution = new Task01Solution(mockOpenRouter.Object);

        // Act
        var taskId = solution.TaskId;

        // Assert
        Assert.Equal(1, taskId);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ShouldReturnSuccess()
    {
        // Arrange
        var mockOpenRouter = new Mock<IOpenRouterService>();
        mockOpenRouter
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<double>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("AI response");

        var solution = new Task01Solution(mockOpenRouter.Object);

        // Act
        var result = await solution.ExecuteAsync("test input");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("AI response", result.Output);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOpenRouterFails_ShouldReturnFailure()
    {
        // Arrange
        var mockOpenRouter = new Mock<IOpenRouterService>();
        mockOpenRouter
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<double>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API error"));

        var solution = new Task01Solution(mockOpenRouter.Object);

        // Act
        var result = await solution.ExecuteAsync("test input");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("Task 01 failed", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncludeMetadata()
    {
        // Arrange
        var mockOpenRouter = new Mock<IOpenRouterService>();
        mockOpenRouter
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<double>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("AI response");

        var solution = new Task01Solution(mockOpenRouter.Object);
        var input = "test";

        // Act
        var result = await solution.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result.Metadata);
        Assert.Equal(1, result.Metadata["taskId"]);
        Assert.Equal(input.Length, result.Metadata["inputLength"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallOpenRouterWithCorrectParameters()
    {
        // Arrange
        var mockOpenRouter = new Mock<IOpenRouterService>();
        mockOpenRouter
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<double>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("AI response");

        var solution = new Task01Solution(mockOpenRouter.Object);

        // Act
        await solution.ExecuteAsync("test input");

        // Assert
        mockOpenRouter.Verify(x => x.CompleteAsync(
            It.Is<string>(s => s.Contains("test input")),
            "openai/gpt-3.5-turbo",
            0.7,
            null,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
