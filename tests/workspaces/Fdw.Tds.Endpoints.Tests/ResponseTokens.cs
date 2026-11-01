using System;
using System.Collections.Generic;
using Fdw.Protocols.Tds;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>
/// Reads a tabular response back into its result sets, DONE tokens, messages and return values, using the
/// protocol package's own TYPE_INFO and value readers.
/// </summary>
internal sealed class ResponseTokens
{
    private readonly Dictionary<byte, Action<TdsPayloadReader>> _readers;
    private ResultSet? _current;

    private ResponseTokens()
    {
        _readers = new()
        {
            [TdsTokenTypes.ColumnMetadata] = ColumnMetadata,
            [TdsTokenTypes.Row] = Row,
            [TdsTokenTypes.Done] = r => Done(r, TdsTokenTypes.Done),
            [TdsTokenTypes.DoneProcedure] = r => Done(r, TdsTokenTypes.DoneProcedure),
            [TdsTokenTypes.DoneInProcedure] = r => Done(r, TdsTokenTypes.DoneInProcedure),
            [TdsTokenTypes.Error] = Message,
            [TdsTokenTypes.Info] = Message,
            [TdsTokenTypes.EnvironmentChange] = r => { EnvironmentChanges.Add(r.ReadBytes(r.ReadUInt16())[0]); },
            [TdsTokenTypes.Order] = r => r.Skip(r.ReadUInt16()),
            [TdsTokenTypes.ReturnStatus] = r => ReturnStatuses.Add(r.ReadInt32()),
            [TdsTokenTypes.ReturnValue] = ReturnValue,
        };
    }

    public List<ResultSet> ResultSets { get; } = [];

    public List<DoneToken> Dones { get; } = [];

    public List<int> MessageNumbers { get; } = [];

    public List<byte> EnvironmentChanges { get; } = [];

    public List<int> ReturnStatuses { get; } = [];

    public List<(string Name, object? Value)> ReturnValues { get; } = [];

    public static ResponseTokens Read(byte[] payload)
    {
        var tokens = new ResponseTokens();
        var reader = new TdsPayloadReader(payload);
        while (reader.Remaining > 0)
        {
            var token = reader.ReadByte();
            if (!tokens._readers.TryGetValue(token, out var read))
                throw new InvalidOperationException($"Token 0x{token:X2} at {reader.Position - 1} is not one these tests read.");

            read(reader);
            if (reader.Overrun)
                throw new InvalidOperationException($"Token 0x{token:X2} ran past the end of the response.");
        }

        return tokens;
    }

    private void ColumnMetadata(TdsPayloadReader reader)
    {
        var count = reader.ReadUInt16();
        _current = new ResultSet();
        for (var i = 0; i < count; i++)
        {
            reader.ReadUInt32();
            reader.ReadUInt16();
            if (!TdsTypeInfo.TryRead(reader, out var type))
                throw new InvalidOperationException($"Column {i} has TYPE_INFO 0x{type.TypeId:X2}, which the reader does not know.");

            _current.Types.Add(type);
            _current.Columns.Add(reader.ReadByteLengthUnicode());
        }

        ResultSets.Add(_current);
    }

    private void Row(TdsPayloadReader reader)
    {
        var set = _current ?? throw new InvalidOperationException("A ROW arrived before any COLMETADATA.");
        var values = new object?[set.Types.Count];
        for (var i = 0; i < values.Length; i++)
        {
            if (!TdsValueReader.TryRead(reader, set.Types[i], out values[i], out var problem))
                throw new InvalidOperationException($"Column {set.Columns[i]} could not be read: {problem}");
        }

        set.Rows.Add(values);
    }

    private void Done(TdsPayloadReader reader, byte token)
        => Dones.Add(new DoneToken(token, reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadInt64()));

    private void Message(TdsPayloadReader reader)
    {
        var length = reader.ReadUInt16();
        var start = reader.Position;
        MessageNumbers.Add(reader.ReadInt32());
        reader.Seek(start + length);
    }

    private void ReturnValue(TdsPayloadReader reader)
    {
        reader.ReadUInt16();
        var name = reader.ReadByteLengthUnicode();
        reader.ReadByte();
        reader.ReadUInt32();
        reader.ReadUInt16();
        if (!TdsTypeInfo.TryRead(reader, out var type) || !TdsValueReader.TryRead(reader, type, out var value, out _))
            throw new InvalidOperationException($"Return value {name} could not be read.");

        ReturnValues.Add((name, value));
    }
}
