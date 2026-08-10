using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Endpoint that updates an existing quality rule.</summary>
public abstract class UpdateQualityRuleEndpoint : Endpoint<UpdateQualityRuleRequest, QualityRuleDto>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="UpdateQualityRuleEndpoint"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected UpdateQualityRuleEndpoint(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for write operations.</summary>
    protected virtual string WritePolicy => "datasets:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        // Why: the client (QualityApiClient.UpdateRule) PUTs here; there was previously no server route (404).
        Put("/quality/rules/{Id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s => s.Summary = "Update a quality rule");
    }

    /// <summary>Loads the rule, applies the provided changes, and persists, returning 404 if not found.</summary>
    public override async Task HandleAsync(UpdateQualityRuleRequest req, CancellationToken ct)
    {
        var existing = await _provider.GetQualityRule(req.Id, ct).ConfigureAwait(false);
        if (!existing.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to load quality rule", Details = existing.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        if (existing.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var config = existing.Value;
        if (!string.IsNullOrWhiteSpace(req.Name)) config.Name = req.Name;
        if (req.Description is not null) config.Description = req.Description;
        if (req.Expression is not null) config.Expression = req.Expression;
        config.IsEnabled = req.IsEnabled;

        var result = await _provider.SaveQualityRule(config, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to update quality rule", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(MapToDto(result.Value!), ct).ConfigureAwait(false);
    }

    /// <summary>Maps a QualityRuleConfiguration to its corresponding DTO.</summary>
    protected virtual QualityRuleDto MapToDto(QualityRuleConfiguration config)
    {
        return new QualityRuleDto
        {
            Id = config.Id,
            DataSetName = config.DataSetName,
            FieldName = config.FieldName,
            RuleType = config.RuleType,
            Severity = config.Severity,
            IsEnabled = config.IsEnabled,
            Description = config.Description,
            MinValue = config.MinValue,
            MaxValue = config.MaxValue,
            Pattern = config.Pattern,
            Expression = config.Expression
        };
    }
}
