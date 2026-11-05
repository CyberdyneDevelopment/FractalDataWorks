using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.Validation;

/// <summary>States that <see cref="DataPathConfiguration"/> (and what derives from or implements it) is checked by <see cref="DataPathConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataPathConfiguration")]
public sealed class DataPathConfigurationValidation : ValidatedConfiguration<DataPathConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataPathConfigurationValidation"/> class.</summary>
    public DataPathConfigurationValidation()
        : base("DataPathConfiguration", new DataPathConfigurationValidator())
    {
    }
}
