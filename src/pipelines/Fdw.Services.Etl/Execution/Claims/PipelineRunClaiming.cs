using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// What the run code does before a run starts, for a queued run and a run carried out at once alike (FDW-6939): it reads the pipeline in the run's own scope, works out what the run claims
/// (<see cref="PipelineRunClaims.For"/>), states the host and slot that claim it, and holds the claim in the operational database through <see cref="PipelineRunClaimHold"/>, waiting while another
/// run holds a key. Scoped, because it reads through the run's scope: the pipeline provider and the data gateway are the run's own.
/// </summary>
/// <param name="pipelines">Finds the pipeline the run belongs to.</param>
/// <param name="settings">The effective settings the run limits are read from.</param>
/// <param name="claims">The claim service.</param>
/// <param name="claimSettings">The four settings of the claim.</param>
/// <param name="environment">The host's environment, which names the host.</param>
/// <param name="slot">The slot of the host's copy.</param>
/// <param name="clock">The clock the lease and the waits are measured with.</param>
/// <param name="dataSets">Resolves a data set to the table it writes, or null when the host has none.</param>
/// <param name="dataStores">Resolves a data store, or null when the host has none.</param>
/// <param name="logger">The logger.</param>
public sealed class PipelineRunClaiming(
    IPipelineConfigurationProvider pipelines,
    IEffectiveSettingsProvider settings,
    IPipelineRunClaimService claims,
    PipelineRunClaimSettings claimSettings,
    IHostEnvironment environment,
    ISlotRole slot,
    TimeProvider clock,
    IDataSetConfigurationProvider? dataSets = null,
    IDataStoreProvider? dataStores = null,
    ILogger<PipelineRunClaiming>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<PipelineRunClaiming>.Instance;

    /// <summary>Claims what a run needs and waits until it holds it.</summary>
    /// <param name="request">The run.</param>
    /// <param name="cancellationToken">Cancels the wait; the rows the run left waiting are released and the cancellation reaches the caller.</param>
    /// <returns>The hold, to be disposed when the run ends, or the failure that refuses the run.</returns>
    public async Task<IGenericResult<PipelineRunClaimHold>> Acquire(PipelineExecutionRequest request, CancellationToken cancellationToken = default)
    {
        var limits = PipelineRunLimits.Read(settings, request.TenantId, _logger);
        if (limits.IsFailure || limits.Value is not { } read)
            return limits.ToNewResult<PipelineRunClaimHold>();

        var pipeline = await pipelines.Get(request.PipelineId, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, cancellationToken).ConfigureAwait(false);
        var configuration = pipeline.IsSuccess ? pipeline.Value : null;
        var claim = await PipelineRunClaims.For(request, configuration, new LoadTargetResolver(dataSets, dataStores), read, _logger, cancellationToken).ConfigureAwait(false);
        if (claim.IsFailure || claim.Value is not { } worked)
            return claim.ToNewResult<PipelineRunClaimHold>();

        var made = PipelineRunClaimRequests.Of(request, worked, (configuration as IClaimKeyPipelineConfiguration)?.ClaimKeyTemplate, environment.ApplicationName, slot.SlotName, _logger);
        if (made.IsFailure || made.Value is not { } toClaim)
            return made.ToNewResult<PipelineRunClaimHold>();

        return await PipelineRunClaimHold.Acquire(claims, toClaim, claimSettings, clock, (span, token) => Task.Delay(span, clock, token), _logger, cancellationToken).ConfigureAwait(false);
    }
}
