using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// One of the things an authentication step may require or contribute.
/// </summary>
public interface IContextElement : ITypeOption<int, ContextElementBase>
{
    /// <summary>Returns whether this element is already present on <paramref name="context"/>.</summary>
    /// <param name="context">The context to check.</param>
    bool IsPresentOn(AuthenticationContext context);
}
