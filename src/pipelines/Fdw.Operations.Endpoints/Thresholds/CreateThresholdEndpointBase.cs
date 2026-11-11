using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Thresholds;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>
/// Creates a threshold (FDW-3948) of a type, with its rules. What cannot be judged is refused before anything is stored: a rule that names a value the type does not report, an
/// escalation type that does not exist, a consequence without the policy or notification rules it needs, or criteria that do not parse.
/// </summary>
public abstract class CreateThresholdEndpointBase : CrudCreateEndpointBase<ThresholdPayload, ThresholdPayload>
{
    private readonly IThresholdConfigurationProvider _thresholds;

    /// <summary>Initializes a new instance of the <see cref="CreateThresholdEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="thresholds">The thresholds.</param>
    protected CreateThresholdEndpointBase(ILogger<CreateThresholdEndpointBase> logger, IThresholdConfigurationProvider thresholds)
        : base(logger)
    {
        _thresholds = thresholds;
    }

    /// <inheritdoc />
    protected override string ResourceName => "thresholds";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create a threshold";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Creates a threshold of a type from GET /thresholds/types, with rules judged in the order given. A rule is criteria over the values the type reports, the escalation type that fires when they hold (GET /thresholds/escalation-types), and the escalation policy or notification rules that escalation needs. A threshold is attached to a subject separately.";

    /// <inheritdoc />
    protected override string GetResourceName(ThresholdPayload request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(ThresholdPayload request, CancellationToken ct)
    {
        var existing = await _thresholds.Get(request.Name, ct).ConfigureAwait(false);
        return existing.IsFailure ? existing.ToNewResult<bool>() : GenericResult<bool>.Success(existing.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<ThresholdPayload>> Create(ThresholdPayload request, CancellationToken ct)
    {
        var built = ThresholdMapper.ToConfiguration(request, Guid.CreateVersion7());
        if (built.IsFailure || built.Value is null)
        {
            return built.ToNewResult<ThresholdPayload>();
        }

        var judged = ThresholdDefinitions.From(built.Value);
        if (judged.IsFailure)
        {
            return judged.ToNewResult<ThresholdPayload>();
        }

        ThresholdEndpointLog.Creating(Logger, request.Type, request.Name, request.Rules.Count);
        var saved = await _thresholds.Save(built.Value, ThresholdDomain.Name, request.Type, request.Name, ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<ThresholdPayload>()
            : await ThresholdReads.Read(_thresholds, request.Name, Logger, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(ThresholdPayload detail, CancellationToken ct) => Send.ResponseAsync(detail, 201, ct);
}
