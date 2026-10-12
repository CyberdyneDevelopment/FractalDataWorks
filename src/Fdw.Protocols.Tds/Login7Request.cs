using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fdw.Protocols.Tds.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Protocols.Tds;

/// <summary>
/// A client's LOGIN7 message (MS-TDS 2.2.6.4): who it signs in as and how it wants the session set up.
/// </summary>
/// <remarks>
/// The password is held only until the authentication flow verifies it, and it is never part of
/// <see cref="ToString"/>.
/// </remarks>
public sealed class Login7Request
{
    // Offsets of the fixed part (TDS 7.2 and later; 94 bytes).
    private const int FixedLength = 94;
    private const int TdsVersionAt = 4;
    private const int PacketSizeAt = 8;
    private const int OptionFlags2At = 25;
    private const int TypeFlagsAt = 26;
    private const int OptionFlags3At = 27;
    private const int HostNameAt = 36;
    private const int UserNameAt = 40;
    private const int PasswordAt = 44;
    private const int ApplicationNameAt = 48;
    private const int ServerNameAt = 52;
    private const int ExtensionAt = 56;
    private const int ClientInterfaceAt = 60;
    private const int LanguageAt = 64;
    private const int DatabaseAt = 68;
    private const int SspiAt = 78;
    private const int ChangePasswordAt = 86;

    private const byte IntegratedSecurityFlag = 0x80;
    private const byte ReadOnlyIntentFlag = 0x20;
    private const byte ExtensionFlag = 0x10;
    private const byte FeatureTerminator = 0xFF;

    /// <summary>
    /// The largest LOGIN7 this server reads. Every variable field sits at a 16-bit offset, so a login's own text
    /// ends inside the first 64 KB; only an SSPI blob, which this server refuses, or the feature extensions, a few
    /// bytes each, reach past it. A stranger's first message stays small whatever limit signed-in callers get.
    /// </summary>
    public const int LargestPayloadBytes = 128 * 1024;

    /// <summary>The feature id a client sends to sign in with a federated (Entra) token.</summary>
    public const byte FederatedAuthenticationFeature = 0x02;

    private Login7Request()
    {
    }

    /// <summary>Gets the TDS version the client asked for.</summary>
    public uint TdsVersion { get; private init; }

    /// <summary>Gets the packet size the client asked for.</summary>
    public int PacketSize { get; private init; }

    /// <summary>Gets the client machine's name.</summary>
    public string HostName { get; private init; } = string.Empty;

    /// <summary>Gets the login name presented.</summary>
    public string UserName { get; private init; } = string.Empty;

    /// <summary>Gets the password presented, decoded.</summary>
    public string Password { get; private init; } = string.Empty;

    /// <summary>Gets the client application's name.</summary>
    public string ApplicationName { get; private init; } = string.Empty;

    /// <summary>Gets the server name the client connected to.</summary>
    public string ServerName { get; private init; } = string.Empty;

    /// <summary>Gets the client interface library's name, such as Core Microsoft SqlClient Data Provider.</summary>
    public string ClientInterfaceName { get; private init; } = string.Empty;

    /// <summary>Gets the language the client asked for, empty for the server's own.</summary>
    public string Language { get; private init; } = string.Empty;

    /// <summary>Gets the database the client asked for, empty for the login's default.</summary>
    public string Database { get; private init; } = string.Empty;

    /// <summary>Gets a value indicating whether the client signs in with integrated (SSPI) authentication.</summary>
    public bool UsesIntegratedSecurity { get; private init; }

    /// <summary>Gets a value indicating whether the client declared read-only intent.</summary>
    public bool ReadOnlyIntent { get; private init; }

    /// <summary>Gets a value indicating whether the client asks to change its password at login.</summary>
    public bool ChangesPassword { get; private init; }

    /// <summary>Gets the feature extensions the client asked for, by feature id.</summary>
    public IReadOnlyList<byte> RequestedFeatures { get; private init; } = [];

    /// <summary>Gets a value indicating whether the client sent a feature extension block at all.</summary>
    public bool HasFeatureExtension { get; private init; }

    /// <summary>Parses a LOGIN7 payload.</summary>
    /// <param name="payload">The message payload.</param>
    /// <param name="logger">The logger for a malformed message.</param>
    /// <returns>The login, or the refusal.</returns>
    public static IGenericResult<Login7Request> Parse(byte[] payload, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Length < FixedLength)
            return Malformed(logger, $"it is {payload.Length} bytes, shorter than the 94-byte fixed part");

