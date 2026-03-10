namespace AiDevs.Infrastructure.FunctionCalling;

public interface IFunctionHandler<TParameters>
{
    Task<string> ExecuteAsync(TParameters parameters, CancellationToken cancellationToken = default);
}
