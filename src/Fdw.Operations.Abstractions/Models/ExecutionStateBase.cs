using Fdw.Collections;

namespace Fdw.Operations.Clients.Models;

/// <summary>Base class for execution states for nodes.</summary>
public abstract class ExecutionStateBase : TypeOptionBase<ExecutionStateBase>, IExecutionState
{
    /// <summary>Initializes a new instance of <see cref="ExecutionStateBase"/>.</summary>
    protected ExecutionStateBase(string name) : base(name) { }
}
