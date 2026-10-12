using System;
using Fdw.Protocols.Tds;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// Writes what SQL Server answers an accepted login with: the database, language and collation the session
/// starts in, the login acknowledgement, the packet size agreed, then DONE.
/// </summary>
public static class TdsLoginResponse
{
    private const string ProgramName = "Microsoft SQL Server";

    /// <summary>Writes the response.</summary>
    /// <param name="payload">The payload being built.</param>
    /// <param name="session">The session the login opened.</param>
    /// <param name="identity">The server this endpoint presents.</param>
    /// <param name="tdsVersion">The TDS version agreed.</param>
    /// <param name="packetSize">The packet size agreed.</param>
    /// <param name="acknowledgeFeatures">Whether the client sent feature extensions, which are acknowledged as none supported.</param>
    public static void Write(TdsPayloadWriter payload, TdsSession session, TdsServerIdentity identity, uint tdsVersion, int packetSize, bool acknowledgeFeatures)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(identity);
        TdsTokenWriter.EnvironmentChange(payload, TdsEnvironmentChangeTypes.Database, session.Database, "master");
        TdsTokenWriter.Message(payload, new SqlServerMessage(5701, 2, 0, $"Changed database context to '{session.Database}'.", 1), identity.ServerName);
        TdsTokenWriter.EnvironmentChange(payload, TdsEnvironmentChangeTypes.Language, identity.Language, string.Empty);
        TdsTokenWriter.Message(payload, new SqlServerMessage(5703, 1, 0, $"Changed language setting to {identity.Language}.", 1), identity.ServerName);
        TdsTokenWriter.EnvironmentChange(payload, TdsEnvironmentChangeTypes.Collation, identity.Collation.Bytes, []);
        TdsTokenWriter.LoginAck(payload, tdsVersion, ProgramName, identity.Version);
        TdsTokenWriter.PacketSize(payload, packetSize);
        if (acknowledgeFeatures)
            TdsTokenWriter.FeatureExtensionAck(payload);

        TdsTokenWriter.Done(payload, TdsTokenTypes.Done, TdsDoneStatus.Final, 0, 0);
    }
}
