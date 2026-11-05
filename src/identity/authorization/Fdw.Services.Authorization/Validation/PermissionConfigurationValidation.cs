using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Authorization.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Authorization.Validation;

/// <summary>States that <see cref="PermissionImplementationConfiguration"/> is checked by <see cref="PermissionConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PermissionImplementationConfiguration")]
public sealed class PermissionConfigurationValidation : ValidatedConfiguration<PermissionImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PermissionConfigurationValidation"/> class.</summary>
    public PermissionConfigurationValidation()
        : base("PermissionImplementationConfiguration", new PermissionConfigurationValidator())
    {
    }
}
