using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RowQuery.FilterNodes;

/// <summary>A group node of a record query.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowQueryNodeHandlers), "Group")]
public sealed class GroupRowQueryNodeHandler : RowQueryNodeHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="GroupRowQueryNodeHandler"/> class.</summary>
    public GroupRowQueryNodeHandler()
        : base("Group")
    {
    }

    /// <inheritdoc/>
    public override IGenericResult ValidateOperators(IFilterNode node, ILogger logger) =>
        node is FilterGroup typed ? RecordQueryValidator.ValidateGroupOperators(typed, logger) : throw NotOfKind(node);

    /// <inheritdoc/>
    public override IGenericResult ValidateColumns(
        IFilterNode node,
        IDataContainer primaryContainer,
        IReadOnlyDictionary<string, IDataContainer> joinedContainers,
        ILogger logger) =>
        node is FilterGroup typed ? RecordColumnValidator.ValidateGroupColumns(typed, primaryContainer, joinedContainers, logger) : throw NotOfKind(node);

    /// <inheritdoc/>
    public override bool Matches(
        IFilterNode node,
        IReadOnlyDictionary<string, object?> childRow,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> joinedRows,
        string? primaryContainerName) =>
        node is FilterGroup typed ? RecordRowMatcher.EvaluateGroup(childRow, joinedRows, typed, primaryContainerName) : throw NotOfKind(node);
}
