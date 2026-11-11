using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Navigation.MemberKinds;

/// <summary>Properties and indexers.</summary>
[TypeOption(typeof(MemberKindFilters), "Property")]
[ExcludeFromCodeCoverage]
public sealed class PropertyMemberKindFilter : MemberKindFilterBase
{
    /// <summary>Initializes a new instance of the <see cref="PropertyMemberKindFilter"/> class.</summary>
    public PropertyMemberKindFilter() : base(2, "Property") { }

    /// <inheritdoc />
    public override SymbolKind Kind => SymbolKind.Property;
}
