using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>The strand is active: it holds a live scope claim and is being worked. Not terminal.</summary>
[TypeOption(typeof(StrandStates), "Active")]
[ExcludeFromCodeCoverage]
public sealed class ActiveStrandState : StrandStateBase
{
    /// <summary>Initializes a new instance of the <see cref="ActiveStrandState"/> class.</summary>
    public ActiveStrandState() : base(1, "Active", isTerminal: false) { }
}
