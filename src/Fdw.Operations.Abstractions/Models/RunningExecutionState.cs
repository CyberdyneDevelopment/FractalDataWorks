using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Currently executing.</summary>
[TypeOption(typeof(ExecutionStates), "Running")]
[ExcludeFromCodeCoverage]
public sealed class RunningExecutionState : ExecutionStateBase
{
    /// <summary>Initializes a new instance of <see cref="RunningExecutionState"/>.</summary>
    public RunningExecutionState() : base(3, "Running") { }
}
