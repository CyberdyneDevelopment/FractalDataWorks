using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Endpoint that retrieves a single quality rule by its identifier.</summary>
public abstract class GetQualityRuleEndpointBase : Endpoint<QualityRuleIdRequest, QualityRuleDto>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="GetQualityRuleEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected GetQualityRuleEndpointBase(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datasets:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/quality/rules/{Id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s => s.Summary = "Get quality rule by ID");
    }

    /// <summary>Retrieves a quality rule by its identifier, returning 404 if not found.</summary>
    public override async Task HandleAsync(QualityRuleIdRequest req, CancellationToken ct)
    {
        var result = await _provider.GetQualityRule(req.Id, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to get quality rule", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(MapToDto(result.Value), ct).ConfigureAwait(false);
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
