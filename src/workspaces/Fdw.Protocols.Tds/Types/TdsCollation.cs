using System;
using System.Collections.Generic;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// A SQL collation as TDS carries it: five bytes of locale, comparison flags, version and sort id
/// (MS-TDS 2.2.5.1.2).
/// </summary>
/// <remarks>
/// <para>
/// The collation is part of the server this one presents itself as: the login reports it and every
/// character column carries it. It is configured by name, and a name this table does not hold is a
/// configuration error rather than a collation guessed at.
/// </para>
/// <para>
/// Every character value this server writes is nvarchar, so the collation describes comparison and
/// sorting to the client; it never changes how a value is encoded.
/// </para>
/// </remarks>
public sealed class TdsCollation
{
    private static readonly Dictionary<string, byte[]> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SQL_Latin1_General_CP1_CI_AS"] = [0x09, 0x04, 0xD0, 0x00, 0x34],
        ["Latin1_General_CI_AS"] = [0x09, 0x04, 0xD0, 0x00, 0x00],
    };

    private readonly byte[] _bytes;

    private TdsCollation(string name, byte[] bytes)
    {
        Name = name;
        _bytes = bytes;
    }

    /// <summary>Gets the collation's name.</summary>
    public string Name { get; }

    /// <summary>Gets the five bytes TDS carries.</summary>
    public ReadOnlySpan<byte> Bytes => _bytes;

    /// <summary>Finds a collation by name.</summary>
    /// <param name="name">The collation's name, such as SQL_Latin1_General_CP1_CI_AS.</param>
    /// <param name="collation">The collation, when the name is one this server carries.</param>
    /// <returns>True when the name is known.</returns>
    public static bool TryGet(string? name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TdsCollation? collation)
    {
        if (name is not null && Known.TryGetValue(name, out var bytes))
        {
            collation = new TdsCollation(name, bytes);
            return true;
        }

        collation = null;
        return false;
    }

    /// <summary>Gets the collation names this server can present.</summary>
    /// <returns>The names.</returns>
    public static IReadOnlyCollection<string> Names() => Known.Keys;
}
