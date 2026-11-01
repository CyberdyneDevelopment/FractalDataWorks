using System;
using Fdw.Protocols.Tds;
using Fdw.Protocols.Tds.Types;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// The SQL Server this endpoint presents itself as: its name, version, edition, collation, language, and
/// the database a login opens.
/// </summary>
/// <remarks>
/// Clients choose behaviour from these — SSMS picks its catalog queries by version and engine edition, a
/// driver decodes text by the collation — so every one is configured. None is assumed.
/// </remarks>
public sealed class TdsServerIdentity
{
    /// <summary>Initializes a new instance of the <see cref="TdsServerIdentity"/> class.</summary>
    /// <param name="serverName">The name <c>@@SERVERNAME</c> and <c>SERVERPROPERTY('ServerName')</c> answer.</param>
    /// <param name="version">The product version.</param>
    /// <param name="edition">The edition <c>SERVERPROPERTY('Edition')</c> answers, such as Standard Edition (64-bit).</param>
    /// <param name="engineEdition">The engine edition <c>SERVERPROPERTY('EngineEdition')</c> answers: 2 Standard, 3 Enterprise, 4 Express.</param>
    /// <param name="collation">The server and database collation.</param>
    /// <param name="language">The session language, such as us_english.</param>
    /// <param name="databaseName">The one database this server holds, which every login opens.</param>
    public TdsServerIdentity(
        string serverName,
        TdsServerVersion version,
        string edition,
        int engineEdition,
        TdsCollation collation,
        string language,
        string databaseName)
    {
        ServerName = serverName ?? throw new ArgumentNullException(nameof(serverName));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Edition = edition ?? throw new ArgumentNullException(nameof(edition));
        EngineEdition = engineEdition;
        Collation = collation ?? throw new ArgumentNullException(nameof(collation));
        Language = language ?? throw new ArgumentNullException(nameof(language));
        DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
    }

    /// <summary>Gets the server's name.</summary>
    public string ServerName { get; }

    /// <summary>Gets the product version.</summary>
    public TdsServerVersion Version { get; }

    /// <summary>Gets the edition's name.</summary>
    public string Edition { get; }

    /// <summary>Gets the engine edition number.</summary>
    public int EngineEdition { get; }

    /// <summary>Gets the server and database collation.</summary>
    public TdsCollation Collation { get; }

    /// <summary>Gets the session language.</summary>
    public string Language { get; }

    /// <summary>Gets the one database this server holds.</summary>
    public string DatabaseName { get; }

    /// <summary>The database id <c>DB_ID()</c> answers for the one database this server holds.</summary>
    /// <remarks>Ids 1 to 4 are SQL Server's system databases; the first user database is 5.</remarks>
    public const int DatabaseId = 5;

    /// <summary>Gets the text <c>@@VERSION</c> answers.</summary>
    /// <returns>The version banner.</returns>
    public string VersionBanner()
        => $"Microsoft SQL Server - {Version} (X64) \n\t{Edition} on FractalDataWorks\n";
}
