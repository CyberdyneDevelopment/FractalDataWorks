using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>Base of every <see cref="UnresolvedReasons"/> option: why an <c>&lt;inheritdoc/&gt;</c> site could not be expanded: the true MA0196 candidates that need explicit documentation written by hand.</summary>
public abstract class UnresolvedReasonBase : TypeOptionBase<UnresolvedReasonBase>, IUnresolvedReason
{
    /// <summary>Initializes a new instance of the <see cref="UnresolvedReasonBase"/> class.</summary>
    /// <param name="id">The option's id; ids start at 1, since 0 is the collection's not-found value.</param>
    /// <param name="name">The option's name, as the results write it.</param>
    protected UnresolvedReasonBase(int id, string name) : base(id, name) { }
}
