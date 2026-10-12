using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Dispatch;

/// <summary>
/// A no-op dispatcher that returns success without performing any dispatch.
/// Use this for domains where trigger-and-track is sufficient and the actual
/// execution dispatch is handled externally (e.g., by a polling scheduler or webhook callback).
/// </summary>
public sealed class NullOperationDispatcher : IOperationDispatcher
{
    /// <inheritdoc />
    public Task<IGenericResult> Dispatch(IExecutionItem execution, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(GenericResult.Success());
}
