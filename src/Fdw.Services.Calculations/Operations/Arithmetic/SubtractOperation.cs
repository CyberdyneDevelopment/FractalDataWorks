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
/// Subtracts the right field value from the left (left - right).
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "Subtract")]
[ExcludeFromCodeCoverage]
public sealed class SubtractOperation : CalculationOperationBase
{
    private readonly ILogger<SubtractOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtractOperation"/> class.
    /// </summary>
    public SubtractOperation()
        : base(id: 2, name: "Subtract", category: "Arithmetic", description: "Subtracts the right value from the left (left - right)")
    {
        _logger = NullLogger<SubtractOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Left", Kind = "Field", IsRequired = true, DisplayName = "Left Operand", HelpText = "The left-hand field value" },
            new OperationParameterDefinition { Name = "Right", Kind = "Field", IsRequired = true, DisplayName = "Right Operand", HelpText = "The right-hand field value" }
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
            var result = left - right;

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
