using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RowQuery.FilterNodes;

/// <summary>The type the record query filter node handler collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class RowQueryNodeHandlerBase : TypeOptionBase<RowQueryNodeHandlerBase>, IRowQueryNodeHandler
{
    /// <summary>Initializes the handler with the name of the filter node kind it handles.</summary>
    /// <param name="name">The filter node kind's name.</param>
    protected RowQueryNodeHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract IGenericResult ValidateOperators(IFilterNode node, ILogger logger);

    /// <inheritdoc/>
    public abstract IGenericResult ValidateColumns(
        IFilterNode node,
        IDataContainer primaryContainer,
        IReadOnlyDictionary<string, IDataContainer> joinedContainers,
        ILogger logger);

    /// <inheritdoc/>
    public abstract bool Matches(
        IFilterNode node,
        IReadOnlyDictionary<string, object?> childRow,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> joinedRows,
        string? primaryContainerName);

    /// <summary>The refusal of a node that is not of the class this handler is named for.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The exception to throw.</returns>
    protected static InvalidOperationException NotOfKind(IFilterNode node) =>
        new($"Unsupported filter node type '{node.GetType().Name}' reached the matcher — RecordQueryValidator should have rejected it first.");
}
