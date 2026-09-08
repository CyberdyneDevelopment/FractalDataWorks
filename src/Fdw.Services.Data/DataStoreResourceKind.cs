using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Dataverses.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// A data store attached to a dataverse.
/// </summary>
/// <remarks>
/// Declared here rather than in the dataverses package because this package owns the data store.
/// A host that has not referenced data stores cannot attach one to a dataverse.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseResourceKinds), "DataStore")]
public sealed class DataStoreResourceKind : DataverseResourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataStoreResourceKind"/> class.</summary>
    /// <remarks>
    /// Why <c>canBeOwned: false</c>: a data store is the platform's, not a project's. It is the
    /// shared place other dataverses read through too, so archiving a dataverse must not take it
    /// along. This is the "uses but does not own" half of the distinction, and the only kind
    /// declared so far that sits on that side of it.
    /// </remarks>
    public DataStoreResourceKind()
        : base("DataStore", canBeOwned: false)
    {
    }
}
