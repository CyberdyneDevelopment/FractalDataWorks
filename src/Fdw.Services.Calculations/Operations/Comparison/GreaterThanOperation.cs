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

namespace Fdw.Services.Calculations.Operations.Comparison;

/// <summary>
/// Compares whether the left field value is greater than the right scalar value.
/// Returns <see langword="true"/> or <see langword="false"/> as the result.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "GreaterThan")]
[ExcludeFromCodeCoverage]
public sealed class GreaterThanOperation : CalculationOperationBase
{
    private readonly ILogger<GreaterThanOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOperation"/> class.
    /// </summary>
    public GreaterThanOperation()
        : base(id: 20, name: "GreaterThan", category: "Comparison", description: "Returns true if the left value is greater than the right value")
    {
        _logger = NullLogger<GreaterThanOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Left", Kind = "Field", IsRequired = true, DisplayName = "Left Operand", HelpText = "The field value to compare" },
            new OperationParameterDefinition { Name = "Right", Kind = "Scalar", IsRequired = true, DisplayName = "Right Operand", HelpText = "The scalar threshold value" }
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
            var left = Convert.ToDecimal(parameters["Left"], CultureInfo.InvariantCulture);
            var right = Convert.ToDecimal(parameters["Right"], CultureInfo.InvariantCulture);
            var result = left > right;

            CalculationOperationLog.OperationExecutionSucceeded(_logger, Name);
            return Task.FromResult(GenericResult<object>.Success(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<object>.Failure(
                CalculationOperationLog.OperationExecutionFailed(_logger, ex, Name)));
        }
    }
}
