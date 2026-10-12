using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for schema compatibility mode TypeOptions.
/// </summary>
/// <remarks>
/// Provides common functionality for compatibility modes that define
/// how strictly two schemas must match for various operations.
/// </remarks>
public abstract class SchemaCompatibilityModeBase : TypeOptionBase<SchemaCompatibilityModeBase>, ISchemaCompatibilityMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaCompatibilityModeBase"/> class.
    /// </summary>
    /// <param name="name">Human-readable name.</param>
    /// <param name="requiresExactTypes">Whether exact type matching is required.</param>
    /// <param name="allowsSourceExtras">Whether additional source fields are allowed.</param>
    /// <param name="allowsTargetExtras">Whether additional target fields are allowed.</param>
    /// <param name="validatesConstraints">Whether constraints are validated.</param>
    protected SchemaCompatibilityModeBase(
        string name,
        bool requiresExactTypes,
        bool allowsSourceExtras,
        bool allowsTargetExtras,
        bool validatesConstraints)
        : base(name)
    {
        RequiresExactTypes = requiresExactTypes;
        AllowsSourceExtras = allowsSourceExtras;
        AllowsTargetExtras = allowsTargetExtras;
        ValidatesConstraints = validatesConstraints;
    }

    /// <inheritdoc/>
    public bool RequiresExactTypes { get; }

    /// <inheritdoc/>
    public bool AllowsSourceExtras { get; }

    /// <inheritdoc/>
    public bool AllowsTargetExtras { get; }

    /// <inheritdoc/>
    public bool ValidatesConstraints { get; }
}
