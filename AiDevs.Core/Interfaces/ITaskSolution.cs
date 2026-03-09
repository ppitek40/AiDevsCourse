namespace AiDevs.Core.Interfaces;

/// <summary>
/// Interface that all task solutions must implement
/// </summary>
public interface ITaskSolution
{
    /// <summary>
    /// Unique task identifier (1-25)
    /// </summary>
    int TaskId { get; }

    /// <summary>
    /// Execute the solution with given input
    /// </summary>
    /// <param name="input">Task-specific input data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Solution result</returns>
    Task<SolutionResult> ExecuteAsync(string input, CancellationToken cancellationToken = default);
}
