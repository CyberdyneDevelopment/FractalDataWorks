using System;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Commands.Data.Extensions;

/// <summary>
/// Builder for single-entity insert commands.
/// The terminal method <see cref="Value"/> returns a <see cref="DataGatewayCall"/> that bundles
/// the address-free command with its <see cref="DataStoreTarget"/>.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class InsertSingleBuilder<T>
{
    private readonly string _containerName;
    private string? _dataStoreName;
    private string? _pathName;
    private string[]? _returnColumns;

    /// <summary>
    /// Initializes a new instance of the <see cref="InsertSingleBuilder{T}"/> class.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    public InsertSingleBuilder(string containerName)
    {
        _containerName = containerName;
    }

    /// <summary>
    /// Specifies the DataStore name for container resolution (required).
    /// </summary>
    /// <param name="dataStoreName">The DataStore name.</param>
    /// <returns>The builder for method chaining.</returns>
    public InsertSingleBuilder<T> DataStore(string dataStoreName)
    {
        _dataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        return this;
    }

    /// <summary>
    /// Specifies the path name within the DataStore (e.g., schema name) (required).
    /// </summary>
    /// <param name="pathName">The path name.</param>
    /// <returns>The builder for method chaining.</returns>
    public InsertSingleBuilder<T> Path(string pathName)
    {
        _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
        return this;
    }

    /// <summary>
    /// Names the columns to return for each row written (a generated key, a column default). The call then answers
    /// <c>IReadOnlyList&lt;IReadOnlyDictionary&lt;string, object?&gt;&gt;</c>, one dictionary for each row, instead of a row count.
    /// </summary>
    /// <param name="columns">The column names; each must be declared by the container.</param>
    /// <returns>This builder.</returns>
    public InsertSingleBuilder<T> Returning(params string[] columns)
    {
        _returnColumns = columns ?? throw new ArgumentNullException(nameof(columns));
        return this;
    }

    /// <summary>
    /// Builds and returns a <see cref="DataGatewayCall"/> containing the insert command
    /// and its <see cref="DataStoreTarget"/> address.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <exception cref="InvalidOperationException">Thrown when DataStore or Path is not specified.</exception>
    public DataGatewayCall Value(T entity)
    {
        if (string.IsNullOrWhiteSpace(_dataStoreName))
            throw new InvalidOperationException("DataStore must be specified. Call DataStore() before Value().");
        if (string.IsNullOrWhiteSpace(_pathName))
            throw new InvalidOperationException("Path must be specified. Call Path() before Value().");

        return new DataGatewayCall(
            new InsertCommand<T>(entity) { ReturnColumns = _returnColumns },
            new DataStoreTarget(_dataStoreName, _pathName, _containerName));
    }
}
