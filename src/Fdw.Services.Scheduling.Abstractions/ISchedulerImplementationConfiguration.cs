using Fdw.Configuration;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// The configuration a scheduler is resolved against.
/// </summary>
/// <remarks>
/// It lives here rather than beside its class because a contract in this package cannot name a type in
/// the core package; the dependency runs the other way. Declaring it is what lets
/// <see cref="ISchedulerServiceProvider"/> name its configuration at all.
/// <para>
/// Scheduling is single-level: a scheduler's configuration is one record, not a list holding typed
/// members, so this one interface serves the whole domain.
/// </para>
/// </remarks>
public interface ISchedulerImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the container the scheduler reads its schedules from.</summary>
    string ScheduleContainerName { get; set; }

    /// <summary>Gets or sets the data store the scheduler reads from.</summary>
    string DataStoreName { get; set; }

    /// <summary>Gets or sets the schema the scheduler reads from.</summary>
    string PathName { get; set; }
}
