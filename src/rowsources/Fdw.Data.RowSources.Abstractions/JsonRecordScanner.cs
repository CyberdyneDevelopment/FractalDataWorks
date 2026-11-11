using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fdw.Data.RowSources.Abstractions.Logging;
using Fdw.Messages;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>
/// Finds the records of a JSON document as its stream is read, one record at a time (FDW-4971).
/// </summary>
/// <remarks>
/// <para>
/// It reads with <see cref="Utf8JsonReader"/> over a buffer it refills from the stream. The records are the objects of the top-level array,
/// or of the array at the records path (property names, and indexes into arrays, as <c>sports.0.leagues.0.teams</c>), or, for JSON Lines,
/// each top-level value. The walk to the records reads token by token, so a value it passes over is never held; a record is held whole while
/// it is handed out, and the buffer only grows to the largest single record.
/// </para>
/// <para>
/// A path that does not exist, a records value that is not an array and an element that is not an object end the records, as they always
/// have; the rest of the document is still read to its end, held by nothing, so malformed JSON fails wherever it is. Malformed JSON ends the
/// records with <see cref="Failure"/> set, naming the record, line and byte; the records before it were handed out.
/// </para>
/// </remarks>
internal sealed class JsonRecordScanner
{
    private readonly Stream _stream;
    private readonly string[] _path;
    private byte[] _buffer;
    private int _start;
    private int _end;
    private bool _streamEnded;
    private JsonReaderState _state;

    // Where the walk to the records stands.
    private bool _rootSeen;
    private int _segment;
    private int _containerDepth;
    private bool _containerIsArray;
    private int _elementIndex;
    private int _targetIndex;
    private bool _awaitingValue;
    private bool _positioned;
    private int _recordDepth;
    private bool _draining;
    private bool _finished;

    /// <summary>Initializes a new instance of the <see cref="JsonRecordScanner"/> class.</summary>
    /// <param name="stream">The document; read forward, never sought.</param>
    /// <param name="recordsPath">The path to the array of records (an optional <c>$</c> or <c>$.</c> prefix), or null for the top-level array.</param>
    /// <param name="lines">Whether each top-level value is a record (JSON Lines); the records path does not apply then.</param>
    /// <param name="bufferSize">The bytes the buffer starts with; it grows only to hold one record that is larger.</param>
    /// <param name="options">How the reader reads: comments, trailing commas, the deepest nesting.</param>
    public JsonRecordScanner(Stream stream, string? recordsPath, bool lines, int bufferSize, JsonReaderOptions options)
    {
        if (bufferSize < 1)
            throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "A JSON reader's buffer holds at least one byte.");

        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _path = lines ? [] : Segments(recordsPath);
        _buffer = new byte[bufferSize];
        _state = new JsonReaderState(new JsonReaderOptions
        {
            AllowTrailingCommas = options.AllowTrailingCommas,
            CommentHandling = options.CommentHandling,
            MaxDepth = options.MaxDepth,
            AllowMultipleValues = lines,
        });

