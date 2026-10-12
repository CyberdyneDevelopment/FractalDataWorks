using Fdw.Collections;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Base class for row mapper types using CRTP pattern.
/// </summary>
public abstract class RowMapperTypeBase : TypeOptionBase<RowMapperTypeBase>, IRowMapperType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RowMapperTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this type.</param>
    protected RowMapperTypeBase(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public abstract int EstimatedAllocationsPerRow { get; }

    /// <inheritdoc />
    public abstract bool SupportsPooling { get; }

    /// <inheritdoc />
    public abstract bool SupportsDynamicAccess { get; }
}
