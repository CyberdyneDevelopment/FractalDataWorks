using System;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Data.Builders.Results;

namespace Fdw.Data.Builders;

/// <summary>
/// Provides a fluent builder API for constructing <see cref="DataFieldConfiguration"/> instances.
/// </summary>
/// <remarks>
/// This builder implements a fluent interface for creating field configurations with validation.
/// It ensures all required properties are set before building the configuration instance.
/// </remarks>
public sealed class DataFieldConfigurationBuilder
{
    private string? _name;
    private string? _description;
    private string? _typeName;
    private bool _isKey;
    private bool _isRequired;
    private bool _isIndexed;
    private int? _maxLength;
    private string? _defaultValue;

    /// <summary>
    /// Sets the field name.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataFieldConfigurationBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the field description.
    /// </summary>
    /// <param name="description">A human-readable description of the field purpose.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataFieldConfigurationBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the field type using a generic type parameter.
    /// </summary>
    /// <typeparam name="T">The .NET type of the field.</typeparam>
    /// <returns>This builder instance for method chaining.</returns>
    public DataFieldConfigurationBuilder WithType<T>()
    {
        _typeName = typeof(T).FullName ?? typeof(T).Name;
        return this;
    }

    /// <summary>
    /// Sets the field type using a Type instance.
    /// </summary>
    /// <param name="type">The .NET type of the field.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataFieldConfigurationBuilder WithType(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }
        _typeName = type.FullName ?? type.Name;
        return this;
    }

    /// <summary>
    /// Sets the field type using a type name string.
    /// </summary>
    /// <param name="typeName">The fully qualified .NET type name.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataFieldConfigurationBuilder WithTypeName(string typeName)
    {
        _typeName = typeName;
        return this;
    }

    /// <summary>
    /// Marks the field as part of the primary key.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// Key fields are automatically marked as required.
    /// </remarks>
    public DataFieldConfigurationBuilder AsKey()
    {
        _isKey = true;
        _isRequired = true;
        return this;
    }

    /// <summary>
    /// Marks the field as required (non-nullable).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public DataFieldConfigurationBuilder AsRequired()
    {
        _isRequired = true;
        return this;
    }

    /// <summary>
    /// Marks the field as optional (nullable).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// Key fields cannot be optional and will remain required.
    /// </remarks>
    public DataFieldConfigurationBuilder AsOptional()
    {
        if (!_isKey)
        {
            _isRequired = false;
        }
        return this;
    }

    /// <summary>
    /// Marks the field as indexed for searching and filtering.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public DataFieldConfigurationBuilder AsIndexed()
    {
        _isIndexed = true;
        return this;
    }

    /// <summary>
    /// Sets the maximum length for string fields.
    /// </summary>
    /// <param name="maxLength">The maximum allowed length for string values.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataFieldConfigurationBuilder WithMaxLength(int maxLength)
    {
        _maxLength = maxLength;
        return this;
    }

    /// <summary>
    /// Sets the default value for the field.
    /// </summary>
    /// <param name="defaultValue">The default value as a string representation.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataFieldConfigurationBuilder WithDefaultValue(string? defaultValue)
    {
        _defaultValue = defaultValue;
        return this;
    }

    /// <summary>
    /// Sets the default value for the field from an object.
    /// </summary>
    /// <param name="defaultValue">The default value which will be converted to string.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataFieldConfigurationBuilder WithDefaultValue(object? defaultValue)
    {
        _defaultValue = defaultValue?.ToString();
        return this;
    }

    /// <summary>
    /// Builds a <see cref="DataFieldConfiguration"/> instance from the configured values.
    /// </summary>
    /// <returns>
    /// A result containing the constructed <see cref="DataFieldConfiguration"/> if validation succeeds,
    /// or a failure result with error details if validation fails.
    /// </returns>
    public IGenericResult<DataFieldConfiguration> Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            return GenericResult<DataFieldConfiguration>.Failure(BuilderResultCodes.ByName("FieldNameRequired"));
        }

        if (string.IsNullOrWhiteSpace(_typeName))
        {
            return GenericResult<DataFieldConfiguration>.Failure(BuilderResultCodes.ByName("FieldTypeRequired"));
        }

        if (_maxLength.HasValue && _maxLength.Value <= 0)
        {
            return GenericResult<DataFieldConfiguration>.Failure(
                BuilderResultCodes.ByName("FieldInvalidMaxLength"),
                ResultDetails.Create().With("FieldName", _name));
        }

        var config = new DataFieldConfiguration
        {
            Name = _name,
            Description = _description,
            TypeName = _typeName,
            IsKey = _isKey,
            IsRequired = _isRequired,
            IsIndexed = _isIndexed,
            MaxLength = _maxLength,
            DefaultValue = _defaultValue
        };

        return GenericResult<DataFieldConfiguration>.Success(config);
    }

    /// <summary>
    /// Resets the builder to its initial state, clearing all configured values.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public DataFieldConfigurationBuilder Reset()
    {
        _name = null;
        _description = null;
        _typeName = null;
        _isKey = false;
        _isRequired = false;
        _isIndexed = false;
        _maxLength = null;
        _defaultValue = null;
        return this;
    }
}
