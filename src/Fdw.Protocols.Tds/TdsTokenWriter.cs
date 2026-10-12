using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Protocols.Tds.Types;

namespace Fdw.Protocols.Tds;

/// <summary>
/// Writes the tokens of a tabular result stream (MS-TDS 2.2.7).
/// </summary>
/// <remarks>
/// A token that leads with its own length is written with a placeholder that is patched once its body
/// is known, so no token's length is computed twice.
/// </remarks>
public static class TdsTokenWriter
{
    private const byte SqlTsqlInterface = 0x01;
    private const ushort NullableFlag = 0x0001;
    private const byte OutputStatus = 0x01;

    /// <summary>Writes LOGINACK.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="tdsVersion">The TDS version negotiated.</param>
    /// <param name="programName">The server program's name.</param>
    /// <param name="version">The version this server reports.</param>
    public static void LoginAck(TdsPayloadWriter writer, uint tdsVersion, string programName, TdsServerVersion version)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(version);
        writer.WriteByte(TdsTokenTypes.LoginAck);
        var length = writer.Mark();
        writer.WriteUInt16(0);
        writer.WriteByte(SqlTsqlInterface);
        writer.WriteUInt32BigEndian(tdsVersion);
        writer.WriteByteLengthUnicode(programName);
        writer.WriteBytes(version.LoginAckBytes());
        Patch(writer, length);
    }

    /// <summary>Writes an ENVCHANGE whose values are text.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="type">The <see cref="TdsEnvironmentChangeTypes"/> value.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="oldValue">The old value.</param>
    public static void EnvironmentChange(TdsPayloadWriter writer, byte type, string newValue, string oldValue)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(TdsTokenTypes.EnvironmentChange);
        var length = writer.Mark();
        writer.WriteUInt16(0);
        writer.WriteByte(type);
        writer.WriteByteLengthUnicode(newValue);
        writer.WriteByteLengthUnicode(oldValue);
        Patch(writer, length);
    }

    /// <summary>Writes an ENVCHANGE whose values are bytes: a collation, a transaction descriptor, a reset.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="type">The <see cref="TdsEnvironmentChangeTypes"/> value.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="oldValue">The old value.</param>
    public static void EnvironmentChange(TdsPayloadWriter writer, byte type, ReadOnlySpan<byte> newValue, ReadOnlySpan<byte> oldValue)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(TdsTokenTypes.EnvironmentChange);
        var length = writer.Mark();
        writer.WriteUInt16(0);
        writer.WriteByte(type);
        writer.WriteByte((byte)newValue.Length);
        writer.WriteBytes(newValue);
        writer.WriteByte((byte)oldValue.Length);
        writer.WriteBytes(oldValue);
        Patch(writer, length);
    }

    /// <summary>Writes an INFO or ERROR, as the message's class says.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="message">The message.</param>
    /// <param name="serverName">The name of the server raising it.</param>
    public static void Message(TdsPayloadWriter writer, SqlServerMessage message, string serverName)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(message);
        writer.WriteByte(message.IsError ? TdsTokenTypes.Error : TdsTokenTypes.Info);
        var length = writer.Mark();
        writer.WriteUInt16(0);
        writer.WriteInt32(message.Number);
        writer.WriteByte(message.State);
        writer.WriteByte(message.Class);
        writer.WriteUShortLengthUnicode(message.Text);
        writer.WriteByteLengthUnicode(serverName);
        writer.WriteByteLengthUnicode(message.ProcedureName);
        writer.WriteInt32(message.LineNumber);
        Patch(writer, length);
    }

    /// <summary>Writes DONE, DONEPROC or DONEINPROC.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="token">The <see cref="TdsTokenTypes"/> value of the DONE kind.</param>
    /// <param name="status">The <see cref="TdsDoneStatus"/> bits.</param>
    /// <param name="currentCommand">The kind of statement that ended.</param>
    /// <param name="rowCount">The rows it affected or returned.</param>
    public static void Done(TdsPayloadWriter writer, byte token, ushort status, ushort currentCommand, long rowCount)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(token);
        writer.WriteUInt16(status);
        writer.WriteUInt16(currentCommand);
        writer.WriteInt64(rowCount);
    }

    /// <summary>Writes COLMETADATA for a result set.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="columns">The result set's columns.</param>
    /// <param name="collation">The collation character columns carry.</param>
    public static void ColumnMetadata(TdsPayloadWriter writer, IReadOnlyList<TdsColumn> columns, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(columns);
        writer.WriteByte(TdsTokenTypes.ColumnMetadata);
        writer.WriteUInt16((ushort)columns.Count);
        foreach (var column in columns)
        {
            writer.WriteUInt32(0);
            writer.WriteUInt16(column.IsNullable ? NullableFlag : (ushort)0);
            column.Type.DataType.WriteTypeInfo(writer, column.Type, collation);
            writer.WriteByteLengthUnicode(column.Name);
        }
    }

    /// <summary>Writes one ROW.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="columns">The result set's columns.</param>
    /// <param name="values">The row's values, one per column, in column order.</param>
    /// <param name="failedColumn">The index of the column whose value could not be written, when one could not.</param>
    /// <returns>True when every value was written; the payload is unusable after a false.</returns>
    public static bool TryRow(TdsPayloadWriter writer, IReadOnlyList<TdsColumn> columns, IReadOnlyList<object?> values, out int failedColumn)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(values);
        writer.WriteByte(TdsTokenTypes.Row);
        for (var i = 0; i < columns.Count; i++)
        {
            if (!columns[i].Type.DataType.TryWriteValue(writer, columns[i].Type, values[i]))
            {
                failedColumn = i;
                return false;
            }
        }

        failedColumn = -1;
        return true;
    }

    /// <summary>Writes RETURNSTATUS.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="status">The procedure's return status.</param>
    public static void ReturnStatus(TdsPayloadWriter writer, int status)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(TdsTokenTypes.ReturnStatus);
        writer.WriteInt32(status);
    }

    /// <summary>Writes RETURNVALUE: an output parameter's value.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="ordinal">The parameter's position in the request.</param>
    /// <param name="name">The parameter's name, with its leading @.</param>
    /// <param name="type">The parameter's SQL type.</param>
    /// <param name="value">The value; null for a NULL.</param>
    /// <param name="collation">The collation a character value carries.</param>
    /// <returns>True when the value was written; the payload is unusable after a false.</returns>
    public static bool TryReturnValue(TdsPayloadWriter writer, ushort ordinal, string name, TdsColumnType type, object? value, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(type);
        writer.WriteByte(TdsTokenTypes.ReturnValue);
        writer.WriteUInt16(ordinal);
        writer.WriteByteLengthUnicode(name);
        writer.WriteByte(OutputStatus);
        writer.WriteUInt32(0);
        writer.WriteUInt16(NullableFlag);
        type.DataType.WriteTypeInfo(writer, type, collation);
        return type.DataType.TryWriteValue(writer, type, value);
    }

    /// <summary>Writes FEATUREEXTACK acknowledging no feature: this server supports none of the extensions.</summary>
    /// <param name="writer">The payload being built.</param>
    public static void FeatureExtensionAck(TdsPayloadWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(TdsTokenTypes.FeatureExtensionAck);
        writer.WriteByte(0xFF);
    }

    /// <summary>Writes the packet-size ENVCHANGE the login acknowledges.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="packetSize">The packet size agreed.</param>
    public static void PacketSize(TdsPayloadWriter writer, int packetSize)
    {
        var text = packetSize.ToString(CultureInfo.InvariantCulture);
        EnvironmentChange(writer, TdsEnvironmentChangeTypes.PacketSize, text, text);
    }

    // A length placeholder holds the bytes written after it.
    private static void Patch(TdsPayloadWriter writer, int lengthAt)
        => writer.PatchUInt16(lengthAt, (ushort)(writer.Length - lengthAt - 2));
}
