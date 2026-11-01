using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// Message cannot be empty.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "EmptyMessage", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EmptyMessageCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyMessageCode"/> class.
    /// </summary>
    public EmptyMessageCode()
        : base(20000, "EmptyMessage",
            ResultSeverities.ByName("Error"),
            "Message cannot be empty",
            isRetryable: false)
    {
    }
}