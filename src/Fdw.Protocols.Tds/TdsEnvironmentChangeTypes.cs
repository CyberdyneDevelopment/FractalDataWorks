namespace Fdw.Protocols.Tds;

/// <summary>
/// The types of an ENVCHANGE token (MS-TDS 2.2.7.9).
/// </summary>
public static class TdsEnvironmentChangeTypes
{
    /// <summary>The current database.</summary>
    public const byte Database = 1;

    /// <summary>The session language.</summary>
    public const byte Language = 2;

    /// <summary>The negotiated packet size.</summary>
    public const byte PacketSize = 4;

    /// <summary>The SQL collation.</summary>
    public const byte Collation = 7;

    /// <summary>A transaction began.</summary>
    public const byte BeginTransaction = 8;

    /// <summary>A transaction was committed.</summary>
    public const byte CommitTransaction = 9;

    /// <summary>A transaction was rolled back.</summary>
    public const byte RollbackTransaction = 10;

    /// <summary>The session was reset.</summary>
    public const byte ResetConnection = 18;
}
