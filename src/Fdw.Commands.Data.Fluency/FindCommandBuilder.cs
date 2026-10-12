using System.Collections.Generic;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Fluent builder for <see cref="FindCommand{T}"/> to eliminate boilerplate and provide a clean API
/// for constructing cross-field search commands.
/// The terminal method <see cref="Build"/> returns <see cref="IGenericResult{DataGatewayCall}"/> so
/// validation failures are surfaced as structured results rather than exceptions.
/// </summary>
/// <typeparam name="T">The result type from the search.</typeparam>
/// <example>
/// <code>
/// var result = Find.In&lt;Customer&gt;("CRM", "sales", "Customers")
///     .Search("acme")
///     .InFields("Name", "Description", "Email")
///     .CaseSensitive(false)
///     .MaxResults(100)
///     .Build();
/// if (!result.IsSuccess) return result.ToNewResult&lt;...&gt;();
/// var call = result.Value;
/// </code>
/// </example>
public class FindCommandBuilder<T>
{
    private readonly string? _dataStoreName;
    private readonly string? _pathName;
    private readonly string? _containerName;
    private string? _searchTerm;
    private IReadOnlyList<string>? _fieldNames;
    private bool _caseSensitive;
    private int? _maxResults;

    /// <summary>
    /// Initializes a new instance of the <see cref="FindCommandBuilder{T}"/> class with full path specification.
    /// </summary>
    /// <param name="dataStoreName">The DataStore name for container resolution.</param>
    /// <param name="pathName">The path within the DataStore (e.g., schema name).</param>
    /// <param name="containerName">The name of the container (table/endpoint) to search.</param>
    public FindCommandBuilder(string? dataStoreName, string? pathName, string? containerName)
    {
        _dataStoreName = dataStoreName;
        _pathName = pathName;
        _containerName = containerName;
    }

    /// <summary>
    /// Sets the search term to find across fields.
    /// </summary>
    /// <param name="searchTerm">The value to search for.</param>
    /// <returns>This builder for chaining.</returns>
    public FindCommandBuilder<T> Search(string? searchTerm)
    {
        _searchTerm = searchTerm;
        return this;
    }

    /// <summary>
    /// Restricts the search to the specified field names.
    /// When not called, all string fields are searched.
    /// </summary>
    /// <param name="fieldNames">The field names to search within.</param>
    /// <returns>This builder for chaining.</returns>
    public FindCommandBuilder<T> InFields(params string[]? fieldNames)
    {
        _fieldNames = fieldNames;
        return this;
    }

    /// <summary>
    /// Sets whether the search should be case-sensitive.
    /// Defaults to false (case-insensitive) if not called.
    /// </summary>
    /// <param name="caseSensitive">True for case-sensitive search; false for case-insensitive.</param>
    /// <returns>This builder for chaining.</returns>
    public FindCommandBuilder<T> CaseSensitive(bool caseSensitive = true)
    {
        _caseSensitive = caseSensitive;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of results to return.
    /// When not called, the system default limit is applied.
    /// </summary>
    /// <param name="maxResults">The maximum number of results.</param>
    /// <returns>This builder for chaining.</returns>
    public FindCommandBuilder<T> MaxResults(int maxResults)
    {
        _maxResults = maxResults;
        return this;
    }

    /// <summary>
    /// Builds and returns a <see cref="DataGatewayCall"/> bundling the find command with its
    /// <see cref="DataStoreTarget"/>. Validates all required fields and returns a failure result
    /// if any are missing.
    /// </summary>
    /// <returns>A result containing the configured call, or a failure with validation details.</returns>
    public IGenericResult<DataGatewayCall> Build()
    {
        if (string.IsNullOrWhiteSpace(_dataStoreName))
        {
            return GenericResult<DataGatewayCall>.Failure(
                GenericMessage.Create(MessageSeverity.Error, "DataStoreName is required for FindCommand", "FDW-FIND-001", "FindCommandBuilder"));
        }

        if (string.IsNullOrWhiteSpace(_pathName))
        {
            return GenericResult<DataGatewayCall>.Failure(
                GenericMessage.Create(MessageSeverity.Error, "PathName is required for FindCommand", "FDW-FIND-002", "FindCommandBuilder"));
        }

        if (string.IsNullOrWhiteSpace(_containerName))
        {
            return GenericResult<DataGatewayCall>.Failure(
                GenericMessage.Create(MessageSeverity.Error, "ContainerName is required for FindCommand", "FDW-FIND-003", "FindCommandBuilder"));
        }

        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
            return GenericResult<DataGatewayCall>.Failure(
                GenericMessage.Create(MessageSeverity.Error, "SearchTerm is required for FindCommand — call Search() before Build()", "FDW-FIND-004", "FindCommandBuilder"));
        }

        var command = new FindCommand<T>
        {
            SearchTerm = _searchTerm,
            FieldNames = _fieldNames,
            CaseSensitive = _caseSensitive,
            MaxResults = _maxResults
        };

        return GenericResult<DataGatewayCall>.Success(
            new DataGatewayCall(command, new DataStoreTarget(_dataStoreName, _pathName, _containerName)));
    }
}
