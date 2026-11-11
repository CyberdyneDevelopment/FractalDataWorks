using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>The member has no overridden member and implements no interface member, so there is nothing for <c>&lt;inheritdoc/&gt;</c> to inherit documentation from.</summary>
[TypeOption(typeof(UnresolvedReasons), "NoBaseMember")]
[ExcludeFromCodeCoverage]
public sealed class NoBaseMemberUnresolvedReason : UnresolvedReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="NoBaseMemberUnresolvedReason"/> class.</summary>
    public NoBaseMemberUnresolvedReason() : base(1, "NoBaseMember") { }
}
