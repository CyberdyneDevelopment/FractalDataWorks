using System;
using Fdw.Services.Scheduling.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Abstract base for scheduling commands. One command per scheduling operation, shared by every
/// implementation (Quartz, Hangfire, ...) — the command never varies per implementation, only the
/// translator that turns it into that implementation's native call does.
/// </summary>
/// <remarks>
/// Carries the scheduler's own <see cref="ISchedulerImplementationConfiguration.DataStoreName"/>/
/// <see cref="ISchedulerImplementationConfiguration.PathName"/>/
/// <see cref="ISchedulerImplementationConfiguration.ScheduleContainerName"/> — the service
/// constructing the command already holds its own configuration and stamps these on; nothing
/// downstream (a translator, or an execution callback resolved later with no configuration in
/// hand) needs to be handed the configuration separately or guess where to read or write.
/// </remarks>
public abstract class SchedulingCommandBase : ISchedulingCommand
{
    /// <summary>Initializes a new instance of the <see cref="SchedulingCommandBase"/> class.</summary>
    /// <param name="commandType">Name of the command type (e.g. "Create", "Pause").</param>
    /// <param name="dataStoreName">The connection the owning scheduler reads and writes.</param>
    /// <param name="pathName">The schema the owning scheduler reads and writes.</param>
    /// <param name="scheduleContainerName">The container the owning scheduler's schedules live in.</param>
    protected SchedulingCommandBase(string commandType, string dataStoreName, string pathName, string scheduleContainerName)
    {
        CommandId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        CommandType = commandType;
        Category = "Scheduling";
        DataStoreName = dataStoreName;
        PathName = pathName;
        ScheduleContainerName = scheduleContainerName;

        SchedulingLogger.CommandCreated(NullLogger<SchedulingCommandBase>.Instance, CommandId, CommandType, Category);
    }

    /// <inheritdoc />
    public Guid CommandId { get; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; }

    /// <inheritdoc />
    public string CommandType { get; }

    /// <inheritdoc />
    public string Category { get; }

    /// <summary>Gets the connection the owning scheduler reads and writes.</summary>
    public string DataStoreName { get; }

    /// <summary>Gets the schema the owning scheduler reads and writes.</summary>
    public string PathName { get; }

    /// <summary>Gets the container the owning scheduler's schedules live in.</summary>
    public string ScheduleContainerName { get; }
}
