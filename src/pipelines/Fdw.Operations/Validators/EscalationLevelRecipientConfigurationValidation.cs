using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>States that <see cref="EscalationLevelRecipientConfiguration"/> (and what derives from or implements it) is checked by <see cref="EscalationLevelRecipientConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "EscalationLevelRecipientConfiguration")]
public sealed class EscalationLevelRecipientConfigurationValidation : ValidatedConfiguration<EscalationLevelRecipientConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="EscalationLevelRecipientConfigurationValidation"/> class.</summary>
    public EscalationLevelRecipientConfigurationValidation()
        : base("EscalationLevelRecipientConfiguration", new EscalationLevelRecipientConfigurationValidator())
    {
    }
}
