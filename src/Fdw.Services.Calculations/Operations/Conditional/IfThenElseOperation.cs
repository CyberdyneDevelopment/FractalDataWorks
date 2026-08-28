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

namespace Fdw.Services.Calculations.Operations.Conditional;

/// <summary>
/// Evaluates a boolean condition field and returns the ThenValue when truthy or
/// the ElseValue when falsy. Equivalent to SQL <c>CASE WHEN condition THEN ... ELSE ... END</c>.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "IfThenElse")]
[ExcludeFromCodeCoverage]
public sealed class IfThenElseOperation : CalculationOperationBase
{
    private readonly ILogger<IfThenElseOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IfThenElseOperation"/> class.
    /// </summary>
    public IfThenElseOperation()
        : base(id: 40, name: "IfThenElse", category: "Conditional", description: "Returns ThenValue when condition is true, ElseValue otherwise")
    {
        _logger = NullLogger<IfThenElseOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Condition", Kind = "Field", IsRequired = true, DisplayName = "Condition", HelpText = "A boolean field value to evaluate" },
            new OperationParameterDefinition { Name = "ThenValue", Kind = "Scalar", IsRequired = true, DisplayName = "Then Value", HelpText = "The value to return when the condition is true" },
            new OperationParameterDefinition { Name = "ElseValue", Kind = "Scalar", IsRequired = true, DisplayName = "Else Value", HelpText = "The value to return when the condition is false" }
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
            CalculationOperationLog.ConditionalEvaluating(_logger, Name);

            var conditionValue = parameters["Condition"];
            var isTruthy = IsTruthy(conditionValue);

            var branch = isTruthy ? "Then" : "Else";
            CalculationOperationLog.ConditionalResolved(_logger, Name, branch);

            var result = isTruthy ? parameters["ThenValue"] : parameters["ElseValue"];

            CalculationOperationLog.OperationExecutionSucceeded(_logger, Name);
            return Task.FromResult(GenericResult<object>.Success(result!));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<object>.Failure(
                CalculationOperationLog.OperationExecutionFailed(_logger, ex, Name)));
        }
    }

    private static bool IsTruthy(object? value)
    {
        if (value is null)
        {
            return false;
        }

        if (value is bool b)
        {
            return b;
        }

        if (value is string s)
        {
            return !string.IsNullOrWhiteSpace(s)
                && !string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(s, "0", StringComparison.Ordinal);
        }

        try
        {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture) != 0m;
        }
        catch (FormatException ex)
        {
            _ = ex;
            return true;
        }
        catch (OverflowException ex)
        {
            _ = ex;
            return true;
        }
        catch (InvalidCastException ex)
        {
            _ = ex;
            return true;
        }
    }
}
