using AiDevs.Infrastructure.Services;
using AiDevs.Solutions.Task01;
using Moq;

namespace AiDevs.Tests.UnitTests;

/// <summary>
/// Unit tests for Task01Solution
/// </summary>
public class Task01SolutionTests
{
    [Fact]
    public void TaskId_ShouldReturn1()
    {
        // Arrange
        var mockOpenRouter = new Mock<IOpenRouterService>();
        var solution = new Task01Solution(mockOpenRouter.Object, new Mock<IAiDevsApiService>().Object);

        // Act
        var taskId = solution.TaskId;

        // Assert
        Assert.Equal(1, taskId);
    }
}
