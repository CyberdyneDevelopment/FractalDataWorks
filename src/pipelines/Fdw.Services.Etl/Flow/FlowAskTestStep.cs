using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// An Ask component in a test run (FDW-6142): a test run keeps no request and tells nobody, so the step judges nothing and fires neither output; the components its lines lead to are skipped, as they are when
/// the call may not go on.
/// </summary>
public sealed class FlowAskTestStep : IFlowDecision
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowAskTestStep"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="logger">The logger.</param>
    public FlowAskTestStep(string name, ILogger logger)
    {
        Name = name;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs { get; } = [AskComponentType.Go, AskComponentType.Asked];

    /// <inheritdoc />
    public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        FlowFired.Set(context.Variables, Name, AskComponentType.Go, false);
        FlowFired.Set(context.Variables, Name, AskComponentType.Asked, false);
        _ = AnswerRequestLog.AskSkippedInATest(_logger, Name);
        return Task.FromResult(GenericResult.Success());
    }
}
