using System.Collections.Generic;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// A transformation of one column: a <see cref="Fdw.Data.Abstractions.TransformationTypes"/> operation
/// and the values of the parameters it declares. Only these attach to field mappings.
/// </summary>
public interface IFieldTransformationConfiguration : ITransformationImplementationConfiguration
{
    /// <summary>Gets or sets the operation: the name of a <see cref="Fdw.Data.Abstractions.TransformationTypes"/> option.</summary>
    string TransformationType { get; set; }

    /// <summary>Gets or sets the values of the parameters the operation declares.</summary>
    IList<FieldTransformationParameterConfiguration> Parameters { get; set; }
}
