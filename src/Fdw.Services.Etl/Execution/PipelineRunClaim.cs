using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// What one pipeline run needs before it may start: the tables it writes, whether it must run alone, how many runs the platform
/// allows at once (FDW-3085), and how many runs of its own pipeline are allowed at once (FDW-3117).
/// </summary>
/// <remarks>
/// The scheduler reads nothing but this, so it knows nothing about pipelines. The limits ride on the claim because they are settings
/// that can change while runs wait: each run is judged by the values read when it was accepted.
/// </remarks>
/// <param name="Run">Which run it is: the execution, the pipeline and the tenant the run belongs to (runs with no tenant count together).</param>
/// <param name="WriteKeys">The tables the run writes, one key each; two runs that share a key take turns. Empty for a run that writes nothing.</param>
/// <param name="Alone">Whether the run must be the only run in flight, because what it writes cannot be told from the pipeline.</param>
/// <param name="ServerLimit">The most runs in flight on the host while this run is among them; at least one.</param>
/// <param name="TenantLimit">The most runs in flight for this run's tenant while this run is among them; at least one.</param>
/// <param name="Cap">How many runs of the run's pipeline may be in flight at once, or <see langword="null"/> for a run whose pipeline does not state it.</param>
public sealed record PipelineRunClaim(PipelineRunIdentity Run, IReadOnlySet<string> WriteKeys, bool Alone, int ServerLimit, int TenantLimit, PipelineRunCap? Cap)
{
    /// <summary>Gets the tenant the run belongs to, or <see langword="null"/> for a run that belongs to none.</summary>
    public Guid? TenantId => Run.TenantId;

    /// <summary>Claims the tables a run writes.</summary>
    /// <param name="run">Which run it is.</param>
    /// <param name="writeKeys">The tables the run writes.</param>
    /// <param name="limits">The limits read for the run.</param>
    /// <param name="cap">How many runs of the run's pipeline may be in flight at once, if its pipeline states it.</param>
    /// <returns>The claim.</returns>
    public static PipelineRunClaim Writing(PipelineRunIdentity run, IEnumerable<string> writeKeys, PipelineRunLimitValues limits, PipelineRunCap? cap)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(writeKeys);
        ArgumentNullException.ThrowIfNull(limits);
        return new PipelineRunClaim(run, new HashSet<string>(writeKeys, StringComparer.OrdinalIgnoreCase), false, limits.PerServer, limits.PerTenant, cap);
    }

    /// <summary>Claims the whole host, for a run whose writes cannot be told from its pipeline.</summary>
    /// <param name="run">Which run it is.</param>
    /// <param name="limits">The limits read for the run.</param>
    /// <param name="cap">How many runs of the run's pipeline may be in flight at once, if its pipeline states it.</param>
    /// <returns>The claim.</returns>
    public static PipelineRunClaim RunningAlone(PipelineRunIdentity run, PipelineRunLimitValues limits, PipelineRunCap? cap)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(limits);
        return new PipelineRunClaim(run, new HashSet<string>(StringComparer.OrdinalIgnoreCase), true, limits.PerServer, limits.PerTenant, cap);
    }
}
