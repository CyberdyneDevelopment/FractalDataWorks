using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// Command type is not supported.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "UnsupportedCommand", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnsupportedCommandCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedCommandCode"/> class.
    /// </summary>
    public UnsupportedCommandCode()
        : base(90004, "UnsupportedCommand",
            ResultSeverities.ByName("Error"),
            "Command type {CommandType} is not supported",
            isRetryable: false)
    {
    }
}