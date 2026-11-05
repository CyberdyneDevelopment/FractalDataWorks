using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>States that <see cref="EscalationPolicyImplementationConfiguration"/> is checked by <see cref="EscalationPolicyConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "EscalationPolicyImplementationConfiguration")]
public sealed class EscalationPolicyConfigurationValidation : ValidatedConfiguration<EscalationPolicyImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="EscalationPolicyConfigurationValidation"/> class.</summary>
    public EscalationPolicyConfigurationValidation()
        : base("EscalationPolicyImplementationConfiguration", new EscalationPolicyConfigurationValidator())
    {
    }
}
