using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Initial state.</summary>
[TypeOption(typeof(ExecutionStates), "Idle")]
[ExcludeFromCodeCoverage]
public sealed class IdleExecutionState : ExecutionStateBase
{
    /// <summary>Initializes a new instance of <see cref="IdleExecutionState"/>.</summary>
    public IdleExecutionState() : base(1, "Idle") { }
}
