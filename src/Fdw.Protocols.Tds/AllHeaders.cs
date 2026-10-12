namespace Fdw.Protocols.Tds;

/// <summary>
/// The ALL_HEADERS stream that opens every SQL batch, RPC and transaction manager request from TDS 7.2
/// on (MS-TDS 2.2.5.3).
/// </summary>
/// <remarks>
/// It carries the transaction descriptor, the outstanding request count and trace headers. This server
/// runs one request at a time and holds no transactions across requests, so it reads past the stream.
/// </remarks>
public static class AllHeaders
{
    /// <summary>Moves a reader past the ALL_HEADERS stream at its position.</summary>
    /// <param name="reader">The reader, positioned at the stream's total length.</param>
    public static void Skip(TdsPayloadReader reader)
    {
        System.ArgumentNullException.ThrowIfNull(reader);
        var start = reader.Position;
        var total = reader.ReadUInt32();
        reader.Seek(start + (int)total);
    }
}
