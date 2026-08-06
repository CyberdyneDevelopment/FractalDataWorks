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
/// Adds two field values together (left + right).
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "Add")]
[ExcludeFromCodeCoverage]
public sealed class AddOperation : CalculationOperationBase
{
    private readonly ILogger<AddOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddOperation"/> class.
    /// </summary>
    public AddOperation()
        : base(id: 1, name: "Add", category: "Arithmetic", description: "Adds two values (left + right)")
    {
        _logger = NullLogger<AddOperation>.Instance;
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
            var result = left + right;

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
