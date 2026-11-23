using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>A row of <c>sched.ScheduleConfiguration</c> read for what a fire needs to know who it acts as (FDW-6144): the schedule's pipeline, its tenant and the grant it stores.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class ScheduleGrantLinkRecord
{
    /// <summary>Gets or sets the schedule's Id, which every version of it keeps.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline the schedule runs.</summary>
    public Guid PipelineId { get; set; }

    /// <summary>Gets or sets the schedule's tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the grant the schedule's runs act under; none for a schedule that has none.</summary>
    public Guid? RunGrantId { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the current version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the row is retired.</summary>
    public bool IsDeleted { get; set; }
}
