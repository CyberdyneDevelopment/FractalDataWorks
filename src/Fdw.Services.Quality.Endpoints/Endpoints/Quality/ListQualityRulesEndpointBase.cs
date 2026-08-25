using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Endpoint that lists quality rules, optionally filtered by DataSet name.</summary>
public abstract class ListQualityRulesEndpointBase : Endpoint<DataSetQueryRequest, List<QualityRuleDto>>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="ListQualityRulesEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected ListQualityRulesEndpointBase(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used in the endpoint route.</summary>
    protected virtual string ResourceName => "quality/rules";

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datasets:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get($"/{ResourceName}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "List quality rules";
            s.Description = "Returns all quality rules, optionally filtered by DataSet name.";
        });
    }

    /// <summary>Retrieves all quality rules, optionally filtering by DataSet name when provided.</summary>
    public override async Task HandleAsync(DataSetQueryRequest req, CancellationToken ct)
    {
        var result = await _provider.GetAllQualityRules(ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to list quality rules", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        IEnumerable<QualityRuleConfiguration> rules = result.Value ?? [];

        // Why: Apply optional DataSet name filter here — DefaultConfigurationProvider has no
        // per-field filter overload. Quality rule counts per DataSet are small enough that
        // in-memory filtering is acceptable.
        if (!string.IsNullOrWhiteSpace(req.DataSetName))
        {
            rules = rules.Where(r =>
                string.Equals(r.DataSetName, req.DataSetName, StringComparison.OrdinalIgnoreCase));
        }

        await Send.OkAsync(rules.Select(MapToDto).ToList(), ct).ConfigureAwait(false);
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
