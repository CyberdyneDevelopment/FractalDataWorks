using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RowQuery.FilterNodes;

/// <summary>A condition node of a record query.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowQueryNodeHandlers), "Condition")]
public sealed class ConditionRowQueryNodeHandler : RowQueryNodeHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="ConditionRowQueryNodeHandler"/> class.</summary>
    public ConditionRowQueryNodeHandler()
        : base("Condition")
    {
    }

    /// <inheritdoc/>
    public override IGenericResult ValidateOperators(IFilterNode node, ILogger logger) =>
        node is FilterCondition typed ? RecordQueryValidator.ValidateConditionOperator(typed, logger) : throw NotOfKind(node);

    /// <inheritdoc/>
    public override IGenericResult ValidateColumns(
        IFilterNode node,
        IDataContainer primaryContainer,
        IReadOnlyDictionary<string, IDataContainer> joinedContainers,
        ILogger logger) =>
        node is FilterCondition typed ? RecordColumnValidator.ValidateCondition(typed, primaryContainer, joinedContainers, logger) : throw NotOfKind(node);

    /// <inheritdoc/>
    public override bool Matches(
        IFilterNode node,
        IReadOnlyDictionary<string, object?> childRow,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> joinedRows,
        string? primaryContainerName) =>
        node is FilterCondition typed ? RecordRowMatcher.EvaluateCondition(childRow, joinedRows, typed, primaryContainerName) : throw NotOfKind(node);
}
