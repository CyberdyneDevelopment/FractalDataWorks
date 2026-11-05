using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>States that <see cref="RolePermissionImplementationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "RolePermissionImplementationConfiguration")]
public sealed class RolePermissionConfigurationNoValidation : UnvalidatedConfiguration<RolePermissionImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="RolePermissionConfigurationNoValidation"/> class.</summary>
    public RolePermissionConfigurationNoValidation()
        : base("RolePermissionImplementationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
