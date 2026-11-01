using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base for the endpoint that evaluates a Built transformation with values for its inputs and says what it yields, so a person
/// building a filter sees what a calculation comes to right now (FDW-2333).
/// </summary>
/// <remarks>
/// The same service evaluates a calculation a filter names, so the value shown here is the value a query would use. The response
/// carries the value and its kind; a calculation that does not exist, is not a single value, lacks an input or is given one it does
/// not declare is refused with the reason. Evaluating changes nothing.
/// </remarks>
public abstract class EvaluateTransformationEndpointBase : Endpoint<EvaluateTransformationRequest, EvaluateTransformationResponse>
{
    private readonly IFilterCalculations _calculations;

    /// <summary>Initializes a new instance of the <see cref="EvaluateTransformationEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="calculations">Evaluates a Built transformation with values for its inputs.</param>
    protected EvaluateTransformationEndpointBase(ILogger<EvaluateTransformationEndpointBase> logger, IFilterCalculations calculations)
    {
        Logger = logger ?? NullLogger<EvaluateTransformationEndpointBase>.Instance;
        _calculations = calculations ?? throw new ArgumentNullException(nameof(calculations));
    }

    /// <summary>Gets the logger.</summary>
    protected new ILogger Logger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/transformations/{Name}/evaluate");
        Policies("datasets:read");
        Tags("Transformations");
        Summary(s =>
        {
            s.Summary = "Evaluate a Built transformation";
            s.Description =
                "Evaluates a Built transformation that keeps one result, with a value for each input it declares, and answers the value " +
                "and its kind (Number or DateTime). The body is an object with an inputs member (an empty object for none). An unknown " +
                "name, a missing input or an input of the wrong type is refused with the reason.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(EvaluateTransformationRequest req, CancellationToken ct)
    {
        var inputs = Inputs(req);
        if (inputs.IsFailure)
        {
            await SendFailure(inputs).ConfigureAwait(false);
            return;
        }

        var evaluated = await _calculations.Evaluate(req.Name, inputs.Value!, ct).ConfigureAwait(false);
        if (evaluated.IsFailure)
        {
            await SendFailure(evaluated).ConfigureAwait(false);
            return;
        }

        var kind = CalculationValueKinds.OfClrType(evaluated.Value!.ValueType);
        await Send.OkAsync(
            new EvaluateTransformationResponse { Value = evaluated.Value.Value, ValueType = kind.Name, DataType = kind.DataTypeName }, ct)
            .ConfigureAwait(false);
    }

    private async Task SendFailure(IGenericResult failure)
    {
        var (statusCode, problem) = ResultHttpStatusMapper.Map(failure, HttpContext);
        await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
    }

    // A Number input takes a JSON number and a Flag input true or false; the calculation checks each against what it declares.
    private IGenericResult<IReadOnlyDictionary<string, object?>> Inputs(EvaluateTransformationRequest req)
    {
        var values = new Dictionary<string, object?>(req.Inputs.Count, StringComparer.Ordinal);
        foreach (var input in req.Inputs)
        {
#pragma warning disable FDW018 // External System.Text.Json JsonValueKind enum — cannot convert to TypeCollection
            switch (input.Value.ValueKind)
            {
                case JsonValueKind.Number when input.Value.TryGetDecimal(out var number):
                    values[input.Key] = number;
                    break;
                case JsonValueKind.True:
                    values[input.Key] = true;
                    break;
                case JsonValueKind.False:
                    values[input.Key] = false;
                    break;
                default:
                    return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(
                        FilterCalculationLog.InputWrongType(Logger, req.Name, input.Key, "Number or Flag", input.Value.ValueKind.ToString()));
            }
#pragma warning restore FDW018
        }

        return GenericResult<IReadOnlyDictionary<string, object?>>.Success(values);
    }
}