        if (lines)
        {
            _rootSeen = true;
            _positioned = true;
            _recordDepth = 0;
        }
    }

    /// <summary>Gets how many records were handed out.</summary>
    public long Records { get; private set; }

    /// <summary>Gets why the records ended early, when the document is malformed; null otherwise.</summary>
    public IGenericMessage? Failure { get; private set; }

    /// <summary>Finds the next record.</summary>
    /// <param name="record">The record's bytes, a whole JSON object; valid until the next call.</param>
    /// <returns>True with the next record; false when there are no more, or the document is malformed (<see cref="Failure"/>).</returns>
    public bool TryNext(out ReadOnlyMemory<byte> record)
    {
        record = default;
        try
        {
            while (!_finished)
            {
                var step = Step();
                if (step.Found)
                {
                    Records++;
                    record = new ReadOnlyMemory<byte>(_buffer, step.Start, step.Length);
                    return true;
                }

                if (!_finished)
                    Refill();
            }
        }
        catch (JsonException ex)
        {
            Failure = RowSourceLog.JsonRecordMalformed(NullLogger.Instance, Records + 1, ex.LineNumber, ex.BytePositionInLine, ex.Message);
            _finished = true;
        }

        return false;
    }

    private static string[] Segments(string? path)
    {
        if (path is null || path.Length == 0)
            return [];

        var clean = path.StartsWith("$.", StringComparison.Ordinal) ? path.Substring(2)
            : path.StartsWith("$", StringComparison.Ordinal) ? path.Substring(1)
            : path;
        return [.. clean.Split('.').Where(segment => segment.Length > 0)];
    }

    private static bool IsValueStart(JsonTokenType type) =>
        type is JsonTokenType.StartObject or JsonTokenType.StartArray or JsonTokenType.String or JsonTokenType.Number
            or JsonTokenType.True or JsonTokenType.False or JsonTokenType.Null;

    // Reads tokens from what the buffer holds until a whole record is found, the records end, or the buffer runs out mid-token.
    private StepOutcome Step()
    {
        var reader = new Utf8JsonReader(new ReadOnlySpan<byte>(_buffer, _start, _end - _start), _streamEnded, _state);
        while (true)
        {
            var before = reader.CurrentState;
            var consumedBefore = (int)reader.BytesConsumed;
            if (!reader.Read())
            {
                Commit((int)reader.BytesConsumed, reader.CurrentState);
                _finished = _streamEnded;
                return default;
            }

            // Why the rest is still read when there are no more records: the document is checked to its end, so a malformed tail fails
            // as it always did, and nothing of it is held.
            if (_draining)
                continue;

            if (!_positioned)
            {
                Navigate(ref reader);
                continue;
            }

            var outcome = Record(ref reader, before, consumedBefore);
            if (outcome.Found || outcome.Incomplete)
                return outcome;
        }
    }

    // At a token inside the array of records (or between JSON Lines): a whole record, the end of the records, or a record not yet all read.
    private StepOutcome Record(ref Utf8JsonReader reader, JsonReaderState before, int consumedBefore)
    {
        var depth = reader.CurrentDepth;
        if (reader.TokenType == JsonTokenType.EndArray && depth == _recordDepth - 1)
        {
            _draining = true;
            return default;
        }

        if (depth != _recordDepth)
            return default;

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            _draining = true;
            return default;
        }

        var tokenStart = (int)reader.TokenStartIndex;
        if (!reader.TrySkip())
        {
            // Why back to the token before the record: the record is read whole or not at all, so the next step starts at its first byte.
            Commit(consumedBefore, before);
            return new StepOutcome(found: false, start: 0, length: 0, incomplete: true);
        }

        var start = _start + tokenStart;
        var length = (int)reader.BytesConsumed - tokenStart;
        Commit((int)reader.BytesConsumed, reader.CurrentState);
        return new StepOutcome(found: true, start, length, incomplete: false);
    }

    // One token of the walk to the records: the root, the value a matched property names, an element of an array, or a property of an object.
    private void Navigate(ref Utf8JsonReader reader)
    {
        var type = reader.TokenType;
        var depth = reader.CurrentDepth;
        if (!_rootSeen)
        {
            _rootSeen = true;
            if (_path.Length > 0)
                EnterContainer(type, depth);
            else if (type == JsonTokenType.StartArray)
                Position(depth);
            else
                _draining = true;
        }
        else if (_awaitingValue)
        {
            _awaitingValue = false;
            Arrive(type, depth);
        }
        else if (_containerIsArray)
        {
            InArray(type, depth);
        }
        else
        {
            InObject(ref reader, type, depth);
        }
    }

    private void InArray(JsonTokenType type, int depth)
    {
        if (type == JsonTokenType.EndArray && depth == _containerDepth)
        {
            _draining = true;
        }
        else if (depth == _containerDepth + 1 && IsValueStart(type))
        {
            if (_elementIndex == _targetIndex)
                Arrive(type, depth);
            else
                _elementIndex++;
        }
    }

    private void InObject(ref Utf8JsonReader reader, JsonTokenType type, int depth)
    {
        if (type == JsonTokenType.EndObject && depth == _containerDepth)
            _draining = true;
        else if (type == JsonTokenType.PropertyName && depth == _containerDepth + 1 && reader.ValueTextEquals(_path[_segment]))
            _awaitingValue = true;
    }

    // Reached the value the current segment names: the records when it is the last, else the container the next segment is looked for in.
    private void Arrive(JsonTokenType type, int depth)
    {
        _segment++;
        if (_segment < _path.Length)
            EnterContainer(type, depth);
        else if (type == JsonTokenType.StartArray)
            Position(depth);
        else
            _draining = true;
    }

    // An object is searched for the segment as a property name; an array takes it as an index, and a segment that is no index finds nothing.
    private void EnterContainer(JsonTokenType type, int depth)
    {
        _containerDepth = depth;
        _containerIsArray = type == JsonTokenType.StartArray;
        _elementIndex = 0;
        if (type == JsonTokenType.StartObject)
            return;

        if (!_containerIsArray || !int.TryParse(_path[_segment], NumberStyles.None, CultureInfo.InvariantCulture, out _targetIndex))
            _draining = true;
    }

    private void Position(int arrayDepth)
    {
        _positioned = true;
        _recordDepth = arrayDepth + 1;
    }

    private void Commit(int consumed, JsonReaderState state)
    {
        _start += consumed;
        _state = state;
    }

    // Moves what is not yet read to the front, grows the buffer when one record fills it, and reads more of the stream.
    private void Refill()
    {
        var unread = _end - _start;
        if (_start > 0)
        {
            Buffer.BlockCopy(_buffer, _start, _buffer, 0, unread);
            _start = 0;
            _end = unread;
        }

        if (_end == _buffer.Length)
            Array.Resize(ref _buffer, _buffer.Length * 2);

        var read = _stream.Read(_buffer, _end, _buffer.Length - _end);
        if (read == 0)
            _streamEnded = true;
        else
            _end += read;
    }

    // What a step found: a whole record at Start for Length bytes, or a record the buffer does not yet hold whole.
    private readonly struct StepOutcome
    {
        public StepOutcome(bool found, int start, int length, bool incomplete)
        {
            Found = found;
            Start = start;
            Length = length;
            Incomplete = incomplete;
        }

        public bool Found { get; }

        public int Start { get; }

        public int Length { get; }

        public bool Incomplete { get; }
    }
}
