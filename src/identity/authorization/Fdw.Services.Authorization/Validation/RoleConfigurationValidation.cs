using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Authorization.Validation;

/// <summary>States that <see cref="RoleImplementationConfiguration"/> is checked by <see cref="RoleConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "RoleImplementationConfiguration")]
public sealed class RoleConfigurationValidation : ValidatedConfiguration<RoleImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="RoleConfigurationValidation"/> class.</summary>
    public RoleConfigurationValidation()
        : base("RoleImplementationConfiguration", new RoleConfigurationValidator())
    {
    }
}
