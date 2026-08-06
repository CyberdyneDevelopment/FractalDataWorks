using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Execution skipped.</summary>
[TypeOption(typeof(ExecutionStates), "Skipped")]
[ExcludeFromCodeCoverage]
public sealed class SkippedExecutionState : ExecutionStateBase
{
    /// <summary>Initializes a new instance of <see cref="SkippedExecutionState"/>.</summary>
    public SkippedExecutionState() : base(6, "Skipped") { }
}
