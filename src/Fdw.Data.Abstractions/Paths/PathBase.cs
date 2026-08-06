using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for path implementations.
/// </summary>
public abstract class PathBase : TypeOptionBase<int, PathBase>, IPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this path type.</param>
    /// <param name="name">The name of this path type.</param>
    protected PathBase(int id, string name) : base(id, name, "Path")
    {
    }

    /// <summary>
    /// Gets the string representation of the path.
    /// </summary>
    public abstract string PathValue { get; }

    /// <summary>
    /// Gets the domain this path belongs to.
    /// </summary>
    public abstract string Domain { get; }
}
