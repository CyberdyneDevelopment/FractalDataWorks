using System.Collections.Generic;
using Fdw.Schema.Schemas;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Schema definition for a container (table, view, endpoint, file).
/// Contains field definitions with Identity/Attribute/Measure roles.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends ISchemaDefinition&lt;IField&gt; to participate in the unified schema system
/// while maintaining backward compatibility with existing container-based code.
/// </para>
/// <para>
/// Key points:
/// - Properties is aliased to Fields for backward compatibility
/// - Get(string) resolves a field by name; Get(IDatumType) provides role-based filtering
/// - Identity/Attribute/Measure helpers remain for convenience
/// </para>
/// </remarks>
public interface IContainerSchema : ISchemaDefinition<IField>
{
    /// <summary>
    /// All fields in this container.
    /// </summary>
    /// <remarks>
    /// This is an alias for Properties (from ISchemaDefinition) to maintain backward compatibility.
    /// </remarks>
    IReadOnlyList<IField> Fields { get; }

    /// <summary>
    /// Get fields with Identity role (primary keys, unique identifiers).
    /// </summary>
    IReadOnlyList<IField> GetIdentityFields();

    /// <summary>Gets the fields a command returns when it names no columns of its own (FDW-988).</summary>
    /// <param name="includeMetadata">Whether the command includes Metadata columns.</param>
    /// <param name="includeDescriptions">Whether the command includes Descriptive columns.</param>
    /// <param name="includeKeys">Whether the command includes DbKey columns (server-side reads only).</param>
    /// <returns>The fields whose datum type says they are returned.</returns>
    IReadOnlyList<IField> GetReturnedFields(bool includeMetadata, bool includeDescriptions, bool includeKeys);

    /// <summary>
    /// Get fields with Attribute role (descriptive, dimensional).
    /// </summary>
    IReadOnlyList<IField> GetAttributeFields();

    /// <summary>
    /// Get fields with Measure role (numeric, aggregatable).
    /// </summary>
    IReadOnlyList<IField> GetMeasureFields();

    /// <summary>
    /// Whether this schema contains nested types (arrays or objects).
    /// </summary>
    bool SupportsNesting { get; }
}
