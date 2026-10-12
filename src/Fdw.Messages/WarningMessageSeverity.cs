using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Messages;

/// <summary>Warning messages that indicate potential issues but don't prevent operation.</summary>
[TypeOption(typeof(MessageSeverities), "Warning")]
[ExcludeFromCodeCoverage]
public sealed class WarningMessageSeverity : MessageSeverityBase
{
    /// <summary>Initializes a new instance of <see cref="WarningMessageSeverity"/>.</summary>
    public WarningMessageSeverity() : base(2, "Warning") { }
}
