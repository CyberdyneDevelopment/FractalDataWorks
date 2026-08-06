using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Runtime;

/// <summary>
/// Concrete runtime implementation of <see cref="IDataSet"/>.
/// Built by <see cref="DataSetFactory"/> from a <c>DataSetConfiguration</c>.
/// </summary>
public sealed class DataSet : IDataSet
{
    private readonly ILogger<DataSet> _logger;

    // Why: fields are declared in-memory at construction (from DataSetConfiguration). They feed
    // both the async GetFields contract and the synchronous Field(name) lookup.
    private readonly IReadOnlyList<IDataField> _fields;

    /// <summary>
    /// Initializes a new <see cref="DataSet"/> with all required runtime members.
    /// </summary>
    /// <param name="name">The unique name of the dataset.</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <param name="composition">The composition strategy for this dataset.</param>
    /// <param name="sources">The data source nodes that supply rows.</param>
    /// <param name="joins">The join definitions (empty for Singular/Union compositions).</param>
    /// <param name="fields">The field definitions for this dataset.</param>
    /// <param name="keys">The key definitions for this dataset.</param>
    /// <param name="logger">Optional logger instance.</param>
    public DataSet(
        string name,
        string? description,
        IDataSetCompositionType composition,
        IReadOnlyList<IDataSetSource> sources,
        IReadOnlyList<IDataSetJoin> joins,
        IReadOnlyList<IDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        ILogger<DataSet>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(joins);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(keys);

        Name = name;
        Description = description;
        Composition = composition;
        Sources = sources;
        Joins = joins;
        _fields = fields;
        Keys = keys;
        _logger = logger ?? NullLogger<DataSet>.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IDataSetCompositionType Composition { get; }

    /// <inheritdoc />
    public IReadOnlyList<IDataSetSource> Sources { get; }

    /// <inheritdoc />
    public IReadOnlyList<IDataSetJoin> Joins { get; }

    /// <inheritdoc />
    // Why: a dataset's child IDataNode set is its fields (the same set Field(name) looks up over).
    // IReadOnlyList<IDataField> is covariant to IReadOnlyList<IDataNode>.
    public IReadOnlyList<IDataNode> Nodes => _fields;

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var field = _fields.FirstOrDefault(f =>
            string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

        if (field is null)
            return GenericResult<IDataNode>.Failure(
                DataSetProviderLog.FieldNotFoundInDataSet(_logger, name, Name));

        return GenericResult<IDataNode>.Success(field);
    }

    /// <summary>Gets the fields declared on this dataset (async contract retained for resolvers).</summary>
    // Why: GetFields/Keys are no longer on IDataSet (the uniform IDataNode surface replaced the
    // node-level async fields + Keys), but resolvers still call GetFields to materialise the
    // dataset's field list. Kept as plain instance members, not interface overrides.
    public Task<IReadOnlyList<IDataField>> GetFields(CancellationToken cancellationToken = default)
        => Task.FromResult(_fields);

    /// <summary>Gets the key definitions for this dataset.</summary>
    public IReadOnlyList<IContainerKey> Keys { get; }

    /// <inheritdoc />
    public IGenericResult<IDataField> Field(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var field = _fields.FirstOrDefault(f =>
            string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

        if (field is null)
        {
            // Why: Log-and-return pattern — single call both logs and returns the message.
            return GenericResult<IDataField>.Failure(
                DataSetProviderLog.FieldNotFoundInDataSet(_logger, name, Name));
        }

        return GenericResult<IDataField>.Success(field);
    }
}
