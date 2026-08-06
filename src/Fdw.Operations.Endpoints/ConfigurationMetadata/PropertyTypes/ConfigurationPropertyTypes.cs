using System;
using System.Collections.Generic;
using System.Reflection;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

/// <summary>
/// TypeCollection for configuration property type mappings.
/// Maps .NET types to UI property type descriptors via ByDataType() lookup.
/// </summary>
[TypeCollection(typeof(ConfigurationPropertyTypeBaseResponse), typeof(IConfigurationPropertyType), typeof(ConfigurationPropertyTypes), RestrictToCurrentCompilation = true)]
public abstract partial class ConfigurationPropertyTypes : TypeCollectionBase<ConfigurationPropertyTypeBaseResponse, IConfigurationPropertyType>
{
    private static readonly HashSet<string> SecretPropertyPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "Secret", "ApiKey", "Credential"
    };

    /// <summary>
    /// Converts a property to its UI property type descriptor.
    /// Handles name-based patterns (secret, connection) then delegates to type-based lookup.
    /// </summary>
    public static IConfigurationPropertyTypeDto Convert(PropertyInfo property)
    {
        if (IsSecretProperty(property.Name))
            return ConfigurationPropertyTypeDtos.Secret;

        if (property.Name.EndsWith("ConnectionName", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(property.Name, "Connection", StringComparison.OrdinalIgnoreCase))
            return ConfigurationPropertyTypeDtos.Connection;

        return Convert(property.PropertyType);
    }

    /// <summary>
    /// Converts a .NET type to its UI property type descriptor.
    /// Unwraps nullable, checks for enum, then performs TypeCollection lookup.
    /// </summary>
    public static IConfigurationPropertyTypeDto Convert(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType.IsEnum)
            return ConfigurationPropertyTypeDtos.Enum;

        var match = ByDataType(underlyingType);
        return match.Id == 0 ? ConfigurationPropertyTypeDtos.Text : match.DtoValue;
    }

    private static bool IsSecretProperty(string propertyName)
    {
        foreach (var pattern in SecretPropertyPatterns)
        {
            if (propertyName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
