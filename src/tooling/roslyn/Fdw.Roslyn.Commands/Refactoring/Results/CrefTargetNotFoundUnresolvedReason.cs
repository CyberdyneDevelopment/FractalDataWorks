using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>An explicit <c>&lt;inheritdoc cref="X"/&gt;</c> names a target that the semantic model cannot resolve to a symbol.</summary>
[TypeOption(typeof(UnresolvedReasons), "CrefTargetNotFound")]
[ExcludeFromCodeCoverage]
public sealed class CrefTargetNotFoundUnresolvedReason : UnresolvedReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="CrefTargetNotFoundUnresolvedReason"/> class.</summary>
    public CrefTargetNotFoundUnresolvedReason() : base(4, "CrefTargetNotFound") { }
}
