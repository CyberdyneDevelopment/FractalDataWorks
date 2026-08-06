using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Data.Builders.Results;

namespace Fdw.Data.Builders;

/// <summary>
/// Provides a fluent builder API for constructing <see cref="DataSetConfiguration"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This builder implements a fluent interface for creating dataset configurations with validation.
/// It ensures all required properties are set and validates the configuration before building.
/// </para>
/// <para>
/// Source configurations are created as separate <see cref="DataSetSourceConfiguration"/> entities
/// and can be accessed via <see cref="SourceConfigurations"/> after building. The caller is
/// responsible for persisting these source configurations separately from the DataSet.
/// </para>
/// </remarks>
public sealed class DataSetConfigurationBuilder
{
    private Guid _dataSetId = Guid.NewGuid();
    private string? _dataSetName;
    private string? _description;
    private string _version = "1.0";
    private string _category = "Dataset";
    private string? _recordTypeName;
    private readonly List<DataFieldConfiguration> _fields = new();
    private readonly List<string> _keyFields = new();
    private readonly List<DataSetSourceConfiguration> _sources = new();
    private CachingConfiguration? _caching;

    /// <summary>
    /// Gets the source configurations created by this builder.
    /// </summary>
    /// <remarks>
    /// These are populated after <see cref="Build"/> is called. The caller is responsible
    /// for persisting these configurations separately from the DataSetConfiguration.
    /// </remarks>
    public IReadOnlyList<DataSetSourceConfiguration> SourceConfigurations => _sources;

    /// <summary>
    /// Sets the DataSet ID. If not called, a new GUID is generated.
    /// </summary>
    /// <param name="id">The unique identifier for the dataset.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder WithId(Guid id)
    {
        _dataSetId = id;
        return this;
    }

    /// <summary>
    /// Sets the dataset name.
    /// </summary>
    /// <param name="name">The unique name identifier for the dataset.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder WithName(string name)
    {
        _dataSetName = name;
        return this;
    }

    /// <summary>
    /// Sets the dataset description.
    /// </summary>
    /// <param name="description">A detailed description of the dataset purpose and content.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the schema version.
    /// </summary>
    /// <param name="version">The schema version string (default: "1.0").</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder WithVersion(string version)
    {
        _version = version ?? "1.0";
        return this;
    }

    /// <summary>
    /// Sets the category for grouping.
    /// </summary>
    /// <param name="category">The category name for organizational purposes (default: "Dataset").</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder WithCategory(string category)
    {
        _category = category ?? "Dataset";
        return this;
    }

    /// <summary>
    /// Sets the record type using a generic type parameter.
    /// </summary>
    /// <typeparam name="TRecord">The .NET type of records in this dataset.</typeparam>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder WithRecordType<TRecord>()
    {
        _recordTypeName = typeof(TRecord).AssemblyQualifiedName ?? typeof(TRecord).FullName ?? typeof(TRecord).Name;
        return this;
    }

    /// <summary>
    /// Sets the record type using a Type instance.
    /// </summary>
    /// <param name="recordType">The .NET type of records in this dataset.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder WithRecordType(Type recordType)
    {
        if (recordType == null)
        {
            throw new ArgumentNullException(nameof(recordType));
        }
        _recordTypeName = recordType.AssemblyQualifiedName ?? recordType.FullName ?? recordType.Name;
        return this;
    }

    /// <summary>
    /// Sets the record type using a type name string.
    /// </summary>
    /// <param name="typeName">The fully qualified .NET type name.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder WithRecordTypeName(string typeName)
    {
        _recordTypeName = typeName;
        return this;
    }

    /// <summary>
    /// Adds a field to the dataset schema.
    /// </summary>
    /// <param name="field">The field configuration to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder AddField(DataFieldConfiguration field)
    {
        if (field != null)
        {
            _fields.Add(field);
            if (field.IsKey && !_keyFields.Contains(field.Name, StringComparer.Ordinal))
            {
                _keyFields.Add(field.Name);
            }
        }
        return this;
    }

    /// <summary>
    /// Adds a field using a builder function.
    /// </summary>
    /// <param name="builderFunc">A function that configures a <see cref="DataFieldConfigurationBuilder"/> and returns the result.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder AddField(Func<DataFieldConfigurationBuilder, IGenericResult<DataFieldConfiguration>> builderFunc)
    {
        if (builderFunc != null)
        {
            var builder = new DataFieldConfigurationBuilder();
            var result = builderFunc(builder);
            if (result.IsSuccess && result.Value != null)
            {
                AddField(result.Value);
            }
        }
        return this;
    }

