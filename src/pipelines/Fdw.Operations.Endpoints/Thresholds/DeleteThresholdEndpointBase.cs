using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Deletes a threshold by name (FDW-3948). A threshold that is attached to a subject stays until it is detached, and the refusal says so.</summary>
public abstract class DeleteThresholdEndpointBase : CrudDeleteEndpointBase<ThresholdNameRequest>
{
    private readonly IThresholdConfigurationProvider _thresholds;

    /// <summary>Initializes a new instance of the <see cref="DeleteThresholdEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="thresholds">The thresholds.</param>
    protected DeleteThresholdEndpointBase(ILogger<DeleteThresholdEndpointBase> logger, IThresholdConfigurationProvider thresholds)
        : base(logger)
    {
        _thresholds = thresholds;
    }

    /// <inheritdoc />
    protected override string ResourceName => "thresholds";

    /// <inheritdoc />
    protected override string DeletePolicy => "thresholds:write";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(ThresholdNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(ThresholdNameRequest request, CancellationToken ct)
    {
        var existing = await _thresholds.Get(request.Name, ct).ConfigureAwait(false);
        return existing.IsFailure ? existing.ToNewResult<bool>() : GenericResult<bool>.Success(existing.Value is not null);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> Delete(ThresholdNameRequest request, CancellationToken ct)
    {
        ThresholdEndpointLog.Deleting(Logger, request.Name);
        return _thresholds.Delete(request.Name, ct);
    }
}
