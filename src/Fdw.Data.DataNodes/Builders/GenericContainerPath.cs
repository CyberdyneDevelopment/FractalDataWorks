using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Builders;

/// <summary>
/// Minimal <see cref="IPath"/> for a generic (non-SQL) container's physical address. Its
/// <see cref="PathValue"/> is the request path the transport reads (for HTTP, the URL path the
/// protocol's <c>GetRequestPath</c> returns via <c>container.Path.PathValue</c>).
/// </summary>
/// <remarks>
/// Why: a generic container (Http/file) has no structured database-style physical path. The builder
/// sets this from the owning <c>DataPath</c>'s name (which carries the request path), preserving the
/// pre-redesign behaviour where the generic container projected <c>Path.Name</c> as its physical URL.
/// </remarks>
internal sealed class GenericContainerPath : IPath
{
    internal GenericContainerPath(string pathValue) => PathValue = pathValue;

    /// <inheritdoc />
    public string PathValue { get; }

    /// <inheritdoc />
    public string Domain => "Generic";
}
