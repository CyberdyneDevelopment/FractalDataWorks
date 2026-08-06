using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Execution failed.</summary>
[TypeOption(typeof(ExecutionStates), "Failed")]
[ExcludeFromCodeCoverage]
public sealed class FailedExecutionState : ExecutionStateBase
{
    /// <summary>Initializes a new instance of <see cref="FailedExecutionState"/>.</summary>
    public FailedExecutionState() : base(5, "Failed") { }
}
