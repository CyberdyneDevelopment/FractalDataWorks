using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// The members of an update body that change a saved schedule, held in one place so the rule that an update must state
/// something, its refusal message, the list of members that message names and the code that applies each stated member to the
/// stored schedule cannot drift apart (FDW-1170).
/// </summary>
/// <remarks>
/// <para>
/// Why the list is data: the "at least one field" rule was written twice (the scheduler's base validator and the live one) and
/// neither counted the time zone, the run parameters or the run roles, so a body that stated only the zone was refused and a
/// body that stated the zone beside another member was accepted and the zone dropped. Every member an update may change is a row
/// here with the way it reaches the stored schedule, so a member cannot be accepted without being applied.
/// </para>
/// <para>
/// Not a member: the schedule's name (the route's) and the pipeline's name (sent only with its id, and applied with it). The
/// scheduler type is stated but never changed, so it is on <see cref="Unchangeable"/> and judged by <see cref="CheckType"/>.
/// </para>
/// </remarks>
public static class UpdateScheduleChanges
{
    /// <summary>The error code the update validator gives a body that states nothing to change.</summary>
    public const string NothingStatedCode = "NothingToUpdate";

    private static readonly Member[] Members =
    [
        new(nameof(UpdateScheduleRequest.PipelineId), request => request.PipelineId is not null,
            Apply: (request, schedule, _) =>
            {
                schedule.PipelineId = request.PipelineId!.Value;
                schedule.PipelineName = request.PipelineName!;
            }),

        new(nameof(UpdateScheduleRequest.CronExpression), request => request.CronExpression is not null,
            Apply: (request, schedule, _) => schedule.CronExpression = request.CronExpression),

        new(nameof(UpdateScheduleRequest.IntervalSeconds), request => request.IntervalSeconds.HasValue,
            Apply: (request, schedule, _) => schedule.IntervalSeconds = request.IntervalSeconds!.Value),

        // The scheduler reads the stored zone when it registers a Cron trigger and compares it when it reconciles, so storing the
        // zone is what moves the trigger.
        new(nameof(UpdateScheduleRequest.TimeZoneId), request => request.TimeZoneId is not null,
            Apply: (request, schedule, _) => schedule.TimeZoneId = request.TimeZoneId!),

        new(nameof(UpdateScheduleRequest.IsEnabled), request => request.IsEnabled.HasValue,
            Apply: (request, schedule, _) => schedule.IsEnabled = request.IsEnabled!.Value),

        // Stated values replace the stored set whole; a body that states none leaves them (contract v2 item 9).
        new(nameof(UpdateScheduleRequest.Parameters), request => request.Parameters is not null,
            Apply: (request, schedule, _) => schedule.Parameters = ScheduleParameterMapper.ToConfiguration(request.Parameters)),

        // The roles arrive already judged (ScheduleRunRoleAuthority); stated roles replace the recorded ones whole (FDW-1161).
        new(nameof(UpdateScheduleRequest.RunRoles), request => request.RunRoles is not null,
            Apply: (_, schedule, runRoles) => schedule.ReplaceRunRoles(runRoles!)),
    ];

    /// <summary>Gets the names of the members an update may change, in the order the refusal lists them.</summary>
    public static IReadOnlyList<string> Names { get; } = [.. Members.Select(member => member.Name)];

    /// <summary>
    /// Gets the names of the members of the request that an update states but never changes: the type of a stored schedule is the
    /// implementation its row is saved under (FDW-1171). They are judged against the stored schedule by <see cref="CheckType"/>
    /// and refused when they differ, so they are not on <see cref="Names"/> and do not count as a change.
    /// </summary>
    public static IReadOnlyList<string> Unchangeable { get; } = [nameof(UpdateScheduleRequest.SchedulerType)];

    /// <summary>Gets the refusal message for a body that states no member to change, naming every member that could be stated.</summary>
    public static string RefusalMessage { get; } =
        string.Concat(
            "At least one updateable field must be supplied (", string.Join(", ", Names), "); a schedule's ",
            string.Join(", ", Unchangeable), " cannot be changed");

    /// <summary>Gets whether the body states at least one member to change.</summary>
    /// <param name="request">The update body.</param>
    /// <returns><see langword="true"/> when the body states any member on the list.</returns>
    public static bool Stated(UpdateScheduleRequest request) => Members.Any(member => member.Stated(request));

    /// <summary>Gets whether the body states the one named member.</summary>
    /// <param name="request">The update body.</param>
    /// <param name="name">A name from <see cref="Names"/>.</param>
    /// <returns><see langword="true"/> when the body states that member.</returns>
    public static bool Stated(UpdateScheduleRequest request, string name) =>
        Members.Single(member => string.Equals(member.Name, name, StringComparison.Ordinal)).Stated(request);

    /// <summary>Judges the scheduler type the body states, if any, against the type the schedule is stored under.</summary>
    /// <param name="request">The update body.</param>
    /// <param name="storedType">The type the stored schedule is saved under.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success when the body states no type or the stored one; otherwise the refusal naming both types.</returns>
    public static IGenericResult CheckType(UpdateScheduleRequest request, string storedType, ILogger logger) =>
        ScheduleTypeChange.Check(request.SchedulerType, storedType, logger);

    /// <summary>Applies every member the body states to the stored schedule; a member it does not state is left as stored.</summary>
    /// <param name="request">The update body.</param>
    /// <param name="schedule">The stored schedule to change.</param>
    /// <param name="runRoles">The roles to record when the body states <c>RunRoles</c>, already judged; <see langword="null"/> when it does not.</param>
    /// <returns>The same schedule, changed.</returns>
    public static IScheduleImplementationConfiguration Apply(
        UpdateScheduleRequest request, IScheduleImplementationConfiguration schedule, IReadOnlyList<string>? runRoles)
    {
        foreach (var member in Members.Where(member => member.Stated(request)))
        {
            member.Apply(request, schedule, runRoles);
        }

        return schedule;
    }

    private sealed record Member(
        string Name,
        Func<UpdateScheduleRequest, bool> Stated,
        Action<UpdateScheduleRequest, IScheduleImplementationConfiguration, IReadOnlyList<string>?> Apply);
}
