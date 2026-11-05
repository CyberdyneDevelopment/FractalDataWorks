using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.Validation;

/// <summary>States that <see cref="DataStoreImplementationConfiguration"/> is checked by <see cref="DataStoreConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataStoreImplementationConfiguration")]
public sealed class DataStoreConfigurationValidation : ValidatedConfiguration<DataStoreImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataStoreConfigurationValidation"/> class.</summary>
    public DataStoreConfigurationValidation()
        : base("DataStoreImplementationConfiguration", new DataStoreConfigurationValidator())
    {
    }
}
