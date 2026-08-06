using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Concrete implementation of IDataSchema.
/// </summary>
public class DataSchema : IDataSchema
{
    private readonly IReadOnlyList<ISchemaField> _fields;
    private readonly Dictionary<string, int> _ordinalMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataSchema"/> class.
    /// </summary>
    /// <param name="id">The unique schema identifier.</param>
    /// <param name="name">The schema name.</param>
    /// <param name="version">The schema version.</param>
    /// <param name="fields">The field definitions.</param>
    public DataSchema(string id, string name, string version, IReadOnlyList<ISchemaField> fields)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        _fields = fields ?? throw new ArgumentNullException(nameof(fields));

        // Build ordinal map for fast lookups
        _ordinalMap = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < _fields.Count; i++)
        {
            _ordinalMap[_fields[i].Name] = i;
        }
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Version { get; }

    /// <inheritdoc/>
    public IReadOnlyList<ISchemaField> Fields => _fields;

    /// <inheritdoc/>
    public IReadOnlyList<string> PrimaryKeyFields { get; } = Array.Empty<string>();

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Metadata { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// Gets field names.
    /// </summary>
    public IReadOnlyList<string> FieldNames => _fields.Select(f => f.Name).ToList();

    /// <inheritdoc/>
    public ISchemaField? GetField(string fieldName)
    {
        return _fields.FirstOrDefault(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public IEnumerable<ISchemaField> GetFields(IEnumerable<string> fieldNames)
    {
        var nameSet = new HashSet<string>(fieldNames, StringComparer.Ordinal);
        return _fields.Where(f => nameSet.Contains(f.Name));
    }

    /// <inheritdoc/>
    public IGenericResult ValidateRecord(IReadOnlyDictionary<string, object> record)
    {
        // Basic validation - can be expanded
        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public IGenericResult ValidateRecord<T>(T record) where T : class
    {
        // Basic validation - can be expanded
        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public IGenericResult CheckCompatibility(IDataSchema otherSchema, ISchemaCompatibilityMode compatibilityMode)
    {
        // Basic compatibility check - can be expanded
        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public IDataSchema ExtendWith(IEnumerable<ISchemaField> additionalFields)
    {
        var combined = _fields.Concat(additionalFields).ToList();
        return new DataSchema(Id, Name, Version, combined);
    }

    /// <inheritdoc/>
    public IDataSchema ProjectTo(IEnumerable<string> fieldNames)
    {
        var projected = GetFields(fieldNames).ToList();
        return new DataSchema(Id, Name, Version, projected);
    }

    /// <summary>
    /// Returns whether a field with the given name exists in this schema.
    /// </summary>
    /// <param name="fieldName">The case-sensitive field name to look up.</param>
    /// <returns><c>true</c> if a field with that name is part of the schema; otherwise <c>false</c>.</returns>
    public bool HasField(string fieldName)
    {
        return _ordinalMap.ContainsKey(fieldName);
    }

    /// <summary>
    /// Gets the ordinal position of a field.
    /// </summary>
    // Why: LEFT throwing (not converted to IGenericResult<int>) — this implements
    // IDataSchema.GetOrdinal(string), a documented interface contract member (XML doc declares
    // <exception cref="ArgumentException">) called directly by DataRow.GetValue<T>(string) and
    // by ExpressionBuilder in per-field/per-row hot paths. Converting the interface signature
    // would ripple into every IDataSchema consumer across the framework. Per the
    // "indexer/contract that must throw" carve-out, this stays as-is.
    public int GetOrdinal(string fieldName)
    {
        if (!_ordinalMap.TryGetValue(fieldName, out var ordinal))
            throw new KeyNotFoundException($"Field '{fieldName}' not found in schema");

        return ordinal;
    }

    /// <summary>
    /// Tries to get the ordinal position of a field.
    /// </summary>
    public bool TryGetOrdinal(string fieldName, out int ordinal)
    {
        return _ordinalMap.TryGetValue(fieldName, out ordinal);
    }

    /// <summary>
    /// Creates an empty schema.
    /// </summary>
    public static DataSchema Empty()
    {
        return new DataSchema("empty", "Empty", "1.0", Array.Empty<ISchemaField>());
    }

    /// <summary>
    /// Creates a schema from fields.
    /// </summary>
    public static DataSchema FromFields(IEnumerable<ISchemaField> fields)
    {
        var fieldList = fields.ToList();
        return new DataSchema(Guid.NewGuid().ToString(), "DynamicSchema", "1.0", fieldList);
    }
}