using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Navigation.MemberKinds;

/// <summary>TypeCollection of a kind of member a member listing can be filtered to, carrying the Roslyn <see cref="Microsoft.CodeAnalysis.SymbolKind"/> it stands for: the kinds a type's members have.</summary>
[TypeCollection(typeof(MemberKindFilterBase), typeof(IMemberKindFilter), typeof(MemberKindFilters))]
[ExcludeFromCodeCoverage]
public abstract partial class MemberKindFilters : TypeCollectionBase<MemberKindFilterBase, IMemberKindFilter> { }
