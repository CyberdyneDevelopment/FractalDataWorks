using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data.Abstractions.Visualization;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for computing statistical summaries (StatSet) for specified columns.
/// </summary>
public abstract class ComputeStatSetEndpointBase : Endpoint<StatSetRequest, StatSetResponse>
{
    private readonly IStatSetService _statSetService;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeStatSetEndpointBase"/> class.
    /// </summary>
    protected ComputeStatSetEndpointBase(IStatSetService statSetService)
    {
        _statSetService = statSetService;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/data-preview/statset");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("authenticated");
#endif
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (summary, tags, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(StatSetRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        CalculationEndpointLog.ExecutingCalculation(EndpointLogger, "statset", "ComputeStatSet");

        var result = await _statSetService.ComputeStatSet(req, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            CalculationEndpointLog.ValidationFailed(EndpointLogger, result.CurrentMessage ?? "Failed to compute statistics");
            AddError("Failed to compute statistics");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
    }
}
