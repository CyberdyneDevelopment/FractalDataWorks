namespace Fdw.Protocols.Tds;

/// <summary>
/// The TDS protocol versions a LOGIN7 packet names and a LOGINACK answers (MS-TDS 2.2.6.4).
/// </summary>
/// <remarks>
/// This server speaks 7.4. A client asking for 7.2 or later is answered at the lower of the two; one
/// asking for anything older is refused, because the 8-byte row counts and the ALL_HEADERS stream this
/// server reads both begin at 7.2.
/// </remarks>
public static class TdsVersions
{
    /// <summary>TDS 7.2 (SQL Server 2005).</summary>
    public const uint Tds72 = 0x72090002;

    /// <summary>TDS 7.3A (SQL Server 2008).</summary>
    public const uint Tds73A = 0x730A0003;

    /// <summary>TDS 7.3B (SQL Server 2008 R2).</summary>
    public const uint Tds73B = 0x730B0003;

    /// <summary>TDS 7.4 (SQL Server 2012 and later): the version this server speaks.</summary>
    public const uint Tds74 = 0x74000004;

    /// <summary>TDS 8.0, which a strict-encryption client names; its messages are 7.4's inside TLS.</summary>
    public const uint Tds80 = 0x08000000;

    /// <summary>Answers the version a LOGINACK reports for the version a client asked for.</summary>
    /// <param name="requested">The version the LOGIN7 packet named.</param>
    /// <returns>The version to acknowledge, or zero when the client's version is too old to serve.</returns>
    public static uint Negotiate(uint requested)
    {
        if (requested == Tds80)
            return Tds74;

        if (requested < Tds72)
            return 0;

        return requested < Tds74 ? requested : Tds74;
    }
}
