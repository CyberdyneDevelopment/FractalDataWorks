using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RowQuery.FilterNodes;

/// <summary>Checks and evaluates one kind of filter node of a record query; the collection is keyed by the node kind's name (<c>FilterNodeKinds</c>).</summary>
public interface IRowQueryNodeHandler : ITypeOption<int, IRowQueryNodeHandler>
{
    /// <summary>Checks that the operators the node uses are the ones a record query supports.</summary>
    /// <param name="node">The node, of the kind this handler is named for.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>Success, or the failure naming the operator a record query does not support.</returns>
    IGenericResult ValidateOperators(IFilterNode node, ILogger logger);

    /// <summary>Checks that every column the node names is declared by the container it belongs to.</summary>
    /// <param name="node">The node, of the kind this handler is named for.</param>
    /// <param name="primaryContainer">The container the query reads.</param>
    /// <param name="joinedContainers">The containers the joins brought in, by name.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>Success, or the failure naming the column.</returns>
    IGenericResult ValidateColumns(
        IFilterNode node,
        IDataContainer primaryContainer,
        IReadOnlyDictionary<string, IDataContainer> joinedContainers,
        ILogger logger);

    /// <summary>Whether the rows satisfy the node.</summary>
    /// <param name="node">The node, of the kind this handler is named for.</param>
    /// <param name="childRow">The row of the queried container.</param>
    /// <param name="joinedRows">The rows the joins brought in, by container name.</param>
    /// <param name="primaryContainerName">The name of the queried container, when a column may be qualified with it.</param>
    /// <returns><see langword="true"/> when the rows satisfy the node.</returns>
    bool Matches(
        IFilterNode node,
        IReadOnlyDictionary<string, object?> childRow,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> joinedRows,
        string? primaryContainerName);
}
