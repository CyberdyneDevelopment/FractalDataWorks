using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A concrete <see cref="IPath"/> for a discovered container: a schema/object pair plus the
/// connection domain it was discovered in.
/// </summary>
/// <remarks>
/// Why public here (not a per-connector internal): every connection adapter's discovery produced the
/// same shape — only the <see cref="Domain"/> literal differed. Each adapter previously kept an
/// identical internal <c>SimplePath</c>; this single public type in the shared abstractions package
/// replaces them, with the domain supplied by the caller instead of hardcoded.
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: produced only against a live connection during discovery.
public sealed class SimplePath : IPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimplePath"/> class.
    /// </summary>
    /// <param name="pathValue">The string representation of the path.</param>
    /// <param name="domain">The connection domain this path belongs to (e.g. "Sql", "PostgreSql").</param>
    /// <param name="schemaName">The schema name segment.</param>
    /// <param name="objectName">The object (table/view) name segment.</param>
    public SimplePath(string pathValue, string domain, string schemaName, string objectName)
    {
        PathValue = pathValue;
        Domain = domain;
        SchemaName = schemaName;
        ObjectName = objectName;
    }

    /// <inheritdoc/>
    public string PathValue { get; }

    /// <inheritdoc/>
    public string Domain { get; }

    /// <summary>
    /// Gets the schema name segment.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    /// Gets the object name segment.
    /// </summary>
    public string ObjectName { get; }
}
