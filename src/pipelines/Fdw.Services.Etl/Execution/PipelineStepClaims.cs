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
/// The claims of the steps that act outside the run (FDW-6139), the rows of <c>etl.PipelineStepClaim</c> in the operational store. A claim is one insert; the table's unique key refuses a second claim of the
/// same step, and what the refusal means is read from the row that is there, never from the text of an error: a run that overlaps another, or a restart, finds the first claim and its outcome.
/// </summary>
public sealed class PipelineStepClaims : IPipelineStepClaims
{
    private const string PathName = "etl";
    private const string ContainerName = "PipelineStepClaim";

    private readonly IDataGatewayProvider _gateways;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineStepClaims"/> class.</summary>
    /// <param name="gateways">Gives the data gateway the operational store is reached through.</param>
    /// <param name="logger">The logger.</param>
    public PipelineStepClaims(IDataGatewayProvider gateways, ILogger<PipelineStepClaims>? logger = null)
    {
        _gateways = gateways;
        _logger = logger ?? NullLogger<PipelineStepClaims>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<StepClaim>> Claim(Guid tenantId, Guid claimRootId, string component, int iteration, Guid executionId, CancellationToken cancellationToken = default)
    {
        var store = PipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(store))
        {
            return GenericResult<StepClaim>.Failure(PipelineStepClaimLog.CannotClaim(_logger, component, "no operational store is named"));
        }

        var gateway = _gateways.ByName(DataGatewayNames.Main);
        var row = new EtlPipelineStepClaimRecord { TenantId = tenantId, ClaimRootId = claimRootId, ComponentName = component, Iteration = iteration, ExecutionId = executionId };
        var inserted = await gateway.Execute<int>(
            Insert.Into<EtlPipelineStepClaimRecord>(ContainerName).DataStore(store).Path(PathName).Value(row), cancellationToken).ConfigureAwait(false);
        if (inserted.IsSuccess)
        {
            return GenericResult<StepClaim>.Success(new StepClaim(row.Id, true, executionId, null));
        }

        var found = await Read(gateway, store, tenantId, claimRootId, component, iteration, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null)
        {
            return GenericResult<StepClaim>.Failure(PipelineStepClaimLog.NotClaimed(_logger, component, LoadFailureText.Of(inserted)));
        }

        return GenericResult<StepClaim>.Success(new StepClaim(found.Value.Id, false, found.Value.ExecutionId, found.Value.Outcome));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Record(Guid tenantId, Guid claimId, string outcome, CancellationToken cancellationToken = default)
    {
        var store = PipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(store))
        {
            return GenericResult.Failure(PipelineStepClaimLog.CannotClaim(_logger, claimId.ToString(), "no operational store is named"));
        }

        var written = await _gateways.ByName(DataGatewayNames.Main).Execute<int>(
            Update.In<EtlPipelineStepClaimOutcome>(ContainerName).DataStore(store).Path(PathName)
                .Where("Id", claimId)
                .Where("TenantId", tenantId)
                .Value(new EtlPipelineStepClaimOutcome { Outcome = outcome }),
            cancellationToken).ConfigureAwait(false);
        return written.IsFailure
            ? GenericResult.Failure(PipelineStepClaimLog.OutcomeNotRecorded(_logger, claimId.ToString(), outcome, LoadFailureText.Of(written)))
            : GenericResult.Success();
    }

    private static async Task<IGenericResult<EtlPipelineStepClaimRecord?>> Read(
        IDataGateway gateway, string store, Guid tenantId, Guid claimRootId, string component, int iteration, CancellationToken cancellationToken)
    {
        var read = await gateway.Execute<IEnumerable<EtlPipelineStepClaimRecord>>(
            Query.From<EtlPipelineStepClaimRecord>(store, PathName, ContainerName)
                .Where(r => r.TenantId).Equal(tenantId)
                .Where(r => r.ClaimRootId).Equal(claimRootId)
                .Where(r => r.ComponentName).Equal(component)
                .Where(r => r.Iteration).Equal(iteration)
                .Build(),
            cancellationToken).ConfigureAwait(false);
        return read.IsFailure || read.Value is null
            ? read.ToNewResult<EtlPipelineStepClaimRecord?>()
            : GenericResult<EtlPipelineStepClaimRecord?>.Success(read.Value.FirstOrDefault());
    }
}
