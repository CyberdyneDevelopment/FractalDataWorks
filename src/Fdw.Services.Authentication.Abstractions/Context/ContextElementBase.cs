using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// Base class for context elements. Replaces the closed <c>ContextElement</c> enum so each element
/// can answer whether it is present on a context itself, rather than a switch elsewhere doing it.
/// </summary>
public abstract class ContextElementBase : TypeOptionBase<int, ContextElementBase>, IContextElement
{
    /// <summary>Initializes a new instance of the <see cref="ContextElementBase"/> class.</summary>
    /// <param name="id">The unique identifier for this element.</param>
    /// <param name="name">The name of this element.</param>
    protected ContextElementBase(int id, string name)
        : base(id, name)
    {
    }

    /// <inheritdoc/>
    public abstract bool IsPresentOn(AuthenticationContext context);
}
