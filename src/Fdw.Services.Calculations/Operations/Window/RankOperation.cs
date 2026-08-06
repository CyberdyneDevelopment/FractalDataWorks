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
/// Assigns a rank within a partition, with gaps for ties.
/// Equivalent to SQL <c>RANK() OVER (PARTITION BY ... ORDER BY ...)</c>.
/// </summary>
[TypeOption(typeof(CalculationOperationTypes), "Rank")]
[ExcludeFromCodeCoverage]
public sealed class RankOperation : CalculationOperationBase
{
    private readonly ILogger<RankOperation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RankOperation"/> class.
    /// </summary>
    public RankOperation()
        : base(id: 31, name: "Rank", category: "Window", description: "Assigns a rank within a partition, with gaps for ties")
    {
        _logger = NullLogger<RankOperation>.Instance;
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

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Function"] = "Rank",
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
