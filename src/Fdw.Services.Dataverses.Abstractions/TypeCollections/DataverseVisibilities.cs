using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Who can find a dataverse.</summary>
/// <remarks>
/// Distinct from access. Visibility decides whether a dataverse appears to someone who is not a member; whether they can then read anything is a grant question, and row-level security still applies underneath either way.
///
/// The matching database CHECK constraint on the column is the backstop, not the definition — it
/// refuses an out-of-set value that reaches the table by some other path. This collection is what
/// an endpoint validates against, so a bad value is rejected with a message naming the option
/// rather than surfacing as a constraint violation.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataverseVisibilityBase), typeof(IDataverseVisibility), typeof(DataverseVisibilities))]
public abstract partial class DataverseVisibilities : TypeCollectionBase<DataverseVisibilityBase, IDataverseVisibility>
{
}
