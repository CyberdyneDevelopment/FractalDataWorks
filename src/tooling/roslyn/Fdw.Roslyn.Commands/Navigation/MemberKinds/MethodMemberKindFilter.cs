using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Navigation.MemberKinds;

/// <summary>Methods, constructors and operators.</summary>
[TypeOption(typeof(MemberKindFilters), "Method")]
[ExcludeFromCodeCoverage]
public sealed class MethodMemberKindFilter : MemberKindFilterBase
{
    /// <summary>Initializes a new instance of the <see cref="MethodMemberKindFilter"/> class.</summary>
    public MethodMemberKindFilter() : base(1, "Method") { }

    /// <inheritdoc />
    public override SymbolKind Kind => SymbolKind.Method;
}
