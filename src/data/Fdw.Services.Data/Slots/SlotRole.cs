using System;
using System.IO;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Slots;

/// <summary>
/// The role of this copy of an A/B slot app, read from the role file the slot file names (FDW-5846).
/// </summary>
/// <remarks>
/// The slot file (<see cref="SlotFile"/>) is read once, at <see cref="Create"/>, for the path of the role file; the role file is read on
/// every call to <see cref="BackgroundWorkMayRun"/>, because a swap rewrites it under a running copy. A host with no
/// <c>APP_SLOT_FILE</c> is not on a slot and its work is not gated. A host that is on a slot and whose slot file cannot be read or states
/// no <c>role_file</c> does not get a gate at all, so it does not start: nothing says it is live.
/// </remarks>
public sealed class SlotRole : ISlotRole
{
    /// <summary>The key of the slot file that holds the path of the role file.</summary>
    public const string RoleFileKey = "role_file";

    /// <summary>What the role file says when this copy is live.</summary>
    public const string Live = "live";

    /// <summary>What the role file says when this copy is standby.</summary>
    public const string Standby = "standby";

    private readonly string? _roleFile;
    private readonly ILogger _logger;

    private SlotRole(string? roleFile, ILogger logger)
    {
        _roleFile = roleFile;
        _logger = logger;
    }

    /// <summary>Builds the gate for the host this process is.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The gate, or why a host that is on a slot cannot have one.</returns>
    public static IGenericResult<ISlotRole> Create(ILogger logger)
    {
        if (SlotFile.DeclaredPath() is not { } declared)
        {
            SlotRoleLog.GateNotOnSlot(logger);
            return GenericResult<ISlotRole>.Success(new SlotRole(null, logger));
        }

        var read = SlotFile.Read(declared, logger);
        if (read.IsFailure || read.Value is not { } file)
            return read.ToNewResult<ISlotRole>();

        if (!file.Values.TryGetValue(RoleFileKey, out var roleFile))
            return GenericResult<ISlotRole>.Failure(SlotRoleLog.RoleFileNotStated(logger, file.FilePath));

        SlotRoleLog.GateOnSlot(logger, roleFile);
        return GenericResult<ISlotRole>.Success(new SlotRole(roleFile, logger));
    }

    /// <inheritdoc />
    public IGenericResult<bool> BackgroundWorkMayRun()
    {
        if (_roleFile is null)
            return GenericResult<bool>.Success(true);

        string content;
        try
        {
            content = File.ReadAllText(_roleFile).Trim();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return GenericResult<bool>.Failure(SlotRoleLog.RoleFileUnreadable(_logger, _roleFile, ex.Message));
        }

        SlotRoleLog.RoleRead(_logger, _roleFile, content);

        if (string.Equals(content, Live, StringComparison.Ordinal))
            return GenericResult<bool>.Success(true);

        return string.Equals(content, Standby, StringComparison.Ordinal)
            ? GenericResult<bool>.Success(false)
            : GenericResult<bool>.Failure(SlotRoleLog.RoleFileSaysUnknown(_logger, _roleFile, content));
    }
}