        var reader = new TdsPayloadReader(payload);
        var optionFlags3 = payload[OptionFlags3At];
        var hasExtension = (optionFlags3 & ExtensionFlag) != 0;
        var features = hasExtension ? Features(reader, payload) : [];

        var login = new Login7Request
        {
            TdsVersion = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(TdsVersionAt)),
            PacketSize = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(PacketSizeAt)),
            UsesIntegratedSecurity = (payload[OptionFlags2At] & IntegratedSecurityFlag) != 0,
            ReadOnlyIntent = (payload[TypeFlagsAt] & ReadOnlyIntentFlag) != 0,
            HostName = Text(reader, payload, HostNameAt),
            UserName = Text(reader, payload, UserNameAt),
            Password = Deobfuscated(reader, payload),
            ApplicationName = Text(reader, payload, ApplicationNameAt),
            ServerName = Text(reader, payload, ServerNameAt),
            ClientInterfaceName = Text(reader, payload, ClientInterfaceAt),
            Language = Text(reader, payload, LanguageAt),
            Database = Text(reader, payload, DatabaseAt),
            ChangesPassword = Length(payload, ChangePasswordAt) > 0,
            HasFeatureExtension = hasExtension,
            RequestedFeatures = features,
        };

        if (reader.Overrun)
            return Malformed(logger, "a field's offset and length run past the end of the payload");

        TdsProtocolLog.LoginRead(logger, login.TdsVersion, login.UserName, login.ApplicationName, login.Database);
        return GenericResult<Login7Request>.Success(login);
    }

    /// <summary>Gets a value indicating whether the client asked for a feature by its id.</summary>
    /// <param name="featureId">The feature id.</param>
    /// <returns>True when it was asked for.</returns>
    public bool Requests(byte featureId) => RequestedFeatures.Contains(featureId);

    /// <summary>Names the login without its password.</summary>
    /// <returns>The login name, application and database.</returns>
    public override string ToString() => $"login '{UserName}', application '{ApplicationName}', database '{Database}'";

    /// <summary>Gets whether the client also carried an SSPI blob.</summary>
    /// <param name="payload">The message payload.</param>
    /// <returns>True when an SSPI blob is present.</returns>
    public static bool CarriesSspi(byte[] payload)
        => payload is { Length: >= FixedLength } && Length(payload, SspiAt) > 0;

    private static ushort Offset(byte[] payload, int at) => BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(at));

    private static ushort Length(byte[] payload, int at) => BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(at + 2));

    private static string Text(TdsPayloadReader reader, byte[] payload, int at)
        => reader.ReadUnicodeAt(Offset(payload, at), Length(payload, at));

    // The password travels obfuscated: each byte had its nibbles swapped and was XORed with 0xA5
    // (MS-TDS 2.2.6.4). That hides it from a casual glance at a capture and nothing more, which is why
    // this server never accepts a login outside TLS.
    private static string Deobfuscated(TdsPayloadReader reader, byte[] payload)
    {
        var bytes = reader.BytesAt(Offset(payload, PasswordAt), Length(payload, PasswordAt) * 2);
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = (byte)(bytes[i] ^ 0xA5);
            bytes[i] = (byte)((b >> 4) | (b << 4));
        }

        return Encoding.Unicode.GetString(bytes);
    }

    // The extension field points at a four-byte offset of the FeatureExt block: feature id, a four-byte
    // data length, the data, repeated to the 0xFF terminator.
    private static List<byte> Features(TdsPayloadReader reader, byte[] payload)
    {
        var pointer = reader.BytesAt(Offset(payload, ExtensionAt), 4);
        if (pointer.Length < 4)
            return [];

        reader.Seek((int)BinaryPrimitives.ReadUInt32LittleEndian(pointer));
        var features = new List<byte>();
        while (!reader.Overrun)
        {
            var feature = reader.ReadByte();
            if (feature == FeatureTerminator)
                break;

            features.Add(feature);
            reader.Skip((int)reader.ReadUInt32());
        }

        return features;
    }

    private static IGenericResult<Login7Request> Malformed(ILogger logger, string reason)
        => GenericResult<Login7Request>.Failure(TdsProtocolLog.Malformed(logger, "login packet", reason));
}
