namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Which load options a load mode or a load step kind takes (FDW-1108). Both state the same five rules as flags, so
/// one check validates the options of either and never branches on an option's name.
/// </summary>
public interface ILoadOptionShape
{
    /// <summary>Gets whether the load matches rows on key columns: at least one is needed, and none are accepted when false.</summary>
    bool UsesKeyColumns { get; }

    /// <summary>Gets whether the load decides a matched row changed by comparing columns: they are accepted, and none is needed, when true.</summary>
    bool UsesCompareColumns { get; }

    /// <summary>Gets whether the load writes a stated list of columns: at least one is needed, and none are accepted when false.</summary>
    bool UsesWriteColumns { get; }

    /// <summary>Gets whether the load can keep versioned history with effective-from and effective-to columns.</summary>
    bool AllowsVersioning { get; }

    /// <summary>Gets whether the load can set a deleted flag instead of removing rows.</summary>
    bool AllowsSoftDelete { get; }
}
