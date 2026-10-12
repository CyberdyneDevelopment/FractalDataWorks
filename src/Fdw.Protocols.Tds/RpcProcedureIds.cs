namespace Fdw.Protocols.Tds;

/// <summary>
/// The well-known procedure ids an RPC request may name instead of a procedure name (MS-TDS 2.2.6.6).
/// </summary>
public static class RpcProcedureIds
{
    /// <summary>sp_cursor.</summary>
    public const ushort Cursor = 1;

    /// <summary>sp_cursoropen.</summary>
    public const ushort CursorOpen = 2;

    /// <summary>sp_cursorprepare.</summary>
    public const ushort CursorPrepare = 3;

    /// <summary>sp_cursorexecute.</summary>
    public const ushort CursorExecute = 4;

    /// <summary>sp_cursorprepexec.</summary>
    public const ushort CursorPrepExec = 5;

    /// <summary>sp_cursorunprepare.</summary>
    public const ushort CursorUnprepare = 6;

    /// <summary>sp_cursorfetch.</summary>
    public const ushort CursorFetch = 7;

    /// <summary>sp_cursoroption.</summary>
    public const ushort CursorOption = 8;

    /// <summary>sp_cursorclose.</summary>
    public const ushort CursorClose = 9;

    /// <summary>sp_executesql.</summary>
    public const ushort ExecuteSql = 10;

    /// <summary>sp_prepare.</summary>
    public const ushort Prepare = 11;

    /// <summary>sp_execute.</summary>
    public const ushort Execute = 12;

    /// <summary>sp_prepexec.</summary>
    public const ushort PrepExec = 13;

    /// <summary>sp_prepexecrpc.</summary>
    public const ushort PrepExecRpc = 14;

    /// <summary>sp_unprepare.</summary>
    public const ushort Unprepare = 15;

    /// <summary>Gets the procedure name a well-known id stands for.</summary>
    /// <param name="id">The id the request carried.</param>
    /// <returns>The procedure's name, or an empty string for an id the protocol does not define.</returns>
    public static string NameOf(ushort id) => id < Names.Length ? Names[id] : string.Empty;

    private static readonly string[] Names =
    [
        string.Empty,
        "sp_cursor",
        "sp_cursoropen",
        "sp_cursorprepare",
        "sp_cursorexecute",
        "sp_cursorprepexec",
        "sp_cursorunprepare",
        "sp_cursorfetch",
        "sp_cursoroption",
        "sp_cursorclose",
        "sp_executesql",
        "sp_prepare",
        "sp_execute",
        "sp_prepexec",
        "sp_prepexecrpc",
        "sp_unprepare",
    ];
}
