using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// The session is finished — its work merged (or abandoned) and its resources released. Terminal.
/// </summary>
[TypeOption(typeof(SessionStates), "Done")]
[ExcludeFromCodeCoverage]
public sealed class DoneState : SessionStateBase
{
    /// <summary>Initializes a new instance of the <see cref="DoneState"/> class.</summary>
    public DoneState() : base(6, "Done", isTerminal: true, isReclaimable: false) { }
}
