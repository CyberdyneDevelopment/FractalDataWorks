using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Endpoint that deletes an existing quality rule.</summary>
public abstract class DeleteQualityRuleEndpoint : Endpoint<QualityRuleIdRequest>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="DeleteQualityRuleEndpoint"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected DeleteQualityRuleEndpoint(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for write operations.</summary>
    protected virtual string ReadPolicy => "datasets:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Delete("/quality/rules/{Id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s => s.Summary = "Delete a quality rule");
    }

    /// <summary>Deletes the quality rule identified by the request ID and returns 204 No Content on success.</summary>
    public override async Task HandleAsync(QualityRuleIdRequest req, CancellationToken ct)
    {
        var result = await _provider.DeleteQualityRule(req.Id, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to delete quality rule", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
