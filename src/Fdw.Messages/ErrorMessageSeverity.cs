using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Messages;

/// <summary>Error messages that indicate failures or critical problems.</summary>
[TypeOption(typeof(MessageSeverities), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorMessageSeverity : MessageSeverityBase
{
    /// <summary>Initializes a new instance of <see cref="ErrorMessageSeverity"/>.</summary>
    public ErrorMessageSeverity() : base(3, "Error") { }
}
