using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Detaches a threshold from a subject (FDW-3948). The link is retired; the threshold and its rules stay.</summary>
public abstract class DetachThresholdEndpointBase : Endpoint<ThresholdSubjectRequest>
{
    private readonly IThresholdLinks _links;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DetachThresholdEndpointBase"/> class.</summary>
    /// <param name="links">The threshold links.</param>
    /// <param name="logger">The logger.</param>
    protected DetachThresholdEndpointBase(IThresholdLinks links, ILogger<DetachThresholdEndpointBase>? logger)
    {
        _links = links;
        _logger = logger ?? NullLogger<DetachThresholdEndpointBase>.Instance;
    }

    /// <summary>Gets the policy a caller holds to change what is attached.</summary>
    protected virtual string WritePolicy => "thresholds:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/thresholds/{Name}/subjects/{Kind}/{SubjectId}");
        Policies(WritePolicy);
        Description(x => x.WithTags("thresholds"));
        Summary(s => s.Summary = "Detach a threshold from a subject");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ThresholdSubjectRequest req, CancellationToken ct)
    {
        ThresholdEndpointLog.Detaching(_logger, req.Name, req.Kind, req.SubjectId.ToString("D"));
        var result = await _links.Detach(req.Kind, req.SubjectId, req.Name, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
