using AiDevs.Core.Models;

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
    /// Execute the solution with streaming progress updates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of progress updates</returns>
    IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(CancellationToken cancellationToken = default);
}
