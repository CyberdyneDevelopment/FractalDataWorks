using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>
/// Endpoint that returns an aggregated quality dashboard summary derived from all quality rules.
/// </summary>
public abstract class GetQualityDashboardEndpoint : EndpointWithoutRequest<QualityDashboardResponseDto>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetQualityDashboardEndpoint"/> class.
    /// </summary>
    /// <param name="provider">The configuration provider for quality data.</param>
    protected GetQualityDashboardEndpoint(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datasets:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/quality/dashboard");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Get quality dashboard";
            s.Description = "Returns aggregated quality rule counts for the dashboard.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _provider.GetAllQualityRules(ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to load quality rules for dashboard", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        var rules = result.Value ?? [];

        // Why: the quality rule configuration has no "last run result" field in the current
        // schema — executions are one-shot fire-and-forget. Until execution history is wired,
        // derive passing/failing from IsEnabled state: enabled rules are passing, disabled (non-
        // enabled) ones are counted as failing. This gives a meaningful baseline health metric.
        var passingRules = rules.Count(r => r.IsEnabled);
        var failingRules = rules.Count - passingRules;

        await Send.OkAsync(new QualityDashboardResponseDto
        {
            TotalRules = rules.Count,
            PassingRules = passingRules,
            FailingRules = failingRules,
        }, ct).ConfigureAwait(false);
    }
}
