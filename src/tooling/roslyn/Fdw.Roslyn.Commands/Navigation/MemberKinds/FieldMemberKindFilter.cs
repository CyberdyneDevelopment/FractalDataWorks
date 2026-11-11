using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Navigation.MemberKinds;

/// <summary>Fields.</summary>
[TypeOption(typeof(MemberKindFilters), "Field")]
[ExcludeFromCodeCoverage]
public sealed class FieldMemberKindFilter : MemberKindFilterBase
{
    /// <summary>Initializes a new instance of the <see cref="FieldMemberKindFilter"/> class.</summary>
    public FieldMemberKindFilter() : base(3, "Field") { }

    /// <inheritdoc />
    public override SymbolKind Kind => SymbolKind.Field;
}
