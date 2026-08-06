using Fdw.Collections;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Base class for row mapper types using CRTP pattern.
/// </summary>
public abstract class RowMapperTypeBase : TypeOptionBase<int, RowMapperTypeBase>, IRowMapperType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RowMapperTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this type.</param>
    /// <param name="name">The name of this type.</param>
    protected RowMapperTypeBase(int id, string name) : base(id, name)
    {
    }

    /// <inheritdoc />
    public abstract int EstimatedAllocationsPerRow { get; }

    /// <inheritdoc />
    public abstract bool SupportsPooling { get; }

    /// <inheritdoc />
    public abstract bool SupportsDynamicAccess { get; }
}
