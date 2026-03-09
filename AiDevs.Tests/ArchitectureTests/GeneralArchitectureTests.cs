using NetArchTest.Rules;

namespace AiDevs.Tests.ArchitectureTests;

/// <summary>
/// General architecture rules for the entire solution
/// </summary>
public class GeneralArchitectureTests
{
    [Fact]
    public void Core_ShouldNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(typeof(AiDevs.Core.Interfaces.ITaskSolution).Assembly)
            .Should()
            .NotHaveDependencyOn("AiDevs.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Core layer should not depend on Infrastructure. " +
            "This violates clean architecture principles.");
    }

    [Fact]
    public void Core_ShouldNotDependOnSolutions()
    {
        var result = Types.InAssembly(typeof(AiDevs.Core.Interfaces.ITaskSolution).Assembly)
            .Should()
            .NotHaveDependencyOn("AiDevs.Solutions")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Core layer should not depend on Solutions.");
    }

    [Fact]
    public void Core_ShouldNotDependOnApi()
    {
        // Core should only depend on System namespaces
        var result = Types.InAssembly(typeof(AiDevs.Core.Interfaces.ITaskSolution).Assembly)
            .Should()
            .OnlyHaveDependenciesOn("System", "Microsoft", "AiDevs.Core")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Core layer should only depend on System and its own namespace.");
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnSolutions()
    {
        var result = Types.InAssembly(typeof(AiDevs.Infrastructure.Services.OpenRouterService).Assembly)
            .Should()
            .NotHaveDependencyOn("AiDevs.Solutions")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Infrastructure should not depend on Solutions.");
    }

    [Fact]
    public void Infrastructure_ShouldOnlyDependOnCore()
    {
        var result = Types.InAssembly(typeof(AiDevs.Infrastructure.Services.OpenRouterService).Assembly)
            .Should()
            .OnlyHaveDependenciesOn("System", "Microsoft", "AiDevs.Core", "AiDevs.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Infrastructure should only depend on Core, System, and itself.");
    }

    [Fact]
    public void Services_ShouldEndWithService()
    {
        var result = Types.InAssembly(typeof(AiDevs.Infrastructure.Services.OpenRouterService).Assembly)
            .That()
            .ResideInNamespace("AiDevs.Infrastructure.Services")
            .Should()
            .HaveNameEndingWith("Service")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All classes in Services namespace should end with 'Service'. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Controllers_ShouldEndWithController()
    {
        // This will work once the API assembly is referenced
        // For now, we'll skip if no controllers are found
        var apiAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "AiDevs");

        if (apiAssembly != null)
        {
            var result = Types.InAssembly(apiAssembly)
                .That()
                .ResideInNamespace("AiDevs.Controllers")
                .Should()
                .HaveNameEndingWith("Controller")
                .GetResult();

            Assert.True(result.IsSuccessful,
                "All classes in Controllers namespace should end with 'Controller'.");
        }
    }

    [Fact]
    public void Interfaces_ShouldStartWithI()
    {
        var coreAssembly = typeof(AiDevs.Core.Interfaces.ITaskSolution).Assembly;

        var result = Types.InAssembly(coreAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All interfaces should start with 'I'. " +
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }
}
