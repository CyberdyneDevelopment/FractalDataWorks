using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>
/// Base class of the soft-delete modes: the ways a soft delete picks the rows it flags.
/// </summary>
public abstract class SoftDeleteModeBase : TypeOptionBase<SoftDeleteModeBase>, ISoftDeleteMode
{
    /// <summary>
    /// Initializes the NotFound sentinel.
    /// </summary>
    protected SoftDeleteModeBase()
        : base(0, "NotFound")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteModeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this mode.</param>
    /// <param name="description">The description of this mode.</param>
    /// <param name="requiresMarker">Whether this mode selects rows by a marker column and value.</param>
    protected SoftDeleteModeBase(string name, string description, bool requiresMarker)
        : base(name, $"Commands:Data:SoftDelete:{name}", $"{name} soft delete", description, "SoftDelete")
    {
        RequiresMarker = requiresMarker;
    }

    /// <inheritdoc />
    public bool RequiresMarker { get; }

    /// <inheritdoc />
    public virtual bool NeedsSourceSet => false;

    /// <inheritdoc />
    public virtual string? Problem(SoftDeleteOptions options) => $"'{Name}' is not a soft-delete mode";

    /// <inheritdoc />
    public virtual IReadOnlyList<SoftDeleteMarker> Conditions(SoftDeleteOptions options) => [];
}
