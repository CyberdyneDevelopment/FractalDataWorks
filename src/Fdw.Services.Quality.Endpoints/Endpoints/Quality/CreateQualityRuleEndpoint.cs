using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Endpoint that creates a new quality rule.</summary>
public abstract class CreateQualityRuleEndpoint : Endpoint<CreateQualityRuleRequest, QualityRuleDto>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="CreateQualityRuleEndpoint"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected CreateQualityRuleEndpoint(QualityConfigurationProvider provider)
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

        await Send.CreatedAtAsync<GetQualityRuleEndpoint>(
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
                CreatedAt = DateTimeOffset.UtcNow
            },
            cancellation: ct).ConfigureAwait(false);
    }
}
