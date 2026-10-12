namespace Fdw.Protocols.Tds;

/// <summary>
/// The token types of a tabular result stream (MS-TDS 2.2.7).
/// </summary>
public static class TdsTokenTypes
{
    /// <summary>A stored procedure's return status.</summary>
    public const byte ReturnStatus = 0x79;

    /// <summary>The column metadata of the result set that follows.</summary>
    public const byte ColumnMetadata = 0x81;

    /// <summary>The columns a result set is ordered by.</summary>
    public const byte Order = 0xA9;

    /// <summary>An error message.</summary>
    public const byte Error = 0xAA;

    /// <summary>An informational message.</summary>
    public const byte Info = 0xAB;

    /// <summary>An output parameter's value.</summary>
    public const byte ReturnValue = 0xAC;

    /// <summary>The acknowledgement of a successful login.</summary>
    public const byte LoginAck = 0xAD;

    /// <summary>The acknowledgement of the login's feature extensions.</summary>
    public const byte FeatureExtensionAck = 0xAE;

    /// <summary>A row.</summary>
    public const byte Row = 0xD1;

    /// <summary>A row whose null columns are carried in a bitmap.</summary>
    public const byte NullBitmapRow = 0xD2;

    /// <summary>A change to the session's environment.</summary>
    public const byte EnvironmentChange = 0xE3;

    /// <summary>The end of a SQL statement.</summary>
    public const byte Done = 0xFD;

    /// <summary>The end of a stored procedure.</summary>
    public const byte DoneProcedure = 0xFE;

    /// <summary>The end of a statement inside a stored procedure.</summary>
    public const byte DoneInProcedure = 0xFF;
}
