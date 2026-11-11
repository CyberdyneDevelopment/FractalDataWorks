using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Navigation.MemberKinds;

/// <summary>Events.</summary>
[TypeOption(typeof(MemberKindFilters), "Event")]
[ExcludeFromCodeCoverage]
public sealed class EventMemberKindFilter : MemberKindFilterBase
{
    /// <summary>Initializes a new instance of the <see cref="EventMemberKindFilter"/> class.</summary>
    public EventMemberKindFilter() : base(4, "Event") { }

    /// <inheritdoc />
    public override SymbolKind Kind => SymbolKind.Event;
}
