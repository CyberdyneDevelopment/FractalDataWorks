using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Endpoint that updates an existing quality rule.</summary>
public abstract class UpdateQualityRuleEndpointBase : Endpoint<UpdateQualityRuleRequest, QualityRuleDto>
{
    private readonly IQualityRuleConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="UpdateQualityRuleEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected UpdateQualityRuleEndpointBase(IQualityRuleConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required to change a quality rule.</summary>
    protected virtual string WritePolicy => "quality/rules:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Patch("/quality/rules/{Id}");
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
        var existing = await _provider.Get(req.Id, ct).ConfigureAwait(false);
        if (!existing.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to load quality rule", Details = existing.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        if (existing.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var config = existing.Value;
        // No Name: the column does not exist, so assigning it changed nothing on the way out.
        if (req.Description is not null) config.Description = req.Description;
        if (req.Expression is not null) config.Expression = req.Expression;
        if (req.IsEnabled is not null) config.IsEnabled = req.IsEnabled.Value;

        var result = await _provider.Update(config, "QualityRule", "QualityRule", config.Name, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to update quality rule", Details = result.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(MapToDto(config), ct).ConfigureAwait(false);
    }

    /// <summary>Maps a QualityRuleImplementationConfiguration to its corresponding DTO.</summary>
    protected virtual QualityRuleDto MapToDto(IQualityRuleImplementationConfiguration config)
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
            Expression = config.Expression,
            CreatedAt = config.CreateDate
        };
    }
}
