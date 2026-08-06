using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Completed successfully.</summary>
[TypeOption(typeof(ExecutionStates), "Success")]
[ExcludeFromCodeCoverage]
public sealed class SuccessExecutionState : ExecutionStateBase
{
    /// <summary>Initializes a new instance of <see cref="SuccessExecutionState"/>.</summary>
    public SuccessExecutionState() : base(4, "Success") { }
}
