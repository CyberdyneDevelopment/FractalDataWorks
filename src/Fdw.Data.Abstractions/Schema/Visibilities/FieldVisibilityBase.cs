using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for field visibility TypeOptions.
/// </summary>
public abstract class FieldVisibilityBase : TypeOptionBase<int, FieldVisibilityBase>, IFieldVisibility
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldVisibilityBase"/> class.
    /// </summary>
    /// <param name="id">Unique numeric identifier.</param>
    /// <param name="name">The name stored in <c>data.DataContainerField.VisibilityId</c>.</param>
    /// <param name="allowsProjection">Whether the field may appear in a dataset projection.</param>
    protected FieldVisibilityBase(int id, string name, bool allowsProjection)
        : base(id, name)
    {
        AllowsProjection = allowsProjection;
    }

    /// <inheritdoc />
    public bool AllowsProjection { get; }
}
