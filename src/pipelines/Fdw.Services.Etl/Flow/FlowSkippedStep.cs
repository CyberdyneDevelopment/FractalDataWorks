using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Stands in for a step with an outside effect (a notification, SQL, another pipeline) in a test run, and does nothing (FDW-1665).
/// </summary>
public sealed class FlowSkippedStep : IFlowStep
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowSkippedStep"/> class.</summary>
    /// <param name="name">The name of the step it stands in for.</param>
    /// <param name="kind">The kind of the step it stands in for.</param>
    /// <param name="logger">The logger.</param>
    public FlowSkippedStep(string name, string kind, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        Name = name;
        Kind = kind;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>Gets the kind of the step it stands in for.</summary>
    public string Kind { get; }

    /// <inheritdoc />
    public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        FlowLog.StepSkippedInTestRun(_logger, Name, Kind);
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
