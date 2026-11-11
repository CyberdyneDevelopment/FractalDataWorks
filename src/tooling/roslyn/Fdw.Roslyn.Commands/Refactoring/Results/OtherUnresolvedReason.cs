using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>The site could not be resolved for a reason that does not fit the other categories (for example the documented symbol could not be determined, or the resolved XML failed to parse).</summary>
[TypeOption(typeof(UnresolvedReasons), "Other")]
[ExcludeFromCodeCoverage]
public sealed class OtherUnresolvedReason : UnresolvedReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="OtherUnresolvedReason"/> class.</summary>
    public OtherUnresolvedReason() : base(5, "Other") { }
}
