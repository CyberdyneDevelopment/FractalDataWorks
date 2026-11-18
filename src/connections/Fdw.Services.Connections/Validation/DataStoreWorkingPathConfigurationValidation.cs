using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.Validation;

/// <summary>States that <see cref="DataStoreWorkingPathConfiguration"/> is validated by <see cref="DataStoreWorkingPathConfigurationValidator"/>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataStoreWorkingPathConfiguration")]
public sealed class DataStoreWorkingPathConfigurationValidation : ValidatedConfiguration<DataStoreWorkingPathConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataStoreWorkingPathConfigurationValidation"/> class.</summary>
    public DataStoreWorkingPathConfigurationValidation()
        : base("DataStoreWorkingPathConfiguration", new DataStoreWorkingPathConfigurationValidator())
    {
    }
}
