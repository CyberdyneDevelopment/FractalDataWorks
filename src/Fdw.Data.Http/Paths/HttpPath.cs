using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Data.Http.Results;
using Fdw.Results;
using IDataNodePath = Fdw.Data.DataStores.Abstractions.IDataPath;

namespace Fdw.Data.Http.Paths;

/// <summary>
/// Represents a path to an HTTP endpoint.
/// Format: /segment/segment/{parameter} (e.g., "/api/v1/customers/{id}")
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires HTTP connections
public sealed class HttpPath : PathBase, IDataPath<IStorageContainer>
{
    private readonly List<IStorageContainer> _containers;
    private readonly Dictionary<string, PathParameter> _parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpPath"/> class.
    /// </summary>
    /// <param name="path">The HTTP path (e.g., "/api/customers").</param>
    /// <param name="containers">Optional containers at this path.</param>
    /// <param name="parameters">Optional path parameters.</param>
    public HttpPath(
        string path,
        IEnumerable<IStorageContainer>? containers = null,
        IReadOnlyDictionary<string, PathParameter>? parameters = null)
        : base(2, "HttpPath")
    {
        PathValue = path ?? throw new ArgumentNullException(nameof(path));
        _containers = containers?.ToList() ?? new List<IStorageContainer>();
        _parameters = parameters != null
            ? parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal)
            : new Dictionary<string, PathParameter>(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public override string PathValue { get; }

    /// <inheritdoc/>
    public override string Domain => "Http";

    // IDataNodePath implementation
    string IDataNodePath.Id => PathValue;
    string IDataNodePath.Name => PathValue.Split('/').Last();
    string IDataNodePath.PathType => "HttpPath";
    string IDataNodePath.FullPath => PathValue;
    IReadOnlyList<string> IDataNodePath.Segments => PathValue.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
    IReadOnlyDictionary<string, PathParameter> IDataNodePath.Parameters => _parameters;
    IReadOnlyDictionary<string, object> IDataNodePath.Metadata => new Dictionary<string, object>(StringComparer.Ordinal);
    bool IDataNodePath.RequiresParameters => _parameters.Values.Any(p => p.IsRequired);

    /// <inheritdoc/>
    public IReadOnlyList<IStorageContainer> Containers => _containers;

    /// <inheritdoc/>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public IStorageContainer? GetContainer(string name)
    {
        return _containers.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public bool ContainsContainer(string name)
    {
        return _containers.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    IDataNodePath IDataNodePath.ResolveParameters(IDictionary<string, object> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return this;

        var resolvedPath = PathValue;
        foreach (var kvp in parameters)
        {
            resolvedPath = resolvedPath.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? string.Empty);
        }

        return new HttpPath(resolvedPath, _containers, _parameters);
    }

    IGenericResult IDataNodePath.ValidateParameters(IDictionary<string, object> parameters)
    {
        if (!((IDataNodePath)this).RequiresParameters)
            return GenericResult.Success();

        foreach (var param in _parameters.Values.Where(p => p.IsRequired))
        {
            if (parameters == null || !parameters.ContainsKey(param.Name))
            {
                return GenericResult.Failure(
                    DataHttpResultCodes.ByName("RequiredParameterMissing"),
                    ResultDetails.Create().With("ParameterName", param.Name));
            }
        }

        return GenericResult.Success();
    }

    IDataNodePath? IDataNodePath.GetParent() => null;
    IEnumerable<IDataNodePath> IDataNodePath.GetChildren() => Enumerable.Empty<IDataNodePath>();
    IDataNodePath IDataNodePath.Combine(string relativePath) =>
        new HttpPath($"{PathValue.TrimEnd('/')}/{relativePath.TrimStart('/')}", _containers, _parameters);
}
