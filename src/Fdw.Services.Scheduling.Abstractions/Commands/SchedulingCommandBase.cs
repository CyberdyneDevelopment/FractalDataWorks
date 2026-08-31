using System;
using Fdw.Services.Scheduling.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Abstract base for scheduling commands. One command per scheduling operation, shared by every
/// implementation (Quartz, Hangfire, ...) — the command never varies per implementation, only the
/// translator that turns it into that implementation's native call does.
/// </summary>
public abstract class SchedulingCommandBase : ISchedulingCommand
{
    /// <summary>Initializes a new instance of the <see cref="SchedulingCommandBase"/> class.</summary>
    /// <param name="commandType">Name of the command type (e.g. "Create", "Pause").</param>
    protected SchedulingCommandBase(string commandType)
    {
        CommandId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        CommandType = commandType;
        Category = "Scheduling";

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
}
