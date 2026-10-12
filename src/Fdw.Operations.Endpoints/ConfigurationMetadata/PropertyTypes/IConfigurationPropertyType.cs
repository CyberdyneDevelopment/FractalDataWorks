using System;
using Fdw.Collections;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Interface for configuration property type mapping.
/// Maps .NET types to UI property type descriptors.
/// </summary>
public interface IConfigurationPropertyType : ITypeOption<int, ConfigurationPropertyTypeBaseResponse>
{
    /// <summary>
    /// Gets the .NET type this property type maps from.
    /// </summary>
    Type DataType { get; }

    /// <summary>
    /// Gets the DTO enum value for serialization to the UI.
    /// </summary>
    IConfigurationPropertyTypeDto DtoValue { get; }
}
