using System;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>
/// The four failures of one kind of timeout that <see cref="InstanceTimeout"/> resolves, each naming the timeout in its own words (FDW-3580).
/// </summary>
internal sealed class TimeoutMessages
{
    /// <summary>Initializes a new instance of the <see cref="TimeoutMessages"/> class.</summary>
    /// <param name="notPositive">The owner states a timeout that is not positive: logger, owner kind, owner name, seconds.</param>
    /// <param name="noSettings">The host registers no settings: logger, owner kind, owner name, setting.</param>
    /// <param name="readThroughItself">The settings are read through the owner being built: logger, owner kind, owner name, setting.</param>
    /// <param name="notStated">The setting is absent or not positive: logger, owner kind, owner name, setting.</param>
    public TimeoutMessages(
        Func<ILogger, string, string, int, IGenericMessage> notPositive,
        Func<ILogger, string, string, string, IGenericMessage> noSettings,
        Func<ILogger, string, string, string, IGenericMessage> readThroughItself,
        Func<ILogger, string, string, string, IGenericMessage> notStated)
    {
        NotPositive = notPositive;
        NoSettings = noSettings;
        ReadThroughItself = readThroughItself;
        NotStated = notStated;
    }

    /// <summary>Gets the failure for a stated timeout that is not positive.</summary>
    public Func<ILogger, string, string, int, IGenericMessage> NotPositive { get; }

    /// <summary>Gets the failure for a host that registers no settings.</summary>
    public Func<ILogger, string, string, string, IGenericMessage> NoSettings { get; }

    /// <summary>Gets the failure for settings that are read through the owner being built.</summary>
    public Func<ILogger, string, string, string, IGenericMessage> ReadThroughItself { get; }

    /// <summary>Gets the failure for a setting that is absent or not positive.</summary>
    public Func<ILogger, string, string, string, IGenericMessage> NotStated { get; }
}
