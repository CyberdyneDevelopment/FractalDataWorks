using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Users.Configuration;

/// <summary>States that <see cref="UsersServiceImplementationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "UsersServiceImplementationConfiguration")]
public sealed class UsersServiceConfigurationNoValidation : UnvalidatedConfiguration<UsersServiceImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="UsersServiceConfigurationNoValidation"/> class.</summary>
    public UsersServiceConfigurationNoValidation()
        : base("UsersServiceImplementationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
