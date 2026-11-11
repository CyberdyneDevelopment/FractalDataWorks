using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Thresholds;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>
/// Replaces a threshold (FDW-3948): its description, whether it is judged, and the whole list of its rules. The request states all of them; a rule it leaves out is gone. A threshold
/// keeps its type: a threshold of another type is another threshold.
/// </summary>
public abstract class UpdateThresholdEndpointBase : CrudUpdateEndpointBase<ThresholdPayload, ThresholdPayload>
{
    private readonly IThresholdConfigurationProvider _thresholds;

    /// <summary>Initializes a new instance of the <see cref="UpdateThresholdEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="thresholds">The thresholds.</param>
    protected UpdateThresholdEndpointBase(ILogger<UpdateThresholdEndpointBase> logger, IThresholdConfigurationProvider thresholds)
        : base(logger)
    {
        _thresholds = thresholds;
    }

    /// <inheritdoc />
    protected override string ResourceName => "thresholds";


    /// <inheritdoc />
    protected override string EndpointSummary => "Replace a threshold";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(ThresholdPayload request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<ThresholdPayload?>> FindForUpdate(ThresholdPayload request, CancellationToken ct) =>
        ThresholdReads.Find(_thresholds, request.Name, ct);

    /// <inheritdoc />
    protected override async Task<IGenericResult<ThresholdPayload>> Update(ThresholdPayload request, ThresholdPayload existing, CancellationToken ct)
    {
        if (!string.Equals(request.Type, existing.Type, StringComparison.Ordinal))
        {
            return ThresholdRefusal.Of<ThresholdPayload>(request.Name, $"its type is '{existing.Type}' and cannot become '{request.Type}'; make another threshold");
        }

        var stored = await _thresholds.Get(request.Name, ct).ConfigureAwait(false);
        if (stored.IsFailure || stored.Value is null)
        {
            return stored.IsFailure
                ? stored.ToNewResult<ThresholdPayload>()
                : ThresholdRefusal.Of<ThresholdPayload>(request.Name, "it was deleted while the request was handled");
        }

        var built = ThresholdMapper.ToConfiguration(request, stored.Value.Id);
        if (built.IsFailure || built.Value is null)
        {
            return built.ToNewResult<ThresholdPayload>();
        }

        var judged = ThresholdDefinitions.From(built.Value);
        if (judged.IsFailure)
        {
            return judged.ToNewResult<ThresholdPayload>();
        }

        ThresholdEndpointLog.Replacing(Logger, request.Name, request.Rules.Count);
        var saved = await _thresholds.Update(built.Value, ThresholdDomain.Name, request.Type, request.Name, ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<ThresholdPayload>()
            : await ThresholdReads.Read(_thresholds, request.Name, Logger, ct).ConfigureAwait(false);
    }
}
