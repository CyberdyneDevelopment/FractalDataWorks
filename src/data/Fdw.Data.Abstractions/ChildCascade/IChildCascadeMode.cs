using Fdw.Collections;

namespace Fdw.Data;

/// <summary>
/// How saving an owner treats the rows of one child collection that hangs from the owner's durable Id (FDW-4240).
/// </summary>
public interface IChildCascadeMode : ITypeOption<int, ChildCascadeModeBase>
{
    /// <summary>Gets a one-sentence description of the mode.</summary>
    string Summary { get; }

    /// <summary>
    /// Gets whether the record's list is the full set of the collection: a current child the list does not carry is retired.
    /// When false, a save inserts or re-versions the children the list carries and retires none; a child leaves only by its own delete route.
    /// </summary>
    bool RetiresAbsentChildren { get; }
}
