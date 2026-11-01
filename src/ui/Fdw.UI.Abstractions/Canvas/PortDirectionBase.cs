using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// Base class for port direction types using the CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class PortDirectionBase : TypeOptionBase<PortDirectionBase>, IPortDirection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PortDirectionBase"/> class.
    /// </summary>
    /// <param name="name">The name of this direction type.</param>
    protected PortDirectionBase(string name) : base(name)
    {
    }
}
