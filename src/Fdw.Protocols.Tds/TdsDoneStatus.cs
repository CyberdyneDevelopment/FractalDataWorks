namespace Fdw.Protocols.Tds;

/// <summary>
/// The status bits of a DONE, DONEPROC or DONEINPROC token (MS-TDS 2.2.7.6).
/// </summary>
public static class TdsDoneStatus
{
    /// <summary>The final DONE of the request.</summary>
    public const ushort Final = 0x0000;

    /// <summary>More results follow in this request.</summary>
    public const ushort More = 0x0001;

    /// <summary>The statement ended in an error.</summary>
    public const ushort Error = 0x0002;

    /// <summary>A transaction is in progress.</summary>
    public const ushort InTransaction = 0x0004;

    /// <summary>The row count is valid.</summary>
    public const ushort Count = 0x0010;

    /// <summary>The acknowledgement of an attention signal.</summary>
    public const ushort Attention = 0x0020;

    /// <summary>A severe server error ended the request.</summary>
    public const ushort ServerError = 0x0100;

    /// <summary>The current-command value of a SELECT statement.</summary>
    public const ushort SelectCommand = 0x00C1;
}