    /// <summary>
    /// Adds a simple field with name and type.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="typeName">The .NET type name.</param>
    /// <param name="isKey">Whether this field is part of the primary key.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder AddField(string name, string typeName, bool isKey = false, bool isRequired = false)
    {
        var field = new DataFieldConfiguration
        {
            Name = name,
            TypeName = typeName,
            IsKey = isKey,
            IsRequired = isRequired || isKey
        };
        return AddField(field);
    }

    /// <summary>
    /// Adds multiple fields to the dataset schema.
    /// </summary>
    /// <param name="fields">The field configurations to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder AddFields(IEnumerable<DataFieldConfiguration> fields)
    {
        if (fields != null)
        {
            foreach (var field in fields.Where(f => f != null))
            {
                AddField(field);
            }
        }
        return this;
    }

    /// <summary>
    /// Adds a calculated field that computes its value from other fields.
    /// </summary>
    /// <param name="name">The calculated field name.</param>
    /// <param name="type">The .NET type of the calculated value.</param>
    /// <param name="calculator">Function that computes the value from a data row.</param>
    /// <param name="description">Optional description of the calculation.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder WithCalculatedField(
        string name,
        Type type,
        Func<IDataRow, object> calculator,
        string? description = null)
    {
        var field = new DataFieldConfiguration
        {
            Name = name,
            TypeName = type.FullName ?? type.Name,
            Description = description,
            IsKey = false,
            IsRequired = false,
            Calculator = calculator
        };

        return AddField(field);
    }

    /// <summary>
    /// Adds a calculated field using generic type parameter.
    /// </summary>
    /// <typeparam name="T">The type of the calculated value.</typeparam>
    /// <param name="name">The calculated field name.</param>
    /// <param name="calculator">Function that computes the value from a data row.</param>
    /// <param name="description">Optional description of the calculation.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder WithCalculatedField<T>(
        string name,
        Func<IDataRow, T> calculator,
        string? description = null)
    {
        return WithCalculatedField(
            name,
            typeof(T),
            row => calculator(row)!,
            description);
    }

    /// <summary>
    /// Adds a field name to the primary key.
    /// </summary>
    /// <param name="fieldName">The name of the field that is part of the primary key.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder AddKeyField(string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(fieldName) && !_keyFields.Contains(fieldName, StringComparer.Ordinal))
        {
            _keyFields.Add(fieldName);
        }
        return this;
    }

    /// <summary>
    /// Adds multiple field names to the primary key.
    /// </summary>
    /// <param name="fieldNames">The names of fields that are part of the primary key.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder AddKeyFields(IEnumerable<string> fieldNames)
    {
        if (fieldNames != null)
        {
            foreach (var fieldName in fieldNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                AddKeyField(fieldName);
            }
        }
        return this;
    }

    /// <summary>
    /// Adds a source configuration for this DataSet.
    /// </summary>
    /// <param name="source">The source configuration to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// The source's <see cref="DataSetSourceConfiguration.DataSetId"/> will be
    /// set to the DataSet's ID when <see cref="Build"/> is called.
    /// </remarks>
    public DataSetConfigurationBuilder AddSource(DataSetSourceConfiguration source)
    {
        if (source != null)
        {
            // Remove existing source with same name if present
            _sources.RemoveAll(s => string.Equals(s.SourceName, source.SourceName, StringComparison.OrdinalIgnoreCase));
            _sources.Add(source);
        }
        return this;
    }

    /// <summary>
    /// Adds a simple source configuration.
    /// </summary>
    /// <param name="sourceName">The source name/identifier (e.g., "Primary", "Fallback").</param>
    /// <param name="connectionType">The connection type (e.g., "MsSql", "Http", "File").</param>
    /// <param name="priority">The priority (lower = higher priority).</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder AddSource(string sourceName, string connectionType, int priority = 100)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return this;
        }

        var source = new DataSetSourceConfiguration
        {
            Id = Guid.NewGuid(),
            SourceName = sourceName,
            ConnectionType = connectionType,
            Priority = priority
        };
        return AddSource(source);
    }

    /// <summary>
    /// Adds multiple source configurations.
    /// </summary>
    /// <param name="sources">The source configurations to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder AddSources(IEnumerable<DataSetSourceConfiguration> sources)
    {
        if (sources != null)
        {
            foreach (var source in sources.Where(s => s != null))
            {
                AddSource(source);
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the caching configuration.
    /// </summary>
    /// <param name="caching">The caching configuration settings.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder WithCaching(CachingConfiguration? caching)
    {
        _caching = caching;
        return this;
    }

    /// <summary>
    /// Builds a <see cref="DataSetConfiguration"/> instance from the configured values.
    /// </summary>
    /// <returns>
    /// A result containing the constructed <see cref="DataSetConfiguration"/> if validation succeeds,
    /// or a failure result with error details if validation fails.
    /// </returns>
    public IGenericResult<DataSetConfiguration> Build()
    {
        var validationResult = ValidateBuildInputs(out var dataSetName, out var recordTypeName);
        if (validationResult is not null)
        {
            return validationResult;
        }

        // Update source configurations with the DataSet ID
        foreach (var source in _sources)
        {
            source.DataSetId = _dataSetId;
        }

        var config = new DataSetConfiguration
        {
            Id = _dataSetId,
            Name = dataSetName,
            Description = _description ?? string.Empty,
            Version = _version,
            Category = _category,
            RecordTypeName = recordTypeName,
            Fields = new List<DataFieldConfiguration>(_fields),
            KeyFields = _keyFields
                .Select((name, i) => new DataSetKeyFieldConfiguration
                {
                    KeyName = name,
                    KeyType = "Surrogate",
                    Ordinal = i
                })
                .ToList(),
            // Why: SourceIds is now computed from Sources — set the source configs; SourceIds projects their Ids.
            Sources = new List<DataSetSourceConfiguration>(_sources),
            Caching = _caching
        };

        return GenericResult<DataSetConfiguration>.Success(config);
    }

    private IGenericResult<DataSetConfiguration>? ValidateBuildInputs(out string dataSetName, out string recordTypeName)
    {
        dataSetName = _dataSetName ?? string.Empty;
        recordTypeName = _recordTypeName ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_dataSetName))
        {
            return GenericResult<DataSetConfiguration>.Failure(BuilderResultCodes.ByName("DatasetNameRequired"));
        }

        if (string.IsNullOrWhiteSpace(_recordTypeName))
        {
            return GenericResult<DataSetConfiguration>.Failure(BuilderResultCodes.ByName("RecordTypeNameRequired"));
        }

        if (_fields.Count == 0)
        {
            return GenericResult<DataSetConfiguration>.Failure(
                BuilderResultCodes.ByName("DatasetMissingFields"),
                ResultDetails.Create().With("DatasetName", _dataSetName));
        }

        if (_keyFields.Count == 0)
        {
            return GenericResult<DataSetConfiguration>.Failure(
                BuilderResultCodes.ByName("DatasetMissingKeyFields"),
                ResultDetails.Create().With("DatasetName", _dataSetName));
        }

        var fieldNames = new HashSet<string>(_fields.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        var duplicateFields = _fields.GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateFields.Count > 0)
        {
            return GenericResult<DataSetConfiguration>.Failure(
                BuilderResultCodes.ByName("DatasetDuplicateFields"),
                ResultDetails.Create()
                    .With("DatasetName", _dataSetName)
                    .With("DuplicateFields", string.Join(", ", duplicateFields)));
        }

        var invalidKeyFields = _keyFields.Where(kf => !fieldNames.Contains(kf)).ToList();
        if (invalidKeyFields.Count > 0)
        {
            return GenericResult<DataSetConfiguration>.Failure(
                BuilderResultCodes.ByName("DatasetInvalidKeyFields"),
                ResultDetails.Create()
                    .With("DatasetName", _dataSetName)
                    .With("InvalidKeyFields", string.Join(", ", invalidKeyFields)));
        }

        return null;
    }

    /// <summary>
    /// Resets the builder to its initial state, clearing all configured values.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public DataSetConfigurationBuilder Reset()
    {
        _dataSetId = Guid.NewGuid();
        _dataSetName = null;
        _description = null;
        _version = "1.0";
        _category = "Dataset";
        _recordTypeName = null;
        _fields.Clear();
        _keyFields.Clear();
        _sources.Clear();
        _caching = null;
        return this;
    }
}
