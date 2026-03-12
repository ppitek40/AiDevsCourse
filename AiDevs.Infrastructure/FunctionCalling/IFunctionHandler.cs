namespace AiDevs.Infrastructure.FunctionCalling;

public interface IFunctionHandler
{
    Type ParametersType { get; }
    Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default);
}
