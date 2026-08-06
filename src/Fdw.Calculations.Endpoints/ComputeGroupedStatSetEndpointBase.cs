using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data.Abstractions.Visualization;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for computing grouped statistical summaries (StatSet) with dimensions.
/// </summary>
public abstract class ComputeGroupedStatSetEndpointBase : Endpoint<GroupedStatSetRequest, GroupedStatSetResponse>
{
    private readonly IStatSetService _statSetService;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeGroupedStatSetEndpointBase"/> class.
    /// </summary>
    protected ComputeGroupedStatSetEndpointBase(IStatSetService statSetService)
    {
        _statSetService = statSetService;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/data-preview/statset/grouped");
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
    public override async Task HandleAsync(GroupedStatSetRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        CalculationEndpointLog.ExecutingCalculation(EndpointLogger, "statset-grouped", "ComputeGroupedStatSet");

        var result = await _statSetService.ComputeGroupedStatSet(req, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            CalculationEndpointLog.ValidationFailed(EndpointLogger, result.CurrentMessage ?? "Failed to compute grouped statistics");
            AddError("Failed to compute grouped statistics");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
    }
}
