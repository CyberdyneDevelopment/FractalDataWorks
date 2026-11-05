using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>Reads a pipeline's finished runs from the operational store and the events of the execution tracker (FDW-3864).</summary>
public sealed class PipelineRunHistory : IPipelineRunHistory
{
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IExecutionTracker _tracker;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunHistory"/> class.</summary>
    public PipelineRunHistory(IDataGatewayProvider dataGateways, IExecutionTracker tracker, ILogger<PipelineRunHistory>? logger = null)
    {
        _dataGateways = dataGateways;
        _tracker = tracker;
        _logger = logger ?? NullLogger<PipelineRunHistory>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<PipelineRunOutcome>>> Page(string pipelineName, int skip, int take, CancellationToken cancellationToken = default) =>
        EtlPipelineTypes.OperationalConnection is { } connection
            ? PipelineRunOutcomeReader.Page(_dataGateways.ByName("Main"), _tracker, _logger, connection, pipelineName, skip, take, cancellationToken)
            : Task.FromResult(GenericResult<IReadOnlyList<PipelineRunOutcome>>.Failure(PipelineFailureRuleLog.OperationalConnectionNotNamed(_logger, pipelineName)));
}
