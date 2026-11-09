using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// The record's list adds to the collection: a save inserts or re-versions the children the list carries and retires none; a child leaves only by its own delete route.
/// </summary>
[TypeOption(typeof(ChildCascadeModes), "UpsertOnly")]
[ExcludeFromCodeCoverage]
public sealed class UpsertOnlyChildCascade : ChildCascadeModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertOnlyChildCascade"/> class.
    /// </summary>
    public UpsertOnlyChildCascade()
        : base("UpsertOnly", "The record's list adds: a save inserts or re-versions the children it carries and retires none.", false)
    {
    }
}
