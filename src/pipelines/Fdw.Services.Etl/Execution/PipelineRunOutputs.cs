using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// What a run gave back (FDW-6136): the rows of <c>etl.PipelineRunOutput</c> in the operational store, one for each output the run's pipeline declares. Written once, when the run ends successfully; read by
/// the pipeline that started the run and waited. OpsDb has no row-level security, so every read filters by the tenant.
/// </summary>
public sealed class PipelineRunOutputs : IPipelineRunOutputs
{
    private const string PathName = "etl";
    private const string ContainerName = "PipelineRunOutput";

    private readonly IDataGatewayProvider _gateways;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunOutputs"/> class.</summary>
    /// <param name="gateways">Gives the data gateway the operational store is reached through.</param>
    /// <param name="logger">The logger.</param>
    public PipelineRunOutputs(IDataGatewayProvider gateways, ILogger<PipelineRunOutputs>? logger = null)
    {
        _gateways = gateways;
        _logger = logger ?? NullLogger<PipelineRunOutputs>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Record(Guid executionId, Guid tenantId, IReadOnlyList<PipelineOutputValue> outputs, CancellationToken cancellationToken = default)
    {
        var store = PipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(store))
        {
            return GenericResult.Failure(PipelineOutputLog.NotWritten(_logger, executionId, "the operational store is not named"));
        }

        var gateway = _gateways.ByName(DataGatewayNames.Main);
        foreach (var output in outputs)
        {
            var inserted = await gateway.Execute<int>(
                Insert.Into<EtlPipelineRunOutputRecord>(ContainerName).DataStore(store).Path(PathName)
                    .Value(new EtlPipelineRunOutputRecord { ExecutionId = executionId, TenantId = tenantId, Name = output.Name, DataType = output.DataType, ValueText = output.Text }),
                cancellationToken).ConfigureAwait(false);
            if (inserted.IsFailure)
            {
                return GenericResult.Failure(PipelineOutputLog.NotWritten(_logger, executionId, LoadFailureText.Of(inserted)));
            }
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<PipelineOutputValue>>> Read(Guid executionId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var store = PipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(store))
        {
            return GenericResult<IReadOnlyList<PipelineOutputValue>>.Failure(PipelineOutputLog.NotRead(_logger, executionId, "the operational store is not named"));
        }

        var read = await _gateways.ByName(DataGatewayNames.Main).Execute<IEnumerable<EtlPipelineRunOutputRecord>>(
            Query.From<EtlPipelineRunOutputRecord>(store, PathName, ContainerName)
                .Where(r => r.ExecutionId).Equal(executionId)
                .Where(r => r.TenantId).Equal(tenantId)
                .Build(),
            cancellationToken).ConfigureAwait(false);
        return read.IsFailure || read.Value is null
            ? GenericResult<IReadOnlyList<PipelineOutputValue>>.Failure(PipelineOutputLog.NotRead(_logger, executionId, LoadFailureText.Of(read)))
            : GenericResult<IReadOnlyList<PipelineOutputValue>>.Success([.. read.Value.Select(row => new PipelineOutputValue(row.Name, row.DataType, row.ValueText))]);
    }
}
