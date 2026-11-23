using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// POCO for the <c>etl.PipelineStepClaim</c> table (FDW-6139): the claim of one step of one iteration of a chain of runs. Property names MUST match the SQL column names exactly. OpsDb has no row-level
/// security, so every read and write filters by <see cref="TenantId"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineStepClaimRecord
{
    /// <summary>Gets or sets the unique identifier of the claim.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the tenant the run ran in.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the first run of the chain of restarts.</summary>
    public Guid ClaimRootId { get; set; }

    /// <summary>Gets or sets the component's name.</summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>Gets or sets which time the step acted in the run, from zero.</summary>
    public int Iteration { get; set; }

    /// <summary>Gets or sets the run that made the claim.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the outcome the step recorded; null while it acts.</summary>
    public string? Outcome { get; set; }
}
