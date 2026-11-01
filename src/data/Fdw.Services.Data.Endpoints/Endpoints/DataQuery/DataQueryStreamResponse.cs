using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Response for a data query no page limit applies to: every row, written as it is read (FDW-1209).
/// </summary>
/// <remarks>
/// <para>
/// The properties are written in the order declared. <see cref="Rows"/> is written as each row is produced, and
/// <see cref="RowCount"/> and <see cref="Complete"/> after it, read only then. A response that fails part-way does not reach
/// them: the rows throw, the JSON is left unfinished and the connection is aborted, so a client either gets a body that parses
/// and ends in <c>"complete": true</c>, or an error. A client that wants proof it has everything checks <see cref="Complete"/>
/// and compares <see cref="RowCount"/> with the rows it read.
/// </para>
/// <para>
/// There is no <c>take</c>: nothing limited the read. <see cref="HasMoreRows"/> is false because every row the query keeps from
/// <see cref="Skip"/> on is in <see cref="Rows"/>. There is no <c>error</c> either: a failure cannot be written into a body the
/// serializer has already been torn out of, so it is logged and aborts the response instead.
/// </para>
/// </remarks>
public class DataQueryStreamResponse
{
    private readonly DataQueryStream _stream;

    /// <summary>Initializes a new instance of the <see cref="DataQueryStreamResponse"/> class.</summary>
    /// <param name="stream">The opened stream whose rows are written.</param>
    /// <param name="skip">The rows skipped.</param>
    public DataQueryStreamResponse(DataQueryStream stream, int skip)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        Skip = skip;
        HasMoreRows = false;
    }

    /// <summary>Gets the data set queried.</summary>
    [JsonPropertyOrder(0)]
    public string DataSetName => _stream.DataSetName;

    /// <summary>Gets the data set's fields, in ordinal order.</summary>
    [JsonPropertyOrder(1)]
    public IReadOnlyList<DataQueryColumnDto> Columns => _stream.Columns;

    /// <summary>Gets the rows skipped.</summary>
    [JsonPropertyOrder(2)]
    public int Skip { get; }

    /// <summary>Gets a value indicating whether more rows follow; false, because every row was asked for.</summary>
    [JsonPropertyOrder(3)]
    public bool HasMoreRows { get; }

    /// <summary>Gets the rows, each keyed by field name.</summary>
    [JsonPropertyOrder(4)]
    public IAsyncEnumerable<IReadOnlyDictionary<string, object?>> Rows => _stream.Rows;

    /// <summary>Gets how many rows <see cref="Rows"/> carried; read after them, so it is their final count.</summary>
    [JsonPropertyOrder(5)]
    public int RowCount => _stream.RowCount;

    /// <summary>Gets a value indicating whether every row was written; only a response that finished carries it, and it is true.</summary>
    [JsonPropertyOrder(6)]
    public bool Complete => _stream.Complete;
}
