using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Attaches a threshold to a subject (FDW-3948) of the kind the threshold judges. The subject is named by its kind and its Id in its domain table. A threshold attaches to a subject once.</summary>
public abstract class AttachThresholdEndpointBase : Endpoint<ThresholdSubjectRequest>
{
    private readonly IThresholdLinks _links;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="AttachThresholdEndpointBase"/> class.</summary>
    /// <param name="links">The threshold links.</param>
    /// <param name="logger">The logger.</param>
    protected AttachThresholdEndpointBase(IThresholdLinks links, ILogger<AttachThresholdEndpointBase>? logger)
    {
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _logger = logger ?? NullLogger<AttachThresholdEndpointBase>.Instance;
    }

    /// <summary>Gets the policy a caller holds to change what is attached.</summary>
    protected virtual string WritePolicy => "thresholds:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/thresholds/{Name}/subjects/{Kind}/{SubjectId}");
        Policies(WritePolicy);
        Description(x => x.WithTags("thresholds"));
        Summary(s => s.Summary = "Attach a threshold to a subject");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ThresholdSubjectRequest req, CancellationToken ct)
    {
        ThresholdEndpointLog.Attaching(_logger, req.Name, req.Kind, req.SubjectId.ToString("D"));
        var result = await _links.Attach(req.Kind, req.SubjectId, req.Name, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
