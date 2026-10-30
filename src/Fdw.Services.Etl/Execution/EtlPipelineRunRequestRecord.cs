using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The row of etl.PipelineRunRequest (FDW-3216): the request of a pipeline run the host has accepted and not started, so a restart can take the run up
/// again. It holds the values callers give runs, so only <see cref="PipelineRunRequestStore"/> reads or writes it and no screen or API lists it.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineRunRequestRecord
{
    /// <summary>Gets or sets the row's identifier.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the run's id, the id of its execution record.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the run's tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the request as text: the pipeline, the trigger source, the roles and each parameter value with its type.</summary>
    public string Request { get; set; } = string.Empty;

    /// <summary>Gets or sets when a run held by DelayedStart is due; none for a run that is not held.</summary>
    public DateTimeOffset? HeldUntil { get; set; }

    /// <summary>Gets or sets how many times the server has started the run again after a stop (FDW-3236); zero for a run not yet restarted.</summary>
    public int Restarts { get; set; }
}
