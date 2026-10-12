using Fdw.Services.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Maps a stored schedule to the DTOs the API answers with, in one place (FDW-1166).
/// </summary>
/// <remarks>
/// Why one mapper: the single read, the create and update responses, the toggle response and the list each built their DTO by
/// hand, and a member added to one (the run roles of FDW-1161) was missing from the others, so a listed schedule showed none and
/// a toggled one showed no parameters either. A schedule's members now reach every response through this class, and every
/// member a DTO declares is set from the stored schedule: the detail once left the time zone and the audit members unset while
/// the list row carried them (FDW-1168). A test reflects over the DTOs so a member added to one cannot be dropped silently.
/// </remarks>
public static class ScheduleDtoMapper
{
    /// <summary>
    /// How deep a read of a schedule must go for these mappings to tell the truth: the run parameters and the run roles are child
    /// collections, and a collection below the depth a read asked for is left empty (see <see cref="ConfigurationLevels"/>), which
    /// a response cannot tell from a schedule that has none.
    /// </summary>
    /// <remarks>
    /// Every read that feeds <see cref="Detail"/> or <see cref="Summary"/> uses this level, so the list and the single read hydrate
    /// the same members (FDW-1168). It is the whole aggregate rather than one level because a schedule's children have none below
    /// them today and a response must not silently lose a member the day they do.
    /// </remarks>
    public const int ReadLevels = ConfigurationLevels.WholeAggregate;

    /// <summary>Maps a stored schedule to the detail DTO.</summary>
    /// <param name="config">The stored schedule.</param>
    /// <returns>The detail, with the run parameters and the run roles the schedule records.</returns>
    public static ScheduleDetailDto Detail(IScheduleImplementationConfiguration config) => new()
    {
        Id = config.Id,
        Name = config.Name,
        PipelineId = config.PipelineId,
        PipelineName = config.PipelineName,
        SchedulerType = config.ScheduleType,
        IsEnabled = config.IsEnabled,
        NextRunTime = config.NextRunTime,
        LastRunTime = config.LastRunTime,
        CronExpression = config.CronExpression,
        IntervalSeconds = config.IntervalSeconds,
        TimeZoneId = config.TimeZoneId,
        CreatedAt = config.CreateDate,
        UpdatedAt = config.ModifyDate,
        CreatedBy = config.CreateBy,
        ModifiedBy = config.ModifyBy,
        CreatedOnBehalfOf = config.CreateOnBehalfOf,
        ModifiedOnBehalfOf = config.ModifyOnBehalfOf,
        Parameters = ScheduleParameterMapper.ToValues(config.Parameters),
        RunRoles = [.. config.RunRoleNames]
    };

    /// <summary>Maps a stored schedule to a list row.</summary>
    /// <param name="config">The stored schedule.</param>
    /// <returns>The summary, with the run parameters and the run roles the schedule records.</returns>
    public static ScheduleSummaryDto Summary(IScheduleImplementationConfiguration config) => new()
    {
        Id = config.Id,
        Name = config.Name,
        PipelineId = config.PipelineId,
        PipelineName = config.PipelineName,
        SchedulerType = config.ScheduleType,
        IsEnabled = config.IsEnabled,
        NextRunTime = config.NextRunTime,
        LastRunTime = config.LastRunTime,
        CronExpression = config.CronExpression,
        IntervalSeconds = config.IntervalSeconds,
        TimeZoneId = config.TimeZoneId,
        CreatedAt = config.CreateDate,
        CreatedBy = config.CreateBy,
        ModifiedBy = config.ModifyBy,
        CreatedOnBehalfOf = config.CreateOnBehalfOf,
        ModifiedOnBehalfOf = config.ModifyOnBehalfOf,
        Parameters = ScheduleParameterMapper.ToValues(config.Parameters),
        RunRoles = [.. config.RunRoleNames]
    };
}
