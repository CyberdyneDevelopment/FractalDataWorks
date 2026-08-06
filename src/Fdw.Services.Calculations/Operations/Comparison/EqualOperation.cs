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
/// Compares whether the left field value is equal to the right scalar value.
/// Returns <see langword="true"/> or <see langword="false"/> as the result.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "Equal")]
[ExcludeFromCodeCoverage]
public sealed class EqualOperation : CalculationOperationBase
{
    private readonly ILogger<EqualOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EqualOperation"/> class.
    /// </summary>
    public EqualOperation()
        : base(id: 22, name: "Equal", category: "Comparison", description: "Returns true if the left value equals the right value")
    {
        _logger = NullLogger<EqualOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Left", Kind = "Field", IsRequired = true, DisplayName = "Left Operand", HelpText = "The field value to compare" },
            new OperationParameterDefinition { Name = "Right", Kind = "Scalar", IsRequired = true, DisplayName = "Right Operand", HelpText = "The scalar value to compare against" }
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
            var result = left == right;

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
