using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// The record's list is the full set of the collection: a save inserts or re-versions the children the list carries and retires the current children it does not.
/// </summary>
[TypeOption(typeof(ChildCascadeModes), "Authoritative")]
[ExcludeFromCodeCoverage]
public sealed class AuthoritativeChildCascade : ChildCascadeModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthoritativeChildCascade"/> class.
    /// </summary>
    public AuthoritativeChildCascade()
        : base("Authoritative", "The record's list is the full set: a save retires the current children the list does not carry.", true)
    {
    }
}
