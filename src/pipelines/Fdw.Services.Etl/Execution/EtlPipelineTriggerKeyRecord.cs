using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The row of etl.PipelineTriggerKey (FDW-4317): the key of one scheduled fire the host has taken, and the run it started. Only <see cref="PipelineTriggerKeys"/> reads or writes it.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineTriggerKeyRecord
{
    /// <summary>Gets or sets the row's identifier.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the tenant the fire is for.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the fire's key: the schedule's Id and the time the fire was due.</summary>
    public string KeyText { get; set; } = string.Empty;

    /// <summary>Gets or sets the time the fire was due.</summary>
    public DateTimeOffset FireTime { get; set; }

    /// <summary>Gets or sets the run the fire started; none while the call that claimed the key has not made its run yet.</summary>
    public Guid? ExecutionId { get; set; }
}
