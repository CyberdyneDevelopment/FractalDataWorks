using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Messages;

/// <summary>Informational messages that provide context or status updates.</summary>
[TypeOption(typeof(MessageSeverities), "Information")]
[ExcludeFromCodeCoverage]
public sealed class InformationMessageSeverity : MessageSeverityBase
{
    /// <summary>Initializes a new instance of <see cref="InformationMessageSeverity"/>.</summary>
    public InformationMessageSeverity() : base(1, "Information") { }
}
