using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Where a universe is in its lifecycle.</summary>
/// <remarks>
/// A closed lifecycle rather than an open extension point, but a TypeCollection rather than an enum because FDW017 asks for one and because ByName lookup is what an endpoint needs to reject a bad value.
///
/// The matching database CHECK constraint on the column is the backstop, not the definition — it
/// refuses an out-of-set value that reaches the table by some other path. This collection is what
/// an endpoint validates against, so a bad value is rejected with a message naming the option
/// rather than surfacing as a constraint violation.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(UniverseStatusBase), typeof(IUniverseStatus), typeof(UniverseStatuses))]
public abstract partial class UniverseStatuses : TypeCollectionBase<UniverseStatusBase, IUniverseStatus>
{
}
