using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="LoadModes"/>, using the CRTP pattern.
/// </summary>
public abstract class LoadModeBase : TypeOptionBase<LoadModeBase>, ILoadMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadModeBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="usesKeyColumns">Whether the load matches rows on key columns: at least one is needed, and none are accepted when false.</param>
    /// <param name="usesCompareColumns">Whether the load decides a matched row changed by comparing columns: they are accepted, and none is needed, when true.</param>
    /// <param name="usesWriteColumns">Whether the load writes a stated list of columns: at least one is needed, and none are accepted when false.</param>
    /// <param name="allowsVersioning">Whether the load can keep versioned history with effective-from and effective-to columns.</param>
    /// <param name="allowsSoftDelete">Whether the load can set a deleted flag instead of removing rows.</param>
    /// <param name="truncatesFirst">Whether the destination is truncated before the load (Replace).</param>
    protected LoadModeBase(string name, bool usesKeyColumns, bool usesCompareColumns, bool usesWriteColumns, bool allowsVersioning, bool allowsSoftDelete, bool truncatesFirst) : base(name, "LoadModes")
    {
        UsesKeyColumns = usesKeyColumns;
        UsesCompareColumns = usesCompareColumns;
        UsesWriteColumns = usesWriteColumns;
        AllowsVersioning = allowsVersioning;
        AllowsSoftDelete = allowsSoftDelete;
        TruncatesFirst = truncatesFirst;
    }

    /// <inheritdoc/>
    public bool UsesKeyColumns { get; }

    /// <inheritdoc/>
    public bool UsesCompareColumns { get; }

    /// <inheritdoc/>
    public bool UsesWriteColumns { get; }

    /// <inheritdoc/>
    public bool AllowsVersioning { get; }

    /// <inheritdoc/>
    public bool AllowsSoftDelete { get; }

    /// <inheritdoc/>
    public bool TruncatesFirst { get; }
}
