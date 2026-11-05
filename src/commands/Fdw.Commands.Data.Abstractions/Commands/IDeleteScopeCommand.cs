namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A delete that states which rows it may remove: the rows its filter names, the rows its soft-delete options flag, or every row
/// when the caller asked for that (FDW-3660). <see cref="DeleteScope"/> reads it, so a delete that states none is refused.
/// </summary>
public interface IDeleteScopeCommand : IFilterableCommand
{
    /// <summary>
    /// Gets a value indicating whether the caller asked to remove every row of the container (<c>AllRows()</c>). A delete with no
    /// filter removes nothing unless this is set.
    /// </summary>
    bool AllRows { get; }

    /// <summary>
    /// Gets a value indicating whether the delete flags rows instead of removing them (a soft delete), so its options bound the rows.
    /// </summary>
    bool FlagsRows { get; }
}
