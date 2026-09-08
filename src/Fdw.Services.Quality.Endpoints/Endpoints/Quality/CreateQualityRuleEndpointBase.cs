using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using System.Linq;
using Fdw.Services.Quality.Abstractions.TypeCollections.QualityRuleTypeOptions;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Endpoint that creates a new quality rule.</summary>
public abstract class CreateQualityRuleEndpointBase : Endpoint<CreateQualityRuleRequest, QualityRuleDto>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="CreateQualityRuleEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected CreateQualityRuleEndpointBase(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for write operations.</summary>
    protected virtual string ReadPolicy => "datasets:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/quality/rules");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s => s.Summary = "Create a quality rule");
    }

    /// <summary>Creates a new quality rule and returns the created resource with a 201 status.</summary>
    public override async Task HandleAsync(CreateQualityRuleRequest req, CancellationToken ct)
    {
        // A rule with no data set or no rule type can never match anything, and both columns
        // are NOT NULL in the database -- which an empty string satisfies, so storage does not
        // catch it either. Rejected here rather than stored: the previous behaviour returned 201
        // for an empty body and created a rule that was ENABLED by default and unrunnable.
        if (string.IsNullOrWhiteSpace(req.DataSetName)
            || string.IsNullOrWhiteSpace(req.RuleType)
            || ReferenceEquals(QualityRuleTypes.ByName(req.RuleType), QualityRuleTypes.NotFound))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(
                new
                {
                    Error = "Quality rule rejected",
                    // The valid types are named rather than left for the caller to guess: a
                    // rule stored with an unrecognised type sits in the list looking correct and
                    // never runs, which is the failure this check exists to prevent.
                    Details = "dataSetName and ruleType are both required, and ruleType must be a "
                              + "registered quality rule type. Valid types: "
                              + string.Join(", ", QualityRuleTypes.All().Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal))
                              + ".",
                }, ct).ConfigureAwait(false);
            return;
        }

        var config = QualityConfigurationProvider.MapQualityRuleFromRequest(
            req.DataSetName, req.FieldName, req.RuleType, req.Severity,
            req.IsEnabled, req.Description, req.MinValue, req.MaxValue,
            req.Pattern, req.Expression, req.Name);

        var result = await _provider.SaveQualityRule(config, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to create quality rule", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        var savedConfig = result.Value!;

        await Send.CreatedAtAsync<GetQualityRuleEndpointBase>(
            new { Id = savedConfig.Id },
            new QualityRuleDto
            {
                Id = savedConfig.Id,
                DataSetName = savedConfig.DataSetName,
                FieldName = savedConfig.FieldName,
                RuleType = savedConfig.RuleType,
                Severity = savedConfig.Severity,
                IsEnabled = savedConfig.IsEnabled,
                Description = savedConfig.Description,
                MinValue = savedConfig.MinValue,
                MaxValue = savedConfig.MaxValue,
                Pattern = savedConfig.Pattern,
                Expression = savedConfig.Expression,
                // Read from the saved row, not invented here: the previous UtcNow was a value the
                // response asserted and the record never held, so create and read disagreed.
                CreatedAt = savedConfig.CreateDate
            },
            cancellation: ct).ConfigureAwait(false);
    }
}
