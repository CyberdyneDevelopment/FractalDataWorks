using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for schema compatibility mode TypeOptions.
/// </summary>
/// <remarks>
/// Schema compatibility modes define how strictly two schemas must match
/// for data migration, merging, and cross-system data exchange.
/// </remarks>
public interface ISchemaCompatibilityMode : ITypeOption<int, SchemaCompatibilityModeBase>
{
    /// <summary>
    /// Gets whether this mode requires exact field type matching.
    /// </summary>
    bool RequiresExactTypes { get; }

    /// <summary>
    /// Gets whether this mode allows additional fields in the source schema.
    /// </summary>
    bool AllowsSourceExtras { get; }

    /// <summary>
    /// Gets whether this mode allows additional fields in the target schema.
    /// </summary>
    bool AllowsTargetExtras { get; }

    /// <summary>
    /// Gets whether this mode validates constraints.
    /// </summary>
    bool ValidatesConstraints { get; }
}
