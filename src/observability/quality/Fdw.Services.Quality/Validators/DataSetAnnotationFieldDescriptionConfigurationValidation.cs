using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Quality.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>States that <see cref="DataSetAnnotationFieldDescriptionConfiguration"/> (and what derives from or implements it) is checked by <see cref="DataSetAnnotationFieldDescriptionConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataSetAnnotationFieldDescriptionConfiguration")]
public sealed class DataSetAnnotationFieldDescriptionConfigurationValidation : ValidatedConfiguration<DataSetAnnotationFieldDescriptionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetAnnotationFieldDescriptionConfigurationValidation"/> class.</summary>
    public DataSetAnnotationFieldDescriptionConfigurationValidation()
        : base("DataSetAnnotationFieldDescriptionConfiguration", new DataSetAnnotationFieldDescriptionConfigurationValidator())
    {
    }
}
