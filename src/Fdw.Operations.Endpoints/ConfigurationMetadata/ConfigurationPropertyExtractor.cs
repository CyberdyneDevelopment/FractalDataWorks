using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Operations.Endpoints.ConfigurationMetadata.PropertyTypes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Extracts property metadata from configuration CLR types for dynamic form generation.
/// </summary>
public static class ConfigurationPropertyExtractor
{
    private static readonly HashSet<string> SystemProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "Type", "ServiceType"
    };

    /// <summary>
    /// Extracts property metadata from a configuration type for UI rendering.
    /// </summary>
    public static IReadOnlyList<ConfigurationPropertyInfoDto> ExtractProperties(Type configurationType)
    {
        var properties = configurationType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => !SystemProperties.Contains(p.Name))
            .OrderBy(p => GetDisplayOrder(p))
            .Select((p, idx) => MapProperty(p, idx))
            .ToList();

        return properties;
    }

    private static int GetDisplayOrder(PropertyInfo property)
    {
        var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
        return displayAttr?.Order ?? 1000;
    }

    private static ConfigurationPropertyInfoDto MapProperty(PropertyInfo property, int defaultOrder)
    {
        var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
        var descAttr = property.GetCustomAttribute<DescriptionAttribute>();
        var requiredAttr = property.GetCustomAttribute<RequiredAttribute>();

        var propertyType = ConfigurationPropertyTypes.Convert(property);
        var allowedValues = GetAllowedValues(property);
        var validationRules = GetValidationRules(property);

        return new ConfigurationPropertyInfoDto
        {
            Name = property.Name,
            DisplayName = displayAttr?.Name ?? FormatDisplayName(property.Name),
            PropertyType = propertyType,
            IsRequired = requiredAttr != null || IsRequiredByType(property.PropertyType),
            IsSecret = propertyType == ConfigurationPropertyTypeDtos.Secret,
            DefaultValue = GetDefaultValue(property),
            Description = descAttr?.Description ?? displayAttr?.Description,
            Placeholder = displayAttr?.Prompt,
            Group = displayAttr?.GroupName,
            DisplayOrder = displayAttr?.Order ?? defaultOrder,
            ValidationRules = validationRules.Count > 0 ? validationRules : null,
            AllowedValues = allowedValues
        };
    }

    private static bool IsRequiredByType(Type type) =>
        type.IsValueType && Nullable.GetUnderlyingType(type) == null && type != typeof(bool);

    private static List<string>? GetAllowedValues(PropertyInfo property)
    {
        var valuesFromAttr = property.GetCustomAttribute<ValuesFromAttribute>();
        if (valuesFromAttr != null)
        {
            var names = GetValuesFromTypeCollection(valuesFromAttr);
            if (names != null)
                return names;
        }

        var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (!underlyingType.IsEnum)
            return null;

        return Enum.GetNames(underlyingType).ToList();
    }

    private static List<string>? GetValuesFromTypeCollection(ValuesFromAttribute attribute)
    {
        var collectionType = attribute.TypeCollectionType;
        if (collectionType == null)
            return null;

        var allMethod = collectionType.GetMethod("All", BindingFlags.Public | BindingFlags.Static);
        if (allMethod == null)
            return null;

        var items = allMethod.Invoke(null, null);
        if (items == null)
            return null;

        var displayProperty = attribute.DisplayProperty;
        var result = new List<string>();

        foreach (var item in (System.Collections.IEnumerable)items)
        {
            if (item is ITypeOption typeOption)
            {
                if (displayProperty != null)
                {
                    var propInfo = item.GetType().GetProperty(displayProperty, BindingFlags.Public | BindingFlags.Instance);
                    var displayValue = propInfo?.GetValue(item)?.ToString();
                    result.Add(displayValue ?? typeOption.Name);
                }
                else
                {
                    result.Add(typeOption.Name);
                }
            }
        }

        return result.Count > 0 ? result : null;
    }

    private static string? GetDefaultValue(PropertyInfo property)
    {
        var defaultAttr = property.GetCustomAttribute<DefaultValueAttribute>();
        return defaultAttr?.Value?.ToString();
    }

    private static List<ValidationRuleInfoDto> GetValidationRules(PropertyInfo property)
    {
        var rules = new List<ValidationRuleInfoDto>();

        var requiredAttr = property.GetCustomAttribute<RequiredAttribute>();
        if (requiredAttr != null)
        {
            rules.Add(new ValidationRuleInfoDto
            {
                RuleType = "Required",
                Message = requiredAttr.ErrorMessage ?? $"{property.Name} is required"
            });
        }

        var maxLengthAttr = property.GetCustomAttribute<MaxLengthAttribute>();
        if (maxLengthAttr != null)
        {
            rules.Add(new ValidationRuleInfoDto
            {
                RuleType = "MaxLength",
                Parameters = new Dictionary<string, object>(StringComparer.Ordinal) { ["max"] = maxLengthAttr.Length }
            });
        }

        var minLengthAttr = property.GetCustomAttribute<MinLengthAttribute>();
        if (minLengthAttr != null)
        {
            rules.Add(new ValidationRuleInfoDto
            {
                RuleType = "MinLength",
                Parameters = new Dictionary<string, object>(StringComparer.Ordinal) { ["min"] = minLengthAttr.Length }
            });
        }

        var rangeAttr = property.GetCustomAttribute<RangeAttribute>();
        if (rangeAttr != null)
        {
            rules.Add(new ValidationRuleInfoDto
            {
                RuleType = "Range",
                Parameters = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["min"] = rangeAttr.Minimum,
                    ["max"] = rangeAttr.Maximum
                }
            });
        }

        var regexAttr = property.GetCustomAttribute<RegularExpressionAttribute>();
        if (regexAttr != null)
        {
            rules.Add(new ValidationRuleInfoDto
            {
                RuleType = "Pattern",
                Message = regexAttr.ErrorMessage,
                Parameters = new Dictionary<string, object>(StringComparer.Ordinal) { ["pattern"] = regexAttr.Pattern }
            });
        }

        return rules;
    }

    private static string FormatDisplayName(string propertyName)
    {
        var result = System.Text.RegularExpressions.Regex.Replace(
            propertyName,
            "(?<!^)([A-Z])",
            " $1",
            System.Text.RegularExpressions.RegexOptions.ExplicitCapture,
            TimeSpan.FromSeconds(1));

        return result;
    }
}
