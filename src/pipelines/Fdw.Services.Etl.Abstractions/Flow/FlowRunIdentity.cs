using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Who a pipeline run is, to a step that starts another pipeline (FDW-1403): the execution it runs under, its tenant and its roles.
/// </summary>
/// <param name="ExecutionId">The execution a started pipeline becomes a child of.</param>
/// <param name="TenantId">The tenant the started pipeline belongs to.</param>
/// <param name="RunRoles">The roles the started pipeline acts with.</param>
public sealed record FlowRunIdentity(Guid ExecutionId, Guid TenantId, IReadOnlyList<string> RunRoles)
{
    /// <summary>
    /// Gets the person the run acts as (FDW-6137), or null for a run that acts as no person. A pipeline the run starts states this person, and the
    /// pipeline layer refuses a start that states any other.
    /// </summary>
    public Guid? PersonId { get; init; }

    /// <summary>
    /// Gets the run this one continues (FDW-6139): the first run of a chain of restarts, or null for a run that is not a restart. A step with an outside effect is claimed under
    /// <see cref="ClaimRootId"/>, so a restarted run finds what the first run already did.
    /// </summary>
    public Guid? ContinuesRun { get; init; }

    /// <summary>Gets the run every claim of the chain is made under: the first run of a chain of restarts, which is the run itself when it is not a restart.</summary>
    public Guid ClaimRootId => ContinuesRun is { } first ? first : ExecutionId;
}
