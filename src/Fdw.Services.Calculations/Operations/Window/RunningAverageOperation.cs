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
/// Computes a running average over a field within a partition.
/// Equivalent to SQL <c>AVG(field) OVER (PARTITION BY ... ORDER BY ... ROWS UNBOUNDED PRECEDING)</c>.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "RunningAverage")]
[ExcludeFromCodeCoverage]
public sealed class RunningAverageOperation : CalculationOperationBase
{
    private readonly ILogger<RunningAverageOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunningAverageOperation"/> class.
    /// </summary>
    public RunningAverageOperation()
        : base(id: 32, name: "RunningAverage", category: "Window", description: "Computes a running average over a field within a partition")
    {
        _logger = NullLogger<RunningAverageOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Field", Kind = "Field", IsRequired = true, DisplayName = "Value Field", HelpText = "The field to compute the running average over" },
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
            var field = parameters["Field"]?.ToString() ?? string.Empty;
            var partitionFields = parameters.TryGetValue("PartitionBy", out var pb) && pb is IEnumerable<string> pf
                ? pf.ToList()
                : [];
            var orderFields = parameters.TryGetValue("OrderBy", out var ob) && ob is IEnumerable<string> of
                ? of.ToList()
                : [];

            CalculationOperationLog.WindowOperationStarted(_logger, Name, partitionFields.Count, orderFields.Count);

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Function"] = "RunningAverage",
                ["Field"] = field,
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
