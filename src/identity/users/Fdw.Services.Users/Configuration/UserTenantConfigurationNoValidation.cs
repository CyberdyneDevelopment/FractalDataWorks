using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Users.Configuration;

/// <summary>States that <see cref="UserTenantImplementationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "UserTenantImplementationConfiguration")]
public sealed class UserTenantConfigurationNoValidation : UnvalidatedConfiguration<UserTenantImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="UserTenantConfigurationNoValidation"/> class.</summary>
    public UserTenantConfigurationNoValidation()
        : base("UserTenantImplementationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
