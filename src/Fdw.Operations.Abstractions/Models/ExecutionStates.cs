using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>TypeCollection for execution states.</summary>
[TypeCollection(typeof(ExecutionStateBase), typeof(IExecutionState), typeof(ExecutionStates))]
[ExcludeFromCodeCoverage]
public abstract partial class ExecutionStates : TypeCollectionBase<ExecutionStateBase, IExecutionState> { }
