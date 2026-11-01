using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for path implementations.
/// </summary>
public abstract class PathBase : TypeOptionBase<PathBase>, IPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathBase"/> class.
    /// </summary>
    /// <param name="name">The name of this path type.</param>
    protected PathBase(string name) : base(name, "Path")
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
