namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// The rule every delete translator applies before it builds a statement (FDW-3660): a delete removes the rows its filter
/// names, or flags rows by its soft-delete options, or removes every row only when the caller said <c>AllRows()</c>. A delete
/// with none of these never reaches a connection as a statement with no condition.
/// </summary>
public static class DeleteScope
{
    /// <summary>
    /// Says why <paramref name="command"/> may not run as it stands, or nothing when it states the rows it removes.
    /// </summary>
    /// <param name="command">The delete to check.</param>
    /// <returns>The problem, or <see langword="null"/> when the delete may run.</returns>
    public static string? Problem(IFilterableCommand command)
    {
        // A filterable command that is not a scope command cannot say AllRows, so with no filter it removes nothing.
        var scope = command as IDeleteScopeCommand;
        if (command.Filter?.Root is not null)
        {
            return scope is { AllRows: true }
                ? "it has a filter and also asks for every row, so which rows it removes is unclear: drop the filter or drop AllRows()"
                : null;
        }

        return scope is { FlagsRows: true } or { AllRows: true }
            ? null
            : "it has no filter and does not ask for every row: name the rows with a filter, or call AllRows() to empty the container";
    }
}
