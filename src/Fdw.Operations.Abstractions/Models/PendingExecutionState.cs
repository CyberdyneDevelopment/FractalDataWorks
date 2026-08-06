using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Waiting for dependencies.</summary>
[TypeOption(typeof(ExecutionStates), "Pending")]
[ExcludeFromCodeCoverage]
public sealed class PendingExecutionState : ExecutionStateBase
{
    /// <summary>Initializes a new instance of <see cref="PendingExecutionState"/>.</summary>
    public PendingExecutionState() : base(2, "Pending") { }
}
