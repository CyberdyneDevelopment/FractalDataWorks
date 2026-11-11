using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Lists the thresholds attached to a subject (FDW-3948), enabled or not, each as a list shows it.</summary>
public abstract class GetSubjectThresholdsEndpointBase : Endpoint<SubjectThresholdsRequest, ThresholdSummaryResponse[]>
{
    private readonly IThresholdLinks _links;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="GetSubjectThresholdsEndpointBase"/> class.</summary>
    /// <param name="links">The threshold links.</param>
    /// <param name="logger">The logger.</param>
    protected GetSubjectThresholdsEndpointBase(IThresholdLinks links, ILogger<GetSubjectThresholdsEndpointBase>? logger)
    {
        _links = links;
        _logger = logger ?? NullLogger<GetSubjectThresholdsEndpointBase>.Instance;
    }

    /// <summary>Gets the policy a caller holds to read what is attached.</summary>
    protected virtual string ReadPolicy => "thresholds:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/subjects/{Kind}/{SubjectId}/thresholds");
        Policies(ReadPolicy);
        Description(x => x.WithTags("thresholds"));
        Summary(s => s.Summary = "List the thresholds attached to a subject");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(SubjectThresholdsRequest req, CancellationToken ct)
    {
        ThresholdEndpointLog.ReadingSubject(_logger, req.Kind, req.SubjectId.ToString("D"));
        var result = await _links.ThresholdsOf(req.Kind, req.SubjectId, ct).ConfigureAwait(false);
        if (result.IsFailure || result.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync([.. result.Value.Select(ThresholdMapper.ToSummary)], ct).ConfigureAwait(false);
    }
}
