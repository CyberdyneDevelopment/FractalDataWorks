using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Quality.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>States that <see cref="DataSetAnnotationImplementationConfiguration"/> is checked by <see cref="DataSetAnnotationConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataSetAnnotationImplementationConfiguration")]
public sealed class DataSetAnnotationConfigurationValidation : ValidatedConfiguration<DataSetAnnotationImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetAnnotationConfigurationValidation"/> class.</summary>
    public DataSetAnnotationConfigurationValidation()
        : base("DataSetAnnotationImplementationConfiguration", new DataSetAnnotationConfigurationValidator())
    {
    }
}
