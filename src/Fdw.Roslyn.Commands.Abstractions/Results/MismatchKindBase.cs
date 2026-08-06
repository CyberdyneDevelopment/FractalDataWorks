using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Base class for <see cref="MismatchKinds"/> options.
/// </summary>
public abstract class MismatchKindBase : TypeOptionBase<int, MismatchKindBase>, IMismatchKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MismatchKindBase"/> class.
    /// </summary>
    /// <param name="id">The option id.</param>
    /// <param name="name">The option name.</param>
    protected MismatchKindBase(int id, string name) : base(id, name) { }
}
