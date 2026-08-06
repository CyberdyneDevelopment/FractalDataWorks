using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>The session is open and being actively worked. Not terminal; not reclaimable.</summary>
[TypeOption(typeof(SessionStates), "Open")]
[ExcludeFromCodeCoverage]
public sealed class OpenState : SessionStateBase
{
    /// <summary>Initializes a new instance of the <see cref="OpenState"/> class.</summary>
    public OpenState() : base(1, "Open", isTerminal: false, isReclaimable: false) { }
}
