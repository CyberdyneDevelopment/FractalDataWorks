using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A command whose rows can be narrowed to a scope: a copy of it that touches only the rows a filter keeps (FDW-5069).
/// </summary>
/// <remarks>
/// What lets a data set with its own filters scope a write through it to the rows it holds, without knowing the command's type. The copy
/// keeps everything else the command states; only its filter changes.
/// </remarks>
public interface IScopeNarrowableCommand : IDataCommand
{
    /// <summary>Returns a copy of this command that touches only rows <paramref name="scope"/> keeps.</summary>
    /// <param name="scope">The filter the rows must also satisfy, in the names the command's target uses.</param>
    /// <returns>The narrowed copy: its filter is <paramref name="scope"/> ANDed with this command's own.</returns>
    IDataCommand NarrowedTo(IFilterExpression scope);
}
