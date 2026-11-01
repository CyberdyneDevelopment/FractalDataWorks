using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Fdw.Protocols.Tds.Tests;

/// <summary>
/// Builds a LOGIN7 payload the way a client does (MS-TDS 2.2.6.4), so the parser is tested against the
/// layout rather than against itself.
/// </summary>
public sealed class Login7Payload
{
    private const int FixedLength = 94;

    public uint TdsVersion { get; init; } = TdsVersions.Tds74;

    public int PacketSize { get; init; } = 8000;

    public string HostName { get; init; } = "WORKSTATION";

    public string UserName { get; init; } = "vendor";

    public string Password { get; init; } = "S3cret!";

    public string ApplicationName { get; init; } = "Microsoft SQL Server Management Studio";

    public string ServerName { get; init; } = "fdwsql";

    public string ClientInterfaceName { get; init; } = "Core Microsoft SqlClient Data Provider";

    public string Language { get; init; } = string.Empty;

    public string Database { get; init; } = "Warehouse";

    public bool IntegratedSecurity { get; init; }

    public string ChangePassword { get; init; } = string.Empty;

    public IReadOnlyList<byte> Features { get; init; } = [];

    public byte[] Build()
    {
        var variable = new List<byte>();
        var fixedPart = new byte[FixedLength];
        BinaryPrimitives.WriteUInt32LittleEndian(fixedPart.AsSpan(4), TdsVersion);
        BinaryPrimitives.WriteInt32LittleEndian(fixedPart.AsSpan(8), PacketSize);
        fixedPart[25] = IntegratedSecurity ? (byte)0x80 : (byte)0;
        fixedPart[27] = Features.Count > 0 ? (byte)0x10 : (byte)0;

        Field(fixedPart, 36, variable, Encoding.Unicode.GetBytes(HostName), HostName.Length);
        Field(fixedPart, 40, variable, Encoding.Unicode.GetBytes(UserName), UserName.Length);
        Field(fixedPart, 44, variable, Obfuscate(Password), Password.Length);
        Field(fixedPart, 48, variable, Encoding.Unicode.GetBytes(ApplicationName), ApplicationName.Length);
        Field(fixedPart, 52, variable, Encoding.Unicode.GetBytes(ServerName), ServerName.Length);
        var extensionPointerAt = FixedLength + variable.Count;
        Field(fixedPart, 56, variable, new byte[Features.Count > 0 ? 4 : 0], Features.Count > 0 ? 4 : 0);
        Field(fixedPart, 60, variable, Encoding.Unicode.GetBytes(ClientInterfaceName), ClientInterfaceName.Length);
        Field(fixedPart, 64, variable, Encoding.Unicode.GetBytes(Language), Language.Length);
        Field(fixedPart, 68, variable, Encoding.Unicode.GetBytes(Database), Database.Length);
        Field(fixedPart, 86, variable, Obfuscate(ChangePassword), ChangePassword.Length);

        if (Features.Count > 0)
        {
            var featureAt = FixedLength + variable.Count;
            BinaryPrimitives.WriteUInt32LittleEndian(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(variable).Slice(extensionPointerAt - FixedLength, 4), (uint)featureAt);
            foreach (var feature in Features)
            {
                variable.Add(feature);
                variable.AddRange(new byte[] { 1, 0, 0, 0, 0 });
            }

            variable.Add(0xFF);
        }

        var payload = new byte[FixedLength + variable.Count];
        fixedPart.CopyTo(payload, 0);
        variable.CopyTo(payload, FixedLength);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0), (uint)payload.Length);
        return payload;
    }

    private static void Field(byte[] fixedPart, int at, List<byte> variable, byte[] bytes, int length)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(fixedPart.AsSpan(at), (ushort)(FixedLength + variable.Count));
        BinaryPrimitives.WriteUInt16LittleEndian(fixedPart.AsSpan(at + 2), (ushort)length);
        variable.AddRange(bytes);
    }

    // What a client does before sending a password: swap each byte's nibbles, then XOR with 0xA5.
    private static byte[] Obfuscate(string password)
    {
        var bytes = Encoding.Unicode.GetBytes(password);
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)(((bytes[i] << 4) | (bytes[i] >> 4)) ^ 0xA5);

        return bytes;
    }
}
