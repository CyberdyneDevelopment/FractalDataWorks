using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations.Operations.Aggregate;

/// <summary>
/// Computes the average (mean) of a set of field values, optionally grouped by one or more fields.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "Average")]
[ExcludeFromCodeCoverage]
public sealed class AverageOperation : CalculationOperationBase
{
    private readonly ILogger<AverageOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AverageOperation"/> class.
    /// </summary>
    public AverageOperation()
        : base(id: 11, name: "Average", category: "Aggregate", description: "Computes the average of a set of values")
    {
        _logger = NullLogger<AverageOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Values", Kind = "Field", IsRequired = true, DisplayName = "Values Field", HelpText = "The field containing numeric values to average" },
            new OperationParameterDefinition { Name = "GroupBy", Kind = "FieldArray", IsRequired = false, DisplayName = "Group By Fields", HelpText = "Optional fields to group the aggregation by" }
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
            if (parameters["Values"] is not IEnumerable<object> values)
            {
                return Task.FromResult(GenericResult<object>.Failure(
                    CalculationOperationLog.ParameterTypeMismatch(_logger, "Values", Name, "IEnumerable<object>")));
            }

            var decimalValues = values
                .Select(v => Convert.ToDecimal(v, CultureInfo.InvariantCulture))
                .ToList();

            CalculationOperationLog.AggregateProcessing(_logger, Name, decimalValues.Count);

            if (decimalValues.Count == 0)
            {
                CalculationOperationLog.AggregateEmptyValues(_logger, Name);
                return Task.FromResult(GenericResult<object>.Success((object)0m));
            }

            var result = decimalValues.Average();

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
