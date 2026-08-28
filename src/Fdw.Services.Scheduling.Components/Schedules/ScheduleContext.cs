#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.UI.Providers;

namespace Fdw.Services.Scheduling.Components.Schedules;

[ExcludeFromCodeCoverage]
public sealed class ScheduleContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    public IReadOnlyList<ScheduleInfoDto> Schedules { get; init; } = [];
    public IReadOnlyList<ScheduleInfoDto> FilteredSchedules { get; init; } = [];

    /// <summary>Gets the list of available schedule types loaded from the configuration API.</summary>
    public IReadOnlyList<ConfigurationTypeSummary> ScheduleTypes { get; init; } = [];

    public string SearchString { get; init; } = "";

    // ── Callbacks ──────────────────────────────────────────────────────────────

    public Func<Task> OnLoadData { get; init; } = () => Task.CompletedTask;
    public Func<string, Task<ScheduleInfoDto?>> OnGetScheduleDetails { get; init; } = _ => Task.FromResult<ScheduleInfoDto?>(null);
    public Func<string, bool, Task<bool>> OnToggleSchedule { get; init; } = (_, _) => Task.FromResult(false);
    public Func<CreateScheduleClientRequest, Task<bool>> OnCreateSchedule { get; init; } = _ => Task.FromResult(false);
    public Func<string, UpdateScheduleClientRequest, Task<bool>> OnUpdateSchedule { get; init; } = (_, _) => Task.FromResult(false);
    public Func<string, Task<bool>> OnDeleteSchedule { get; init; } = _ => Task.FromResult(false);
    public Func<string, Task> OnSearchChanged { get; init; } = _ => Task.CompletedTask;
}
