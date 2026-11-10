using System;
using System.Collections;
using System.Data;
using System.Data.Common;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// A reader over a table that counts the rows read from it and can fail on a stated read, as a store failing part way through
/// does (FDW-4903). It says whether it was closed, so a test can see that whoever owned it released it.
/// </summary>
internal sealed class CountingReader : DbDataReader
{
    private readonly DataTableReader _inner;
    private readonly int? _failOnRead;

    /// <summary>Initializes a new instance of the <see cref="CountingReader"/> class.</summary>
    /// <param name="table">The rows to read.</param>
    /// <param name="failOnRead">The read (counting from one) that throws as the store failing; none never fails.</param>
    public CountingReader(DataTable table, int? failOnRead = null)
    {
        _inner = table.CreateDataReader();
        _failOnRead = failOnRead;
    }

    /// <summary>Gets how many times a row was asked for.</summary>
    public int Reads { get; private set; }

    /// <summary>Gets a value indicating whether the reader was closed or disposed.</summary>
    public bool Released { get; private set; }

    public override bool Read()
    {
        Reads++;
        if (Reads == _failOnRead)
        {
            throw new InvalidOperationException("the store went away");
        }

        return _inner.Read();
    }

    public override void Close()
    {
        Released = true;
        _inner.Close();
    }

    protected override void Dispose(bool disposing)
    {
        Released = true;
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override int Depth => _inner.Depth;

    public override int FieldCount => _inner.FieldCount;

    public override bool HasRows => _inner.HasRows;

    public override bool IsClosed => _inner.IsClosed;

    public override int RecordsAffected => _inner.RecordsAffected;

    public override object this[int ordinal] => _inner[ordinal];

    public override object this[string name] => _inner[name];

    public override bool GetBoolean(int ordinal) => _inner.GetBoolean(ordinal);

    public override byte GetByte(int ordinal) => _inner.GetByte(ordinal);

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
        _inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

    public override char GetChar(int ordinal) => _inner.GetChar(ordinal);

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
        _inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

    public override string GetDataTypeName(int ordinal) => _inner.GetDataTypeName(ordinal);

    public override DateTime GetDateTime(int ordinal) => _inner.GetDateTime(ordinal);

    public override decimal GetDecimal(int ordinal) => _inner.GetDecimal(ordinal);

    public override double GetDouble(int ordinal) => _inner.GetDouble(ordinal);

    public override IEnumerator GetEnumerator() => _inner.GetEnumerator();

    public override Type GetFieldType(int ordinal) => _inner.GetFieldType(ordinal);

    public override float GetFloat(int ordinal) => _inner.GetFloat(ordinal);

    public override Guid GetGuid(int ordinal) => _inner.GetGuid(ordinal);

    public override short GetInt16(int ordinal) => _inner.GetInt16(ordinal);

    public override int GetInt32(int ordinal) => _inner.GetInt32(ordinal);

    public override long GetInt64(int ordinal) => _inner.GetInt64(ordinal);

    public override string GetName(int ordinal) => _inner.GetName(ordinal);

    public override int GetOrdinal(string name) => _inner.GetOrdinal(name);

    public override string GetString(int ordinal) => _inner.GetString(ordinal);

    public override object GetValue(int ordinal) => _inner.GetValue(ordinal);

    public override int GetValues(object[] values) => _inner.GetValues(values);

    public override bool IsDBNull(int ordinal) => _inner.IsDBNull(ordinal);

    public override bool NextResult() => _inner.NextResult();
}
