using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections;

/// <summary>States that <see cref="DataContainerKeyFieldConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataContainerKeyFieldConfiguration")]
public sealed class DataContainerKeyFieldConfigurationNoValidation : UnvalidatedConfiguration<DataContainerKeyFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataContainerKeyFieldConfigurationNoValidation"/> class.</summary>
    public DataContainerKeyFieldConfigurationNoValidation()
        : base("DataContainerKeyFieldConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
