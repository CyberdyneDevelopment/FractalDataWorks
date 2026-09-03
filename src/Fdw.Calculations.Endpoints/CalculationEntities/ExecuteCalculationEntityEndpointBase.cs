using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Calculations;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for executing a calculation entity.
/// Route: POST /calculation-entities/{id}/execute
/// </summary>
public abstract class ExecuteCalculationEntityEndpointBase : Endpoint<ExecuteCalculationEntityRequest, ExecuteCalculationEntityResponse>
{
    private readonly ICalculationEntityService _service;
    private readonly IDataGatewayProvider _dataGateways;

    /// <summary>
    /// Initializes a new instance of <see cref="ExecuteCalculationEntityEndpointBase"/>.
    /// </summary>
    protected ExecuteCalculationEntityEndpointBase(ICalculationEntityService service, IDataGatewayProvider dataGateways)
    {
        _service = service;
        _dataGateways = dataGateways;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("calculation-entities/{Id}/execute");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("calculation-entities:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Execute a calculation entity";
            s.Description = "Executes the specified calculation entity and returns the result.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ExecuteCalculationEntityRequest req, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        var entityResult = await _service.GetCalculationById(req.Id, ct).ConfigureAwait(false);
        if (entityResult.IsFailure)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var entity = entityResult.Value!;
        var context = CreateExecutionContext();

        var result = await _service.ExecuteCalculation(entity.Name, context, ct).ConfigureAwait(false);

        stopwatch.Stop();

        if (result.IsFailure)
        {
            AddError(result.Messages?.ToString() ?? "Calculation execution failed");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new ExecuteCalculationEntityResponse
        {
            CalculationName = entity.Name,
            ResultJson = result.Value ?? string.Empty,
            DurationMs = stopwatch.ElapsedMilliseconds
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the execution context for the calculation. Override to provide custom context.
    /// </summary>
    protected virtual ICalculationContext CreateExecutionContext() => new CalculationContext(_dataGateways);
}
