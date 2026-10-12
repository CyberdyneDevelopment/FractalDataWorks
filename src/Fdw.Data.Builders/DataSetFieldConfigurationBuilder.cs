using System;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Data.Builders.Results;

namespace Fdw.Data.Builders;

/// <summary>
/// Provides a fluent builder API for constructing <see cref="DataSetFieldConfiguration"/> instances.
/// </summary>
/// <remarks>
/// This builder implements a fluent interface for creating field configurations with validation.
/// It ensures all required properties are set before building the configuration instance.
/// </remarks>
public sealed class DataSetFieldConfigurationBuilder
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
    public DataSetFieldConfigurationBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the field description.
    /// </summary>
    /// <param name="description">A human-readable description of the field purpose.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetFieldConfigurationBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the field's data type to the one <c>DataTypes</c> option that reads a .NET type.
    /// </summary>
    /// <typeparam name="T">The .NET type of the field's values.</typeparam>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">No option, or more than one, reads <typeparamref name="T"/>.</exception>
    public DataSetFieldConfigurationBuilder WithType<T>() => WithType(typeof(T));

    /// <summary>
    /// Sets the field's data type to the one <c>DataTypes</c> option that reads a .NET type.
    /// </summary>
    /// <param name="type">The .NET type of the field's values.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">No option, or more than one, reads <paramref name="type"/>.</exception>
    public DataSetFieldConfigurationBuilder WithType(Type type)
    {
        _typeName = FieldDataType.NameFor(type);
        return this;
    }

    /// <summary>
    /// Sets the field's data type to a <c>DataTypes</c> option.
    /// </summary>
    /// <param name="dataType">The option, such as <c>DataTypes.Int32</c>.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetFieldConfigurationBuilder WithDataType(IDataType dataType)
    {
        if (dataType == null)
        {
            throw new ArgumentNullException(nameof(dataType));
        }
        _typeName = dataType.Name;
        return this;
    }

    /// <summary>
    /// Sets the field's data type by name.
    /// </summary>
    /// <param name="typeName">The name of a <c>DataTypes</c> option, such as <c>Int32</c> or <c>String</c>.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks><see cref="Build"/> refuses a name that is not an option's.</remarks>
    public DataSetFieldConfigurationBuilder WithTypeName(string typeName)
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
    public DataSetFieldConfigurationBuilder AsKey()
    {
        _isKey = true;
        _isRequired = true;
        return this;
    }

    /// <summary>
    /// Marks the field as required (non-nullable).
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetFieldConfigurationBuilder AsRequired()
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
    public DataSetFieldConfigurationBuilder AsOptional()
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
    public DataSetFieldConfigurationBuilder AsIndexed()
    {
        _isIndexed = true;
        return this;
    }

    /// <summary>
    /// Sets the maximum length for string fields.
    /// </summary>
    /// <param name="maxLength">The maximum allowed length for string values.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetFieldConfigurationBuilder WithMaxLength(int maxLength)
    {
        _maxLength = maxLength;
        return this;
    }

    /// <summary>
    /// Sets the default value for the field.
    /// </summary>
    /// <param name="defaultValue">The default value as a string representation.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetFieldConfigurationBuilder WithDefaultValue(string? defaultValue)
    {
        _defaultValue = defaultValue;
        return this;
    }

    /// <summary>
    /// Sets the default value for the field from an object.
    /// </summary>
    /// <param name="defaultValue">The default value which will be converted to string.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetFieldConfigurationBuilder WithDefaultValue(object? defaultValue)
    {
        _defaultValue = defaultValue?.ToString();
        return this;
    }

    /// <summary>
    /// Builds a <see cref="DataSetFieldConfiguration"/> instance from the configured values.
    /// </summary>
    /// <returns>
    /// A result containing the constructed <see cref="DataSetFieldConfiguration"/> if validation succeeds,
    /// or a failure result with error details if validation fails.
    /// </returns>
    public IGenericResult<DataSetFieldConfiguration> Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            return GenericResult<DataSetFieldConfiguration>.Failure(BuilderResultCodes.ByName("FieldNameRequired"));
        }

        if (string.IsNullOrWhiteSpace(_typeName))
        {
            return GenericResult<DataSetFieldConfiguration>.Failure(BuilderResultCodes.ByName("FieldTypeRequired"));
        }

        if (DataTypeNames.Find(_typeName) is null)
        {
            return GenericResult<DataSetFieldConfiguration>.Failure(
                BuilderResultCodes.ByName("FieldTypeUnknown"),
                ResultDetails.Create().With("FieldName", _name).With("DataType", _typeName));
        }

        if (_maxLength.HasValue && _maxLength.Value <= 0)
        {
            return GenericResult<DataSetFieldConfiguration>.Failure(
                BuilderResultCodes.ByName("FieldInvalidMaxLength"),
                ResultDetails.Create().With("FieldName", _name));
        }

        var config = new DataSetFieldConfiguration
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

        return GenericResult<DataSetFieldConfiguration>.Success(config);
    }

    /// <summary>
    /// Resets the builder to its initial state, clearing all configured values.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetFieldConfigurationBuilder Reset()
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
