using System;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// Base class for configuration property type options.
/// Uses CRTP pattern with TypeLookup on DataType for O(1) type-based resolution.
/// </summary>
public abstract class ConfigurationPropertyTypeBaseResponse : TypeOptionBase<int, ConfigurationPropertyTypeBaseResponse>, IConfigurationPropertyType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationPropertyTypeBaseResponse"/> class.
    /// </summary>
    protected ConfigurationPropertyTypeBaseResponse(int id, string name, Type dataType, IConfigurationPropertyTypeDto dtoValue)
        : base(id, name)
    {
        DataType = dataType;
        DtoValue = dtoValue;
    }

    /// <summary>
    /// Parameterless constructor for source-generator Empty sentinel.
    /// </summary>
    protected ConfigurationPropertyTypeBaseResponse()
        : base(0, "NotFound")
    {
        DataType = typeof(object);
        DtoValue = ConfigurationPropertyTypeDtos.Text;
    }

    /// <summary>
    /// Gets the .NET type this property type maps from.
    /// </summary>
    [TypeLookup("ByDataType")]
    public Type DataType { get; }

    /// <summary>
    /// Gets the DTO enum value for serialization to the UI.
    /// </summary>
    public IConfigurationPropertyTypeDto DtoValue { get; }
}
