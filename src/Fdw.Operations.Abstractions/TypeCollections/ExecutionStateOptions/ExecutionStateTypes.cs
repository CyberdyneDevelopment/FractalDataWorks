using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// TypeCollection for execution state types defining the state machine.
/// State transitions: Scheduled → Triggered → Initialized → Running → Completed/Failed
/// Additional states: Paused, Compensating, Retrying, Cancelled
/// </summary>
/// <remarks>
/// <para>
/// State machine diagram:
/// <code>
/// Scheduled → Triggered → Initialized → Running → Completed
///                                    ↘       ↓ ↘ Failed
///                                 Paused ↔ Running ↔ Retrying
///                                           ↓
///                                    Compensating → Failed
///                                           ↓
///                                       Cancelled
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(ExecutionStateTypeBase), typeof(IExecutionStateType), typeof(ExecutionStateTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class ExecutionStateTypes : TypeCollectionBase<ExecutionStateTypeBase, IExecutionStateType>
{
}

// =============================================================================
// Initial States (1-9)
// =============================================================================

// =============================================================================
// Active States (10-19)
// =============================================================================

// =============================================================================
// Terminal States (20-29)
// =============================================================================