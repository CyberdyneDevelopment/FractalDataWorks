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

namespace Fdw.Services.Calculations.Operations.Window;

/// <summary>
/// Assigns a sequential row number within a partition, ordered by the specified fields.
/// Equivalent to SQL <c>ROW_NUMBER() OVER (PARTITION BY ... ORDER BY ...)</c>.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "RowNumber")]
[ExcludeFromCodeCoverage]
public sealed class RowNumberOperation : CalculationOperationBase
{
    private readonly ILogger<RowNumberOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RowNumberOperation"/> class.
    /// </summary>
    public RowNumberOperation()
        : base(id: 30, name: "RowNumber", category: "Window", description: "Assigns a sequential row number within a partition")
    {
        _logger = NullLogger<RowNumberOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "PartitionBy", Kind = "FieldArray", IsRequired = false, DisplayName = "Partition By", HelpText = "Optional fields to partition the window by" },
            new OperationParameterDefinition { Name = "OrderBy", Kind = "FieldArray", IsRequired = true, DisplayName = "Order By", HelpText = "Fields that define the ordering within each partition" }
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
            var partitionFields = parameters.TryGetValue("PartitionBy", out var pb) && pb is IEnumerable<string> pf
                ? pf.ToList()
                : [];
            var orderFields = parameters.TryGetValue("OrderBy", out var ob) && ob is IEnumerable<string> of
                ? of.ToList()
                : [];

            CalculationOperationLog.WindowOperationStarted(_logger, Name, partitionFields.Count, orderFields.Count);

            // Row number assignment is a placeholder — the actual windowing happens
            // when the step executor applies this operation across a data set.
            // This method returns the operation metadata for the executor to use.
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Function"] = "RowNumber",
                ["PartitionBy"] = partitionFields,
                ["OrderBy"] = orderFields
            };

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
