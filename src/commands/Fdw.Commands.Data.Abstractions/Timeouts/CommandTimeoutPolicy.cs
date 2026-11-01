using System;
using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions.Timeouts;

/// <summary>
/// Typed accessor over <see cref="IDataCommand.Metadata"/> for the timeout a command carries (FDW-3324), as <c>CachePolicy</c> is for caching.
/// </summary>
/// <remarks>
/// Why the command carries it: a connection gives every command its own command timeout, but a caller that sends a command for its own
/// purpose, such as a secret manager reading its table, has a limit of its own that the connection cannot know. The limit travels with the
/// command, and every connection kind applies it to the command it translates it into or refuses the command (<c>ConnectionBase</c>). There is
/// no default: a command that carries none is run with its connection's timeout.
/// </remarks>
public static class CommandTimeoutPolicy
{
    /// <summary>Metadata key for the timeout (<see cref="TimeSpan"/>, longer than zero).</summary>
    public const string CommandTimeoutKey = "CommandTimeout";

    /// <summary>Metadata key for who set the limit, in the words a failure uses (<see cref="string"/>), for example <c>secret manager 'MsSqlSecrets'</c>.</summary>
    public const string CommandTimeoutOwnerKey = "CommandTimeoutOwner";

    /// <summary>Writes a command timeout and its owner into a command's metadata.</summary>
    /// <param name="metadata">The metadata a command is built with.</param>
    /// <param name="timeout">The timeout, longer than zero.</param>
    /// <param name="owner">Who set the limit, in the words a failure uses; not empty.</param>
    public static void Set(IDictionary<string, object> metadata, TimeSpan timeout, string owner)
    {
        if (metadata == null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "A command timeout is longer than zero.");
        }

        if (string.IsNullOrWhiteSpace(owner))
        {
            throw new ArgumentException("A command timeout names who set it.", nameof(owner));
        }

        metadata[CommandTimeoutKey] = timeout;
        metadata[CommandTimeoutOwnerKey] = owner;
    }

    /// <summary>Reads the timeout a command carries.</summary>
    /// <param name="command">The command.</param>
    /// <param name="timeout">The timeout, when the command carries one.</param>
    /// <param name="owner">Who set the limit, when the command carries a timeout and an owner; otherwise empty.</param>
    /// <returns><see langword="true"/> when the command carries a timeout longer than zero.</returns>
    public static bool TryGet(IDataCommand command, out TimeSpan timeout, out string owner)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        // A command with no metadata carries no timeout; nothing is assumed in its place.
        var metadata = command.Metadata;
        owner = metadata != null && metadata.TryGetValue(CommandTimeoutOwnerKey, out var named) && named is string name ? name : string.Empty;
        if (metadata != null && metadata.TryGetValue(CommandTimeoutKey, out var carried) && carried is TimeSpan limit && limit > TimeSpan.Zero)
        {
            timeout = limit;
            return true;
        }

        timeout = TimeSpan.Zero;
        return false;
    }
}
