using System;
using System.Globalization;

namespace Fdw.Protocols.Tds;

/// <summary>
/// The SQL Server product version this server reports: in pre-login, in the login acknowledgement,
/// and to <c>@@VERSION</c> and <c>SERVERPROPERTY</c>.
/// </summary>
/// <remarks>
/// Clients choose behaviour by it — SSMS reads the major version to pick which catalog queries to run —
/// so it is configured, never assumed.
/// </remarks>
public sealed class TdsServerVersion
{
    /// <summary>Initializes a new instance of the <see cref="TdsServerVersion"/> class.</summary>
    /// <param name="major">The major version, such as 16 for SQL Server 2022.</param>
    /// <param name="minor">The minor version.</param>
    /// <param name="build">The build number.</param>
    /// <param name="revision">The revision.</param>
    public TdsServerVersion(byte major, byte minor, ushort build, ushort revision)
    {
        Major = major;
        Minor = minor;
        Build = build;
        Revision = revision;
    }

    /// <summary>Gets the major version.</summary>
    public byte Major { get; }

    /// <summary>Gets the minor version.</summary>
    public byte Minor { get; }

    /// <summary>Gets the build number.</summary>
    public ushort Build { get; }

    /// <summary>Gets the revision.</summary>
    public ushort Revision { get; }

    /// <summary>Parses a version written as major.minor.build.revision, such as 16.0.4135.4.</summary>
    /// <param name="text">The version text.</param>
    /// <param name="version">The parsed version.</param>
    /// <returns>True when the text was a version of four parts that fit their fields.</returns>
    public static bool TryParse(string? text, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TdsServerVersion? version)
    {
        version = null;
        var parts = text?.Split('.');
        if (parts is not { Length: 4 }
            || !byte.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major)
            || !byte.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor)
            || !ushort.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var build)
            || !ushort.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var revision))
            return false;

        version = new TdsServerVersion(major, minor, build, revision);
        return true;
    }

    /// <summary>Formats the version as major.minor.build.revision.</summary>
    /// <returns>The version text.</returns>
    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Build}.{Revision}");

    /// <summary>Gets the four-byte version a LOGINACK carries: major, minor, then the build big-endian.</summary>
    /// <returns>The version bytes.</returns>
    public byte[] LoginAckBytes() => [Major, Minor, (byte)(Build >> 8), (byte)(Build & 0xFF)];

    /// <summary>Gets the six-byte version a pre-login VERSION option carries.</summary>
    /// <returns>The version bytes: major, minor, build big-endian, then the sub-build.</returns>
    public byte[] PreLoginBytes()
        => [Major, Minor, (byte)(Build >> 8), (byte)(Build & 0xFF), (byte)(Revision >> 8), (byte)(Revision & 0xFF)];

    /// <summary>Gets the integer <c>@@MICROSOFTVERSION</c> answers: major, minor and build packed.</summary>
    /// <returns>The packed version.</returns>
    public int MicrosoftVersion() => (Major << 24) | (Minor << 16) | Build;
}
