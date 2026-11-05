using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Quality.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>States that <see cref="DataSetAnnotationFieldBusinessNameConfiguration"/> (and what derives from or implements it) is checked by <see cref="DataSetAnnotationFieldBusinessNameConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataSetAnnotationFieldBusinessNameConfiguration")]
public sealed class DataSetAnnotationFieldBusinessNameConfigurationValidation : ValidatedConfiguration<DataSetAnnotationFieldBusinessNameConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetAnnotationFieldBusinessNameConfigurationValidation"/> class.</summary>
    public DataSetAnnotationFieldBusinessNameConfigurationValidation()
        : base("DataSetAnnotationFieldBusinessNameConfiguration", new DataSetAnnotationFieldBusinessNameConfigurationValidator())
    {
    }
}
