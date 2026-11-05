using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>States that <see cref="EscalationLevelConfiguration"/> (and what derives from or implements it) is checked by <see cref="EscalationLevelConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "EscalationLevelConfiguration")]
public sealed class EscalationLevelConfigurationValidation : ValidatedConfiguration<EscalationLevelConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="EscalationLevelConfigurationValidation"/> class.</summary>
    public EscalationLevelConfigurationValidation()
        : base("EscalationLevelConfiguration", new EscalationLevelConfigurationValidator())
    {
    }
}
