using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Reads one threshold by name, with its rules (FDW-3948).</summary>
public abstract class GetThresholdEndpointBase : CrudGetEndpointBase<ThresholdNameRequest, ThresholdPayload>
{
    private readonly IThresholdConfigurationProvider _thresholds;

    /// <summary>Initializes a new instance of the <see cref="GetThresholdEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="thresholds">The thresholds.</param>
    protected GetThresholdEndpointBase(ILogger<GetThresholdEndpointBase> logger, IThresholdConfigurationProvider thresholds)
        : base(logger)
    {
        _thresholds = thresholds;
    }

    /// <inheritdoc />
    protected override string ResourceName => "thresholds";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(ThresholdNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<ThresholdPayload?>> FindByIdentifier(ThresholdNameRequest request, CancellationToken ct) =>
        ThresholdReads.Find(_thresholds, request.Name, ct);
}
