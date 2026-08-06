using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Abstract endpoint that compares configuration between two environments for a given entity.
/// </summary>
public abstract class CompareEnvironmentsEndpointBase : Endpoint<CompareEnvironmentsRequest, ConfigDiffDto>
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareEnvironmentsEndpointBase"/> class.
    /// </summary>
    /// <param name="promotionService">The promotion service.</param>
    /// <param name="logger">The logger instance.</param>
    protected CompareEnvironmentsEndpointBase(
        IPromotionService promotionService,
        ILogger<CompareEnvironmentsEndpointBase>? logger)
    {
        _promotionService = promotionService;
        _logger = logger ?? NullLogger<CompareEnvironmentsEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/promotion/compare");
        Policies("datasets:read");
        Summary(s => s.Summary = "Compare configuration between two environments");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Compares configuration between the specified environments.</summary>
    public override async Task HandleAsync(CompareEnvironmentsRequest req, CancellationToken ct)
    {
        PromotionEndpointLog.ComparingEnvironments(_logger, req.SourceEnvironment, req.TargetEnvironment, req.EntityType, req.EntityName);

        try
        {
            var result = await _promotionService.CompareEnvironments(
                req.SourceEnvironment,
                req.TargetEnvironment,
                req.EntityType,
                req.EntityName,
                ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                PromotionEndpointLog.CompareEnvironmentsFailed(_logger, req.SourceEnvironment, req.TargetEnvironment, result.CurrentMessage!);
                AddError("Failed to compare environments");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                AddError("Failed to compare environments");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            var diff = result.Value;
            var response = new ConfigDiffDto
            {
                SourceEnvironment = diff.SourceEnvironment,
                TargetEnvironment = diff.TargetEnvironment,
                EntityType = diff.EntityType,
                EntityName = diff.EntityName,
                Differences = diff.Differences.Select(d => new ConfigDiffItemDto
                {
                    PropertyPath = d.PropertyPath,
                    SourceValue = d.SourceValue?.ToString(),
                    TargetValue = d.TargetValue?.ToString(),
                    DiffType = d.DiffType
                }).ToList()
            };

            PromotionEndpointLog.EnvironmentsCompared(_logger, response.Differences.Count, req.SourceEnvironment, req.TargetEnvironment);
            await Send.OkAsync(response, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PromotionEndpointLog.CompareEnvironmentsFailed(_logger, req.SourceEnvironment, req.TargetEnvironment, ex.Message);
            AddError("Failed to compare environments");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
