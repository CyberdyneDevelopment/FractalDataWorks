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

namespace Fdw.Services.Calculations.Operations.Arithmetic;

/// <summary>
/// Divides the left field value by the right (left / right).
/// Returns a failure result when the divisor is zero.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "Divide")]
[ExcludeFromCodeCoverage]
public sealed class DivideOperation : CalculationOperationBase
{
    private readonly ILogger<DivideOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DivideOperation"/> class.
    /// </summary>
    public DivideOperation()
        : base(id: 4, name: "Divide", category: "Arithmetic", description: "Divides the left value by the right (left / right)")
    {
        _logger = NullLogger<DivideOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Left", Kind = "Field", IsRequired = true, DisplayName = "Dividend", HelpText = "The value to divide" },
            new OperationParameterDefinition { Name = "Right", Kind = "Field", IsRequired = true, DisplayName = "Divisor", HelpText = "The value to divide by" }
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

            if (right == 0m)
            {
                return Task.FromResult(GenericResult<object>.Failure(
                    CalculationOperationLog.DivisionByZero(_logger, Name)));
            }

            var result = left / right;

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
