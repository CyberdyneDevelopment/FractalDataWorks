using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Endpoint that deletes an existing quality rule.</summary>
public abstract class DeleteQualityRuleEndpointBase : Endpoint<QualityRuleIdRequest>
{
    private readonly IQualityRuleConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="DeleteQualityRuleEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected DeleteQualityRuleEndpointBase(IQualityRuleConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required to delete a quality rule.</summary>
    protected virtual string DeletePolicy => "quality/rules:delete";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Delete("/quality/rules/{Id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(DeletePolicy);
#endif
        Summary(s => s.Summary = "Delete a quality rule");
    }

    /// <summary>Deletes the quality rule identified by the request ID and returns 204 No Content on success.</summary>
    public override async Task HandleAsync(QualityRuleIdRequest req, CancellationToken ct)
    {
        var result = await _provider.Delete(req.Id, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to delete quality rule", Details = result.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
