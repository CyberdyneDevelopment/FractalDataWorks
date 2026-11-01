using Fdw.Protocols.Tds;

namespace Fdw.Tds.Endpoints.Sql.Statements;

/// <summary>
/// How a statement ended: the rows it returned, and whether its DONE token reports them.
/// </summary>
public sealed class SqlStatementOutcome
{
    private SqlStatementOutcome(long rowCount, bool counted, ushort command, bool writesDone)
    {
        RowCount = rowCount;
        Counted = counted;
        Command = command;
        WritesDone = writesDone;
    }

    /// <summary>Gets the rows the statement returned.</summary>
    public long RowCount { get; }

    /// <summary>Gets a value indicating whether the DONE token reports the row count.</summary>
    public bool Counted { get; }

    /// <summary>Gets the current-command value the DONE token carries.</summary>
    public ushort Command { get; }

    /// <summary>Gets a value indicating whether the statement ends in its own DONE; a block's statements end in theirs.</summary>
    public bool WritesDone { get; }

    /// <summary>A statement that ran and returned no rows.</summary>
    /// <returns>The outcome.</returns>
    public static SqlStatementOutcome Ran() => new(0, counted: false, command: 0, writesDone: true);

    /// <summary>A SELECT that returned rows.</summary>
    /// <param name="rows">How many.</param>
    /// <returns>The outcome.</returns>
    public static SqlStatementOutcome Selected(long rows) => new(rows, counted: true, command: TdsDoneStatus.SelectCommand, writesDone: true);

    /// <summary>A block whose statements each wrote their own DONE.</summary>
    /// <returns>The outcome.</returns>
    public static SqlStatementOutcome Block() => new(0, counted: false, command: 0, writesDone: false);
}
