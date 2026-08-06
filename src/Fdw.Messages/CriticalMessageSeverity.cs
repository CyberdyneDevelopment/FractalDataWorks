using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Messages;

/// <summary>Critical messages that indicate system-level failures.</summary>
[TypeOption(typeof(MessageSeverities), "Critical")]
[ExcludeFromCodeCoverage]
public sealed class CriticalMessageSeverity : MessageSeverityBase
{
    /// <summary>Initializes a new instance of <see cref="CriticalMessageSeverity"/>.</summary>
    public CriticalMessageSeverity() : base(4, "Critical") { }
}
