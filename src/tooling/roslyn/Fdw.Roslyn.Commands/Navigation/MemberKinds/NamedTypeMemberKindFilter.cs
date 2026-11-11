using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Navigation.MemberKinds;

/// <summary>Nested types.</summary>
[TypeOption(typeof(MemberKindFilters), "NamedType")]
[ExcludeFromCodeCoverage]
public sealed class NamedTypeMemberKindFilter : MemberKindFilterBase
{
    /// <summary>Initializes a new instance of the <see cref="NamedTypeMemberKindFilter"/> class.</summary>
    public NamedTypeMemberKindFilter() : base(5, "NamedType") { }

    /// <inheritdoc />
    public override SymbolKind Kind => SymbolKind.NamedType;
}
