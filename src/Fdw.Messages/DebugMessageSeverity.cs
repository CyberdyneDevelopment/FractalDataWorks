using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Messages;

/// <summary>Debug-level messages for detailed diagnostic information.</summary>
[TypeOption(typeof(MessageSeverities), "Debug")]
[ExcludeFromCodeCoverage]
public sealed class DebugMessageSeverity : MessageSeverityBase
{
    /// <summary>Initializes a new instance of <see cref="DebugMessageSeverity"/>.</summary>
    public DebugMessageSeverity() : base(0, "Debug") { }
}
