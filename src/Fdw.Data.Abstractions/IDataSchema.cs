using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents the schema definition for a data container, describing the structure,
/// field types, constraints, and relationships within the data.
/// </summary>
/// <remarks>
/// IDataSchema provides a unified way to describe data structure across different
/// container formats. It enables schema validation, type conversion, and field
/// mapping regardless of whether the data is in CSV, JSON, SQL, or other formats.
/// </remarks>
public interface IDataSchema
{
    /// <summary>
    /// Gets the unique identifier for this schema.
    /// </summary>
    /// <value>A unique identifier for this schema definition.</value>
    string Id { get; }

    /// <summary>
    /// Gets the display name for this schema.
    /// </summary>
    /// <value>A human-readable name for UI and logging purposes.</value>
    string Name { get; }

    /// <summary>
    /// Gets the schema version.
    /// </summary>
    /// <value>The version identifier for schema evolution and compatibility tracking.</value>
    string Version { get; }

    /// <summary>
    /// Gets the field definitions in this schema.
    /// </summary>
    /// <value>An ordered collection of field definitions.</value>
    IReadOnlyList<ISchemaField> Fields { get; }

    /// <summary>
    /// Gets the primary key field names for this schema.
    /// </summary>
    /// <value>
    /// Field names that constitute the primary key, or empty if no primary key is defined.
    /// </value>
    IReadOnlyList<string> PrimaryKeyFields { get; }

    /// <summary>
    /// Gets metadata about this schema.
    /// </summary>
    /// <value>Additional properties and configuration information.</value>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets a field definition by name.
    /// </summary>
    /// <param name="fieldName">The name of the field to retrieve.</param>
    /// <returns>The field definition, or null if not found.</returns>
    ISchemaField? GetField(string fieldName);

    /// <summary>
    /// Gets the ordinal position of a field by name.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The zero-based ordinal position of the field.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the field is not found.</exception>
    int GetOrdinal(string fieldName);

    /// <summary>
    /// Gets field definitions by their names.
    /// </summary>
    /// <param name="fieldNames">The names of the fields to retrieve.</param>
    /// <returns>Field definitions for the specified names, excluding any not found.</returns>
    IEnumerable<ISchemaField> GetFields(IEnumerable<string> fieldNames);

    /// <summary>
    /// Validates that a record conforms to this schema.
    /// </summary>
    /// <param name="record">The record to validate as field name/value pairs.</param>
    /// <returns>A result indicating whether the record is valid, with specific error details.</returns>
    /// <remarks>
    /// This method performs comprehensive validation including type checking,
    /// constraint validation, required field checking, and custom validation rules.
    /// </remarks>
    IGenericResult ValidateRecord(IReadOnlyDictionary<string, object> record);

    /// <summary>
    /// Validates that a strongly-typed object conforms to this schema.
    /// </summary>
    /// <typeparam name="T">The type of the object to validate.</typeparam>
    /// <param name="record">The object to validate.</param>
    /// <returns>A result indicating whether the object is valid, with specific error details.</returns>
    /// <remarks>
    /// This method performs validation by mapping object properties to schema fields
    /// and then applying the same validation rules as the dictionary-based method.
    /// </remarks>
    IGenericResult ValidateRecord<T>(T record) where T : class;

    /// <summary>
    /// Checks if this schema is compatible with another schema.
    /// </summary>
    /// <param name="otherSchema">The schema to check compatibility with.</param>
    /// <param name="compatibilityMode">The level of compatibility to check.</param>
    /// <returns>A result indicating whether the schemas are compatible.</returns>
    /// <remarks>
    /// Schema compatibility is important for data migration, merging, and
    /// cross-system data exchange. Different compatibility modes provide
    /// different levels of strictness in the compatibility check.
    /// </remarks>
    IGenericResult CheckCompatibility(IDataSchema otherSchema, ISchemaCompatibilityMode compatibilityMode);

    /// <summary>
    /// Creates a new schema with additional fields.
    /// </summary>
    /// <param name="additionalFields">The fields to add to the schema.</param>
    /// <returns>A new schema instance with the combined fields.</returns>
    /// <remarks>
    /// This method enables schema composition and extension without modifying
    /// the original schema. The original schema remains unchanged.
    /// </remarks>
    IDataSchema ExtendWith(IEnumerable<ISchemaField> additionalFields);

    /// <summary>
    /// Creates a new schema containing only the specified fields.
    /// </summary>
    /// <param name="fieldNames">The names of the fields to include in the projection.</param>
    /// <returns>A new schema instance containing only the specified fields.</returns>
    /// <remarks>
    /// This method enables schema projection for operations that only need
    /// a subset of fields, which can improve performance and reduce complexity.
    /// </remarks>
    IDataSchema ProjectTo(IEnumerable<string> fieldNames);
}
