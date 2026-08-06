using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that modified timestamp is earlier than created timestamp.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("InvalidTimestamp")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class InvalidTimestampMessage : SchedulingMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidTimestampMessage"/> class.
    /// </summary>
    public InvalidTimestampMessage()
        : base(1005, "InvalidTimestamp", MessageSeverity.Error,
               "Modified timestamp cannot be earlier than created timestamp", "SCHED_INVALID_TIMESTAMP")
    { }
}
