using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.Validation;

/// <summary>States that <see cref="DataContainerConfiguration"/> (and what derives from or implements it) is checked by <see cref="DataContainerConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataContainerConfiguration")]
public sealed class DataContainerConfigurationValidation : ValidatedConfiguration<DataContainerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataContainerConfigurationValidation"/> class.</summary>
    public DataContainerConfigurationValidation()
        : base("DataContainerConfiguration", new DataContainerConfigurationValidator())
    {
    }
}
