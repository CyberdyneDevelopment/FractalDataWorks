using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Schema;
using Fdw.Schema.Indexes;
using Fdw.Schema.Keys;
using Fdw.Schema.Properties;
using Fdw.Schema.Schemas;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Implementation of container schema with field role filtering.
/// </summary>
/// <remarks>
/// <para>
/// This implementation participates in the unified schema system by implementing
/// ISchemaDefinition&lt;IField&gt; while maintaining backward compatibility with existing
/// container-based APIs.
/// </para>
/// <para>
/// Layout defaults to Tabular, as containers typically represent flat table structures.
/// Child schemas and path expressions are null for tabular layouts.
/// </para>
/// </remarks>
public sealed class ContainerSchema : IContainerSchema
{
    /// <summary>
    /// Gets or initializes all fields in this container.
    /// </summary>
    public required IReadOnlyList<IField> Fields { get; init; }

    /// <summary>
    /// Gets the schema name.
    /// </summary>
    /// <remarks>
    /// Defaults to empty string. Set via initializer when creating schema instances.
    /// </remarks>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional description of this schema.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the properties (alias for Fields).
    /// </summary>
    /// <remarks>
    /// This property implements ISchemaDefinition&lt;IField&gt;.Properties and is aliased to Fields.
    /// </remarks>
    public IReadOnlyList<IField> Properties => Fields;

    /// <summary>
    /// Gets the surrogate key definition (auto-generated, no business meaning).
    /// </summary>
    public IKeyDefinition<IField>? SurrogateKey { get; init; }

    /// <summary>
    /// Gets the natural key definition (business identifier).
    /// </summary>
    public IKeyDefinition<IField>? NaturalKey { get; init; }

    /// <summary>
    /// Gets the indexes defined on this schema.
    /// </summary>
    public IReadOnlyList<IIndexDefinition<IField>> Indexes { get; init; } = [];

    /// <summary>
    /// Gets the data layout type (defaults to Tabular for containers).
    /// </summary>
    public IDataLayout Layout { get; init; } = DataLayouts.ByName("Tabular");

    /// <summary>
    /// Gets the child schemas for hierarchical layouts.
    /// </summary>
    /// <remarks>
    /// Null for tabular layouts. Set this property when creating hierarchical/nested schemas.
    /// </remarks>
    public IReadOnlyList<ISchemaDefinition<IField>>? Children { get; init; }

    /// <summary>
    /// Gets the path expression for navigating to this schema within a parent.
    /// </summary>
    /// <remarks>
    /// Null for tabular layouts. For nested schemas, use JSON path or XPath expressions.
    /// </remarks>
    public string? PathExpression { get; init; }

    /// <summary>
    /// Get fields with Identity role (primary keys, unique identifiers).
    /// </summary>
    /// <remarks>
    /// Uses IsKeyRole from PropertyRoles to identify Surrogate and NaturalKey roles.
    /// </remarks>
    public IReadOnlyList<IField> GetIdentityFields() =>
        Fields.Where(f => f.Role.IsKeyRole).ToList();

    /// <summary>
    /// Get the fields a dataset may select.
    /// </summary>
    /// <remarks>
    /// Every command that builds a column list from the container reads this rather than Fields.
    /// Fields is the container as declared — which is what an admin or analyst authoring it needs to
    /// see, including the physical key. A dataset gets what the container chose to expose.
    /// </remarks>
    public IReadOnlyList<IField> GetProjectableFields() =>
        Fields.Where(f => f.Visibility.AllowsProjection).ToList();

    /// <summary>
    /// Get fields with Attribute role (descriptive, dimensional).
    /// </summary>
    /// <remarks>
    /// Includes both Attribute and Lookup roles.
    /// </remarks>
    public IReadOnlyList<IField> GetAttributeFields() =>
        Fields.Where(f => !f.Role.IsKeyRole && !f.Role.IsAggregatable).ToList();

    /// <summary>
    /// Get fields with Measure role (numeric, aggregatable).
    /// </summary>
    /// <remarks>
    /// Uses IsAggregatable from PropertyRoles.
    /// </remarks>
    public IReadOnlyList<IField> GetMeasureFields() =>
        Fields.Where(f => f.Role.IsAggregatable).ToList();

    /// <summary>
    /// Get a specific field by name (case-insensitive).
    /// </summary>
    public IField? Get(string name) =>
        Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all properties with the specified role.
    /// </summary>
    /// <param name="role">The property role to filter by.</param>
    /// <returns>A list of fields matching the specified role.</returns>
    public IReadOnlyList<IField> Get(IPropertyRole role)
    {
        if (role == null)
            return [];

        // Map role to the appropriate helper method
        if (role.IsKeyRole)
            return GetIdentityFields();

        if (role.IsAggregatable)
            return GetMeasureFields();

        // Neither key nor aggregatable - must be attribute
        return GetAttributeFields();
    }

    /// <summary>
    /// Whether this schema contains nested types (arrays or objects).
    /// </summary>
    public bool SupportsNesting =>
        Fields.Any(f => f.FieldType.IsNested);
}
