using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for listing the window functions a windowed calculation may name.
/// Route: GET /calculations/window-functions
/// </summary>
public abstract class ListWindowFunctionsEndpointBase : EndpointWithoutRequest<WindowFunctionsResponse>
{
    /// <summary>Initializes a new instance of the <see cref="ListWindowFunctionsEndpointBase"/> class.</summary>
    protected ListWindowFunctionsEndpointBase(ILogger logger)
    {
        EndpointLogger = logger;
    }

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/calculations/window-functions");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("calculations:read");
#endif
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (summary, tags, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken ct)
    {
        CalculationEndpointLog.ListingWindowFunctions(EndpointLogger);

        var functions = WindowedCalculationTypes.All()
            .Select(option => new WindowFunctionPayload { Name = option.Name, Category = option.Category })
            .ToList();

        CalculationEndpointLog.ListedWindowFunctions(EndpointLogger, functions.Count);

        return Send.OkAsync(new WindowFunctionsResponse { Functions = functions }, ct);
    }
}
