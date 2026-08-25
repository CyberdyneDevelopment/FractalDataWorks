using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Results;

namespace Fdw.Data.Files.Paths;

/// <summary>
/// Represents a path to a file or file pattern.
/// Format: /path/to/file.ext or /path/to/*.csv
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires filesystem access
public sealed class FilePath : PathBase, IDataPath<IStorageContainer>
{
    private readonly List<IStorageContainer> _containers;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilePath"/> class.
    /// </summary>
    /// <param name="path">The file path or pattern.</param>
    /// <param name="containers">Optional containers at this path.</param>
    public FilePath(
        string path,
        IEnumerable<IStorageContainer>? containers = null)
        : base(3, "FilePath")
    {
        PathValue = path ?? throw new ArgumentNullException(nameof(path));
        _containers = containers?.ToList() ?? new List<IStorageContainer>();
        IsPattern = path.Contains('*') || path.Contains('?');
    }

    /// <inheritdoc/>
    public override string PathValue { get; }

    /// <inheritdoc/>
    public override string Domain => "File";

    /// <summary>
    /// Gets a value indicating whether this is a file pattern (contains wildcards).
    /// </summary>
    public bool IsPattern { get; }

    /// <summary>
    /// Gets the directory path.
    /// </summary>
    public string Directory => Path.GetDirectoryName(PathValue) ?? string.Empty;

    /// <summary>
    /// Gets the file name (or pattern).
    /// </summary>
    public string FileName => Path.GetFileName(PathValue);

    /// <summary>
    /// Gets the file extension (if not a pattern).
    /// </summary>
    public string Extension => IsPattern ? string.Empty : Path.GetExtension(PathValue);

    // IDataNodePath implementation — using fully qualified type to resolve ambiguity with
    // Fdw.Data.Abstractions.IDataNodePath (Phase 1 DataNodes addition)
    string Fdw.Data.DataStores.Abstractions.IDataPath.Id => PathValue;
    string Fdw.Data.DataStores.Abstractions.IDataPath.Name => FileName;
    string Fdw.Data.DataStores.Abstractions.IDataPath.PathType => "FilePath";
    string Fdw.Data.DataStores.Abstractions.IDataPath.FullPath => PathValue;
    IReadOnlyList<string> Fdw.Data.DataStores.Abstractions.IDataPath.Segments => PathValue.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    IReadOnlyDictionary<string, PathParameter> Fdw.Data.DataStores.Abstractions.IDataPath.Parameters => new Dictionary<string, PathParameter>(StringComparer.Ordinal);
    IReadOnlyDictionary<string, object> Fdw.Data.DataStores.Abstractions.IDataPath.Metadata => new Dictionary<string, object>(StringComparer.Ordinal);
    bool Fdw.Data.DataStores.Abstractions.IDataPath.RequiresParameters => false;

    /// <inheritdoc/>
    public IReadOnlyList<IStorageContainer> Containers => _containers;

    /// <inheritdoc/>
    public IStorageContainer? GetContainer(string name) =>
        _containers.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc/>
    public bool ContainsContainer(string name) =>
        _containers.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    Fdw.Data.DataStores.Abstractions.IDataPath Fdw.Data.DataStores.Abstractions.IDataPath.ResolveParameters(IDictionary<string, object> parameters) => this;
    IGenericResult Fdw.Data.DataStores.Abstractions.IDataPath.ValidateParameters(IDictionary<string, object> parameters) => GenericResult.Success();
    Fdw.Data.DataStores.Abstractions.IDataPath? Fdw.Data.DataStores.Abstractions.IDataPath.GetParent() => null;
    IEnumerable<Fdw.Data.DataStores.Abstractions.IDataPath> Fdw.Data.DataStores.Abstractions.IDataPath.GetChildren() => Enumerable.Empty<Fdw.Data.DataStores.Abstractions.IDataPath>();
    Fdw.Data.DataStores.Abstractions.IDataPath Fdw.Data.DataStores.Abstractions.IDataPath.Combine(string relativePath) =>
        new FilePath(Path.Combine(PathValue, relativePath), _containers);
}
