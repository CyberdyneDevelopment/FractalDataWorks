using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>The states a lifecycle phase is in: NotRun, Deferred and Ran (FDW-4149). Replaces the PhaseState enum.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PhaseStateBase), typeof(IPhaseState), typeof(PhaseStates))]
public abstract partial class PhaseStates : TypeCollectionBase<PhaseStateBase, IPhaseState>
{
}
