using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Projects.Validation;

/// <summary>States that <see cref="ServerPolicyDefaultConfiguration"/> is checked by <see cref="ServerPolicyDefaultConfigurationValidator"/> when it is written (FDW-4052).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ServerPolicyDefaultConfiguration")]
public sealed class ServerPolicyDefaultConfigurationValidation : ValidatedConfiguration<ServerPolicyDefaultConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ServerPolicyDefaultConfigurationValidation"/> class.</summary>
    public ServerPolicyDefaultConfigurationValidation()
        : base("ServerPolicyDefaultConfiguration", new ServerPolicyDefaultConfigurationValidator())
    {
    }
}
