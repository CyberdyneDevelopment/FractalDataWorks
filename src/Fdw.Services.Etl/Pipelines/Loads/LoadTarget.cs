using System;
using Fdw.Commands.Data.Loads;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Where a load writes, or what an insert-from-query reads: a resolved container and the connection that serves it (FDW-1109).
/// </summary>
public sealed class LoadTarget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadTarget"/> class.
    /// </summary>
    /// <param name="description">The data set or container this resolves, for messages.</param>
    /// <param name="container">The resolved container.</param>
    /// <param name="connectionId">The connection of the container's data store.</param>
    public LoadTarget(string description, IDataContainer container, Guid connectionId)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Container = container ?? throw new ArgumentNullException(nameof(container));
        ConnectionId = connectionId;
    }

    /// <summary>Gets the data set or container this resolves, for messages.</summary>
    public string Description { get; }

    /// <summary>Gets the resolved container.</summary>
    public IDataContainer Container { get; }

    /// <summary>Gets the connection of the container's data store.</summary>
    public Guid ConnectionId { get; }

    /// <summary>Describes the container as the source of a set-based load.</summary>
    /// <param name="owner">What the load is, for the refusal message.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="filter">The filter that restricts the source rows, or <see langword="null"/> for all of them.</param>
    /// <returns>The table, schema and database, or a failure when the container is not stored in a table.</returns>
    public IGenericResult<QuerySource> AsQuerySource(string owner, ILogger logger, IFilterExpression? filter = null)
        => Container.Path is IDatabasePath path
            ? GenericResult<QuerySource>.Success(new QuerySource(path.ObjectName, path.Schema, path.Database) { Filter = filter })
            : GenericResult<QuerySource>.Failure(PipelineRunLog.SourceNotTableLike(logger, owner, Description));
}
