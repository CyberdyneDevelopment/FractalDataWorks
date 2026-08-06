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

namespace Fdw.Services.Calculations.Operations.Window;

/// <summary>
/// Accesses a field value from a preceding row within a partition.
/// Equivalent to SQL <c>LAG(field, offset) OVER (PARTITION BY ... ORDER BY ...)</c>.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "Lag")]
[ExcludeFromCodeCoverage]
public sealed class LagOperation : CalculationOperationBase
{
    private readonly ILogger<LagOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LagOperation"/> class.
    /// </summary>
    public LagOperation()
        : base(id: 34, name: "Lag", category: "Window", description: "Accesses a field value from a preceding row within a partition")
    {
        _logger = NullLogger<LagOperation>.Instance;
        Parameters =
        [
            new OperationParameterDefinition { Name = "Field", Kind = "Field", IsRequired = true, DisplayName = "Value Field", HelpText = "The field to look behind on" },
            new OperationParameterDefinition { Name = "Offset", Kind = "Scalar", IsRequired = true, DisplayName = "Offset", HelpText = "Number of rows to look behind (default 1)" },
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
            var offset = Convert.ToInt32(parameters["Offset"], CultureInfo.InvariantCulture);
            var partitionFields = parameters.TryGetValue("PartitionBy", out var pb) && pb is IEnumerable<string> pf
                ? pf.ToList()
                : [];
            var orderFields = parameters.TryGetValue("OrderBy", out var ob) && ob is IEnumerable<string> of
                ? of.ToList()
                : [];

            CalculationOperationLog.WindowOperationStarted(_logger, Name, partitionFields.Count, orderFields.Count);

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Function"] = "Lag",
                ["Field"] = field,
                ["Offset"] = offset,
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
