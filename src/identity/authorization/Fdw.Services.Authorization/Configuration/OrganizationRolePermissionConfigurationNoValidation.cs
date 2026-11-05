using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>States that <see cref="OrganizationRolePermissionConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "OrganizationRolePermissionConfiguration")]
public sealed class OrganizationRolePermissionConfigurationNoValidation : UnvalidatedConfiguration<OrganizationRolePermissionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OrganizationRolePermissionConfigurationNoValidation"/> class.</summary>
    public OrganizationRolePermissionConfigurationNoValidation()
        : base("OrganizationRolePermissionConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
