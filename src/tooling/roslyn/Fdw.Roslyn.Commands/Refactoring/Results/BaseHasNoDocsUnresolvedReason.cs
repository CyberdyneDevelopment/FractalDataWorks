using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>A base member (override or interface member) exists, but it carries no XML documentation to inherit. Commonly a BCL or referenced-assembly member whose XML doc file did not ship.</summary>
[TypeOption(typeof(UnresolvedReasons), "BaseHasNoDocs")]
[ExcludeFromCodeCoverage]
public sealed class BaseHasNoDocsUnresolvedReason : UnresolvedReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="BaseHasNoDocsUnresolvedReason"/> class.</summary>
    public BaseHasNoDocsUnresolvedReason() : base(2, "BaseHasNoDocs") { }
}
