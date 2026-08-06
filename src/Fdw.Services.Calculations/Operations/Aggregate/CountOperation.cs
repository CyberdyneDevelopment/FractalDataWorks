using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
/// Counts the number of values in a field, optionally grouped by one or more fields.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "Count")]
[ExcludeFromCodeCoverage]
public sealed class CountOperation : CalculationOperationBase
{
    private readonly ILogger<CountOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountOperation"/> class.
    /// </summary>
    public CountOperation()
        : base(id: 14, name: "Count", category: "Aggregate", description: "Counts the number of values in a field")
    {
        _logger = NullLogger<CountOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Values", Kind = "Field", IsRequired = true, DisplayName = "Values Field", HelpText = "The field containing values to count" },
            new OperationParameterDefinition { Name = "GroupBy", Kind = "FieldArray", IsRequired = false, DisplayName = "Group By Fields", HelpText = "Optional fields to group the count by" }
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

            var count = values.Count();

            CalculationOperationLog.AggregateProcessing(_logger, Name, count);
            CalculationOperationLog.OperationExecutionSucceeded(_logger, Name);
            return Task.FromResult(GenericResult<object>.Success((object)count));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<object>.Failure(
                CalculationOperationLog.OperationExecutionFailed(_logger, ex, Name)));
        }
    }
}
