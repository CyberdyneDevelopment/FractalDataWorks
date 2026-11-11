using Fdw.Collections;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Navigation.MemberKinds;

/// <summary>One value of <see cref="MemberKindFilters"/>: a kind of member a member listing can be filtered to, carrying the Roslyn <see cref="SymbolKind"/> it stands for: the kinds a type's members have.</summary>
public interface IMemberKindFilter : ITypeOption<int, MemberKindFilterBase>
{
    /// <summary>Gets the Roslyn symbol kind this filter stands for.</summary>
    SymbolKind Kind { get; }
}
