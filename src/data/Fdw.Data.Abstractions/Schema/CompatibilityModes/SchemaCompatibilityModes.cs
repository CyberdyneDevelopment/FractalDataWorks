using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// TypeCollection for schema compatibility modes.
/// </summary>
/// <remarks>
/// Provides lookup methods for compatibility modes used in schema validation
/// and data migration operations.
/// </remarks>
[TypeCollection(typeof(SchemaCompatibilityModeBase), typeof(ISchemaCompatibilityMode), typeof(SchemaCompatibilityModes))]
public abstract partial class SchemaCompatibilityModes : TypeCollectionBase<SchemaCompatibilityModeBase, ISchemaCompatibilityMode>
{
}
