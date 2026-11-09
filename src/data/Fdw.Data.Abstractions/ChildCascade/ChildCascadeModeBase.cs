using Fdw.Collections;

namespace Fdw.Data;

/// <summary>
/// Base class for the modes in which a save cascades to a child collection that hangs from the owner's durable Id (CRTP pattern).
/// </summary>
public abstract class ChildCascadeModeBase : TypeOptionBase<ChildCascadeModeBase>, IChildCascadeMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChildCascadeModeBase"/> class.
    /// </summary>
    /// <param name="name">The mode's name.</param>
    /// <param name="summary">A one-sentence description.</param>
    /// <param name="retiresAbsentChildren">Whether a current child the saved list does not carry is retired.</param>
    protected ChildCascadeModeBase(string name, string summary, bool retiresAbsentChildren)
        : base(name)
    {
        Summary = summary;
        RetiresAbsentChildren = retiresAbsentChildren;
    }

    /// <inheritdoc />
    public string Summary { get; }

    /// <inheritdoc />
    public bool RetiresAbsentChildren { get; }
}
