using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.Validation;

/// <summary>States that <see cref="DataContainerFieldConfiguration"/> (and what derives from or implements it) is checked by <see cref="DataContainerFieldConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataContainerFieldConfiguration")]
public sealed class DataContainerFieldConfigurationValidation : ValidatedConfiguration<DataContainerFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataContainerFieldConfigurationValidation"/> class.</summary>
    public DataContainerFieldConfigurationValidation()
        : base("DataContainerFieldConfiguration", new DataContainerFieldConfigurationValidator())
    {
    }
}
