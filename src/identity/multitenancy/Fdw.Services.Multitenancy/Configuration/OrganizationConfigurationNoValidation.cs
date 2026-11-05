using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Multitenancy.Configuration;

/// <summary>States that <see cref="OrganizationImplementationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "OrganizationImplementationConfiguration")]
public sealed class OrganizationConfigurationNoValidation : UnvalidatedConfiguration<OrganizationImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OrganizationConfigurationNoValidation"/> class.</summary>
    public OrganizationConfigurationNoValidation()
        : base("OrganizationImplementationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
