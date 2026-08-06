using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Data.FileSystem;

/// <summary>
/// The native FileSystem command that reads records from a configured file container through the
/// config-driven record source factory. Carries the source <c>QueryCommand</c>'s filter/join,
/// copied through unchanged, so the connection can apply them over the decoded rows via the shared
/// <c>RecordQueryEvaluator</c>.
/// </summary>
public sealed class FileSystemReadCommand : FileSystemRecordCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemReadCommand"/> class.
    /// </summary>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="container">The configured container to read.</param>
    /// <param name="filter">The filter expression (WHERE clause), copied through unchanged from the source command.</param>
    /// <param name="joins">The join expressions, copied through unchanged from the source command.</param>
    public FileSystemReadCommand(
        string relativePath,
        IDataContainer container,
        IFilterExpression? filter,
        IReadOnlyList<IJoinExpression> joins)
        : base(relativePath, container)
    {
        Filter = filter;
        Joins = joins;
    }

    /// <summary>Gets the filter expression (WHERE clause), copied through unchanged from the source command.</summary>
    public IFilterExpression? Filter { get; }

    /// <summary>Gets the join expressions, copied through unchanged from the source command.</summary>
    public IReadOnlyList<IJoinExpression> Joins { get; }
}
