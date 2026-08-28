using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations.Operations.Geometry;

/// <summary>
/// Computes the Euclidean length of a vector via the Pythagorean theorem:
/// <c>sqrt(A^2 + B^2 + C^2)</c>. The third leg (C) is optional, so the operation covers both the
/// planar (two-leg) and spatial (three-leg) cases.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "Pythagorean")]
[ExcludeFromCodeCoverage]
public sealed class PythagoreanOperation : CalculationOperationBase
{
    private readonly ILogger<PythagoreanOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PythagoreanOperation"/> class.
    /// </summary>
    public PythagoreanOperation()
        : base(id: 50, name: "Pythagorean", category: "Geometry", description: "Euclidean length sqrt(A^2 + B^2 + C^2); C optional")
    {
        _logger = NullLogger<PythagoreanOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "A", Kind = "Field", IsRequired = true, DisplayName = "Leg A", HelpText = "First leg value" },
            new OperationParameterDefinition { Name = "B", Kind = "Field", IsRequired = true, DisplayName = "Leg B", HelpText = "Second leg value" },
            new OperationParameterDefinition { Name = "C", Kind = "Field", IsRequired = false, DisplayName = "Leg C", HelpText = "Optional third leg value (spatial case)" }
        ];
    }

    /// <inheritdoc />
    public override Task<IGenericResult<object>> Calculate(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        CalculationOperationLog.OperationExecutionStarted(_logger, Name, Category);

        try
        {
            var a = Convert.ToDouble(parameters["A"], CultureInfo.InvariantCulture);
            var b = Convert.ToDouble(parameters["B"], CultureInfo.InvariantCulture);
            var sumOfSquares = (a * a) + (b * b);

            if (parameters.TryGetValue("C", out var cRaw) && cRaw is not null)
            {
                var c = Convert.ToDouble(cRaw, CultureInfo.InvariantCulture);
                sumOfSquares += c * c;
            }

            var result = Math.Sqrt(sumOfSquares);

            CalculationOperationLog.OperationExecutionSucceeded(_logger, Name);
            return Task.FromResult(GenericResult<object>.Success((object)result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<object>.Failure(
                CalculationOperationLog.OperationExecutionFailed(_logger, ex, Name)));
        }
    }
}
