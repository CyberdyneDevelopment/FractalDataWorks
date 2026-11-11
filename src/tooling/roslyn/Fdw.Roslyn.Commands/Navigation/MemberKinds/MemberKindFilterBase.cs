using Fdw.Collections;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Navigation.MemberKinds;

/// <summary>Base of every <see cref="MemberKindFilters"/> option: a kind of member a member listing can be filtered to, carrying the Roslyn <see cref="SymbolKind"/> it stands for: the kinds a type's members have.</summary>
public abstract class MemberKindFilterBase : TypeOptionBase<MemberKindFilterBase>, IMemberKindFilter
{
    /// <summary>Initializes a new instance of the <see cref="MemberKindFilterBase"/> class.</summary>
    /// <param name="id">The option's id; ids start at 1, since 0 is the collection's not-found value.</param>
    /// <param name="name">The option's name, as a request states it (matched ignoring case).</param>
    protected MemberKindFilterBase(int id, string name) : base(id, name) { }

    /// <inheritdoc />
    public abstract SymbolKind Kind { get; }
}
