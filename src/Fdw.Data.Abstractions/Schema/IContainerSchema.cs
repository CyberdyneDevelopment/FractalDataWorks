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
/// - Get(string) resolves a field by name; Get(IPropertyRole) provides role-based filtering
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

    /// <summary>Gets the fields a dataset may select.</summary>
    /// <returns>The projectable fields.</returns>
    IReadOnlyList<IField> GetProjectableFields();

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
