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

namespace Fdw.Services.Calculations.Operations.Conditional;

/// <summary>
/// Returns the first non-null value from an ordered list of field values.
/// Equivalent to SQL <c>COALESCE(field1, field2, ...)</c>.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "Coalesce")]
[ExcludeFromCodeCoverage]
public sealed class CoalesceOperation : CalculationOperationBase
{
    private readonly ILogger<CoalesceOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoalesceOperation"/> class.
    /// </summary>
    public CoalesceOperation()
        : base(id: 41, name: "Coalesce", category: "Conditional", description: "Returns the first non-null value from a list of fields")
    {
        _logger = NullLogger<CoalesceOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Values", Kind = "FieldArray", IsRequired = true, DisplayName = "Value Fields", HelpText = "An ordered list of fields to check for non-null values" }
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
            if (parameters["Values"] is not IEnumerable<object?> values)
            {
                return Task.FromResult(GenericResult<object>.Failure(
                    CalculationOperationLog.ParameterTypeMismatch(_logger, "Values", Name, "IEnumerable<object?>")));
            }

            var valueList = values.ToList();
            var totalFields = valueList.Count;

            for (var i = 0; i < valueList.Count; i++)
            {
                if (valueList[i] is not null && valueList[i] is not DBNull)
                {
                    CalculationOperationLog.CoalesceValueFound(_logger, i, totalFields);
                    CalculationOperationLog.OperationExecutionSucceeded(_logger, Name);
                    return Task.FromResult(GenericResult<object>.Success(valueList[i]!));
                }
            }

            CalculationOperationLog.CoalesceAllNull(_logger, Name, totalFields);
            CalculationOperationLog.OperationExecutionSucceeded(_logger, Name);
            return Task.FromResult(GenericResult<object>.Success((object)DBNull.Value));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<object>.Failure(
                CalculationOperationLog.OperationExecutionFailed(_logger, ex, Name)));
        }
    }
}
