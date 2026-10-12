using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Base class for <see cref="MismatchKinds"/> options.
/// </summary>
public abstract class MismatchKindBase : TypeOptionBase<MismatchKindBase>, IMismatchKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MismatchKindBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    protected MismatchKindBase(string name) : base(name) { }
}
