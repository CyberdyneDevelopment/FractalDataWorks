using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Email: The recipient is an email address
/// </summary>
[TypeOption(typeof(EscalationRecipientTypes), "Email", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EmailEscalationRecipientType : EscalationRecipientTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailEscalationRecipientType"/> class.
    /// </summary>
    public EmailEscalationRecipientType()
        : base(name: "Email", description: "The recipient is an email address")
    {
    }
}
