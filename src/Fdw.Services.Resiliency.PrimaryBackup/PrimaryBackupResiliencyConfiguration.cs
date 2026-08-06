using Fdw.Configuration;
using System;
using Fdw.Services.Resiliency;

namespace Fdw.Services.Resiliency.PrimaryBackup;

/// <summary>
/// Configuration for the PrimaryBackup resiliency strategy.
/// Fields map to the <c>settings.PrimaryBackupResiliency</c> database table.
/// </summary>
public sealed class PrimaryBackupResiliencyConfiguration : ResiliencyConfiguration
{
    /// <inheritdoc/>
    public override string SectionName => "Resiliency:PrimaryBackup";

    /// <inheritdoc/>
    public override string StrategyType => "PrimaryBackup";

    /// <summary>
    /// Gets or sets the backup data set identifier to use when primary fails.
    /// References <c>data.DataSet.Id</c>.
    /// </summary>
    public Guid BackupDataSetId { get; set; }

    /// <summary>
    /// Gets or sets the refresh schedule identifier to trigger after backup activation.
    /// References <c>sched.Schedule.Id</c>. Used to re-sync the primary source.
    /// </summary>
    public Guid RefreshScheduleId { get; set; }

    /// <summary>
    /// Gets or sets whether data served from the backup source is annotated in the lineage graph.
    /// </summary>
    public bool FlagBackupDataInLineage { get; set; } = true;
}
