using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// Base class for port direction types using the CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class PortDirectionBase : TypeOptionBase<int, PortDirectionBase>, IPortDirection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PortDirectionBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this direction type.</param>
    /// <param name="name">The name of this direction type.</param>
    protected PortDirectionBase(int id, string name) : base(id, name)
    {
    }
}
