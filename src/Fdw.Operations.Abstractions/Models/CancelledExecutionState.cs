using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Execution was cancelled.</summary>
[TypeOption(typeof(ExecutionStates), "Cancelled")]
[ExcludeFromCodeCoverage]
public sealed class CancelledExecutionState : ExecutionStateBase
{
    /// <summary>Initializes a new instance of <see cref="CancelledExecutionState"/>.</summary>
    public CancelledExecutionState() : base(7, "Cancelled") { }
}
