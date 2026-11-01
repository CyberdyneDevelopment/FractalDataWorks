using System;
using Fdw.Protocols.Tds;
using Fdw.Protocols.Tds.Types;
using Shouldly;
using Xunit;

namespace Fdw.Protocols.Tds.Tests;

/// <summary>
/// Every value this server writes, read back by the reader a client's RPC parameters go through: the two
/// sides of TYPE_INFO and of each type's value encoding must agree, or a client reads something else.
/// </summary>
public sealed class TdsValueRoundTripTests
{
    public static TheoryData<string, int, byte, byte, object?, object?> Values => new()
    {
        { "Int", 0, 0, 0, 42, 42 },
        { "Int", 0, 0, 0, null, null },
        { "BigInt", 0, 0, 0, 9_000_000_000L, 9_000_000_000L },
        { "SmallInt", 0, 0, 0, (short)-7, (short)-7 },
        { "TinyInt", 0, 0, 0, (byte)200, (byte)200 },
        { "Bit", 0, 0, 0, true, true },
        { "Float", 0, 0, 0, 3.5d, 3.5d },
        { "Real", 0, 0, 0, 1.25f, 1.25f },
        { "Decimal", 0, 10, 2, 123.45m, 123.45m },
        { "Decimal", 0, 38, 4, -98765432109876543210.1234m, -98765432109876543210.1234m },
        { "NVarChar", 50, 0, 0, "héllo wörld", "héllo wörld" },
        { "NVarChar", 50, 0, 0, string.Empty, string.Empty },
        { "VarBinary", 10, 0, 0, new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 } },
        { "UniqueIdentifier", 0, 0, 0, Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff"), Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff") },
        { "Date", 0, 0, 0, new DateOnly(2026, 10, 1), new DateTime(2026, 10, 1) },
        { "Time", 0, 0, 7, new TimeSpan(0, 13, 45, 30).Add(TimeSpan.FromTicks(1_234_567)), new TimeSpan(0, 13, 45, 30).Add(TimeSpan.FromTicks(1_234_567)) },
        { "DateTime2", 0, 0, 7, new DateTime(2026, 10, 1, 13, 45, 30).AddTicks(1_234_567), new DateTime(2026, 10, 1, 13, 45, 30).AddTicks(1_234_567) },
        { "DateTimeOffset", 0, 0, 7, new DateTimeOffset(2026, 10, 1, 13, 45, 30, TimeSpan.FromHours(-5)), new DateTimeOffset(2026, 10, 1, 13, 45, 30, TimeSpan.FromHours(-5)) },
    };

    public static TheoryData<string, int, byte, byte, object> Unwritable => new()
    {
        { "Int", 0, 0, 0, 1.5d },
        { "TinyInt", 0, 0, 0, 300 },
        { "Decimal", 0, 5, 2, 123.456m },
        { "Decimal", 0, 5, 2, 12345.6m },
        { "NVarChar", 5, 0, 0, "longer than five" },
        { "UniqueIdentifier", 0, 0, 0, "not a guid" },
    };

    [Theory]
    [MemberData(nameof(Values))]
    public void AValueReadsBackAsItWasWritten(string typeName, int length, byte precision, byte scale, object? written, object? expected)
    {
        var type = new TdsColumnType(SqlDataTypes.ByName(typeName), length, precision, scale);
        TdsCollation.TryGet("SQL_Latin1_General_CP1_CI_AS", out var collation).ShouldBeTrue();
        var writer = new TdsPayloadWriter(64);

        type.DataType.WriteTypeInfo(writer, type, collation);
        type.DataType.TryWriteValue(writer, type, written).ShouldBeTrue();

        var reader = new TdsPayloadReader(writer.ToArray());
        TdsTypeInfo.TryRead(reader, out var info).ShouldBeTrue();
        TdsValueReader.TryRead(reader, info, out var read, out var problem).ShouldBeTrue(problem);
        read.ShouldBe(expected);
        reader.Remaining.ShouldBe(0);
        reader.Overrun.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(Unwritable))]
    public void AValueTheColumnCannotHoldIsNotWritten(string typeName, int length, byte precision, byte scale, object value)
    {
        var type = new TdsColumnType(SqlDataTypes.ByName(typeName), length, precision, scale);

        type.DataType.TryWriteValue(new TdsPayloadWriter(64), type, value).ShouldBeFalse();
    }

    [Fact]
    public void ADoneTokenIsThirteenBytesWithItsRowCount()
    {
        var writer = new TdsPayloadWriter(16);

        TdsTokenWriter.Done(writer, TdsTokenTypes.Done, (ushort)(TdsDoneStatus.More | TdsDoneStatus.Count), 0xC1, 1234);

        var reader = new TdsPayloadReader(writer.ToArray());
        reader.Length.ShouldBe(13);
        reader.ReadByte().ShouldBe(TdsTokenTypes.Done);
        reader.ReadUInt16().ShouldBe((ushort)(TdsDoneStatus.More | TdsDoneStatus.Count));
        reader.ReadUInt16().ShouldBe((ushort)0xC1);
        reader.ReadInt64().ShouldBe(1234L);
    }

    [Theory]
    [InlineData("16.0.4135.4", true)]
    [InlineData("16.0.4135", false)]
    [InlineData("sixteen.0.1.2", false)]
    [InlineData("300.0.1.2", false)]
    public void AServerVersionParsesOnlyWithFourParts(string text, bool parses)
    {
        TdsServerVersion.TryParse(text, out _).ShouldBe(parses);
    }
}
