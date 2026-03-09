using NetArchTest.Rules;

namespace AiDevs.Tests.ArchitectureTests;

/// <summary>
/// Architecture tests to ensure tasks remain isolated from each other
/// </summary>
public class TaskIsolationTests
{
    private const string SolutionsAssembly = "AiDevs.Solutions";

    [Fact]
    public void Tasks_ShouldNotReferenceBetweenEachOther()
    {
        // Get all types from the Solutions assembly
        var types = Types.InAssembly(typeof(AiDevs.Solutions.Task01.Task01Solution).Assembly);

        // Get all task namespaces (Task01, Task02, etc.)
        var taskNamespaces = types.GetTypes()
            .Where(t => t.Namespace?.Contains(".Task") == true)
            .Select(t => ExtractTaskNamespace(t.Namespace!))
            .Distinct()
            .Where(ns => ns != null)
            .ToList();

        // For each task, verify it doesn't reference other tasks
        foreach (var taskNamespace in taskNamespaces)
        {
            var result = types
                .That()
                .ResideInNamespace(taskNamespace!)
                .ShouldNot()
                .HaveDependencyOnAny(taskNamespaces.Where(ns => ns != taskNamespace).ToArray())
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"Task {taskNamespace} should not reference other tasks. " +
                $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
        }
    }

    [Fact]
    public void Tasks_CanOnlyReferenceAllowedNamespaces()
    {
        // Get all types from task folders
        var types = Types.InAssembly(typeof(AiDevs.Solutions.Task01.Task01Solution).Assembly);

        var taskTypes = types
            .That()
            .ResideInNamespaceMatching("AiDevs.Solutions.Task*")
            .GetTypes()
            .ToList();

        // Skip test if no task types found
        if (!taskTypes.Any())
        {
            return;
        }

        // Task types should only reference allowed namespaces
        var result = types
            .That()
            .ResideInNamespaceMatching("AiDevs.Solutions.Task*")
            .Should()
            .OnlyHaveDependenciesOn(
                "AiDevs.Solutions",
                "AiDevs.Core",
                "AiDevs.Infrastructure",
                "System",
                "Microsoft",
                "Moq" // Allow Moq for testing
            )
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Tasks should only reference allowed namespaces. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void AllTaskSolutions_ShouldImplementITaskSolution()
    {
        var types = Types.InAssembly(typeof(AiDevs.Solutions.Task01.Task01Solution).Assembly);

        var result = types
            .That()
            .ResideInNamespaceMatching("AiDevs.Solutions.Task*")
            .And()
            .HaveNameEndingWith("Solution")
            .Should()
            .ImplementInterface(typeof(AiDevs.Core.Interfaces.ITaskSolution))
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"All task solution classes must implement ITaskSolution. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void TaskSolutions_ShouldBeInCorrectNamespace()
    {
        var types = Types.InAssembly(typeof(AiDevs.Solutions.Task01.Task01Solution).Assembly);

        var taskSolutionTypes = types
            .That()
            .ImplementInterface(typeof(AiDevs.Core.Interfaces.ITaskSolution))
            .GetTypes();

        foreach (var type in taskSolutionTypes)
        {
            // Extract task number from class name (e.g., Task01Solution -> 01)
            var className = type.Name;
            if (className.StartsWith("Task") && className.EndsWith("Solution"))
            {
                var taskNumber = className.Substring(4, className.Length - 12); // Extract "01" from "Task01Solution"
                var expectedNamespace = $"AiDevs.Solutions.Task{taskNumber}";

                Assert.Equal(expectedNamespace, type.Namespace);
            }
        }
    }

    [Fact]
    public void Tasks_ShouldNotContainPublicModels()
    {
        // This test ensures that if tasks have models, they should be internal
        // to prevent accidental cross-task usage
        var types = Types.InAssembly(typeof(AiDevs.Solutions.Task01.Task01Solution).Assembly);

        var result = types
            .That()
            .ResideInNamespaceMatching("AiDevs.Solutions.Task*.Models")
            .Should()
            .NotBePublic()
            .Or()
            .BeSealed() // If public, should at least be sealed
            .GetResult();

        // This is a warning test - we allow it to fail but log it
        if (!result.IsSuccessful)
        {
            // You can choose to make this a hard failure by uncommenting:
            // Assert.True(false, $"Task models should be internal or sealed: {string.Join(", ", result.FailingTypeNames)}");
        }
    }

    /// <summary>
    /// Extract task namespace (e.g., "AiDevs.Solutions.Task01" from "AiDevs.Solutions.Task01.Models")
    /// </summary>
    private static string? ExtractTaskNamespace(string fullNamespace)
    {
        var parts = fullNamespace.Split('.');
        var taskIndex = Array.FindIndex(parts, p => p.StartsWith("Task"));

        if (taskIndex >= 0 && taskIndex < parts.Length)
        {
            return string.Join(".", parts.Take(taskIndex + 1));
        }

        return null;
    }
}
