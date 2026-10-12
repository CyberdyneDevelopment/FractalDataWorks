using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Protocols.Tds.Tests;

/// <summary>
/// How messages are cut into packets and joined again: header fields, packet ids, end of message, and the
/// limits the reader enforces.
/// </summary>
public sealed class TdsMessageFramingTests
{
    private const int HeaderLength = 8;

    [Fact]
    public async Task AMessageLargerThanAPacketIsSplitAndNumberedFromOne()
    {
        using var stream = new MemoryStream();
        var writer = new TdsMessageWriter(stream, NullLogger.Instance) { SessionId = 51 };
        var payload = Enumerable.Range(0, 10_000).Select(i => (byte)i).ToArray();

        var written = await writer.Write(TdsPacketTypes.TabularResult, payload, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        var packets = Packets(stream.ToArray());
        packets.Length.ShouldBe(3);
        packets.Select(p => p[6]).ShouldBe(new byte[] { 1, 2, 3 });
        packets.Select(p => p[1]).ShouldBe(new[] { TdsPacketStatus.Normal, TdsPacketStatus.Normal, TdsPacketStatus.EndOfMessage });
        packets.Select(p => p[0]).ShouldAllBe(type => type == TdsPacketTypes.TabularResult);
        packets.Select(p => BinaryPrimitives.ReadUInt16BigEndian(p.AsSpan(4))).ShouldAllBe(spid => spid == 51);
        packets.Sum(p => p.Length - HeaderLength).ShouldBe(payload.Length);
    }

    [Fact]
    public async Task AnEmptyMessageIsOneHeaderOnlyPacket()
    {
        using var stream = new MemoryStream();
        var writer = new TdsMessageWriter(stream, NullLogger.Instance);

        await writer.Write(TdsPacketTypes.TabularResult, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken);

        var packets = Packets(stream.ToArray());
        packets.Length.ShouldBe(1);
        packets[0].Length.ShouldBe(HeaderLength);
        packets[0][1].ShouldBe(TdsPacketStatus.EndOfMessage);
    }

    [Fact]
    public async Task APartWritesOnlyWholePacketsAndIdsRunOnAcrossParts()
    {
        using var stream = new MemoryStream();
        var writer = new TdsMessageWriter(stream, NullLogger.Instance);
        var body = TdsMessageWriter.InitialPacketSize - HeaderLength;
        var payload = new byte[body + 100];

        var part = await writer.WritePart(TdsPacketTypes.TabularResult, payload, endOfMessage: false, TestContext.Current.CancellationToken);
        var rest = await writer.WritePart(TdsPacketTypes.TabularResult, payload.AsMemory(part.Value), endOfMessage: true, TestContext.Current.CancellationToken);

        part.Value.ShouldBe(body);
        rest.Value.ShouldBe(100);
        var packets = Packets(stream.ToArray());
        packets.Select(p => p[6]).ShouldBe(new byte[] { 1, 2 });
        packets[1][1].ShouldBe(TdsPacketStatus.EndOfMessage);
    }

    [Fact]
    public async Task WhatTheWriterSplitsTheReaderJoins()
    {
        using var stream = new MemoryStream();
        var payload = Enumerable.Range(0, 9_000).Select(i => (byte)(i * 7)).ToArray();
        await new TdsMessageWriter(stream, NullLogger.Instance).Write(TdsPacketTypes.SqlBatch, payload, TestContext.Current.CancellationToken);
        stream.Position = 0;

        var message = await new TdsMessageReader(stream, 1_000_000, NullLogger.Instance).Read(TestContext.Current.CancellationToken);

        message.IsSuccess.ShouldBeTrue();
        message.Value!.PacketType.ShouldBe(TdsPacketTypes.SqlBatch);
        message.Value.Payload.ShouldBe(payload);
        message.Value.ResetsConnection.ShouldBeFalse();
    }

    [Fact]
    public async Task AStreamThatEndsBeforeAnyPacketReadsAsClosed()
    {
        using var stream = new MemoryStream();

        var message = await new TdsMessageReader(stream, TdsMessageReader.SmallestLimit, NullLogger.Instance).Read(TestContext.Current.CancellationToken);

        message.IsSuccess.ShouldBeTrue();
        ReferenceEquals(message.Value, TdsMessage.Closed).ShouldBeTrue();
    }

    [Fact]
    public async Task AMessageOverTheLimitIsRefused()
    {
        using var stream = new MemoryStream();
        await new TdsMessageWriter(stream, NullLogger.Instance).Write(TdsPacketTypes.SqlBatch, new byte[TdsMessageReader.SmallestLimit + 1], TestContext.Current.CancellationToken);
        stream.Position = 0;

        var message = await new TdsMessageReader(stream, TdsMessageReader.SmallestLimit, NullLogger.Instance).Read(TestContext.Current.CancellationToken);

        message.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task APacketOfAnotherTypeInsideAMessageIsRefused()
    {
        var first = Packet(TdsPacketTypes.SqlBatch, TdsPacketStatus.Normal, new byte[10]);
        var second = Packet(TdsPacketTypes.Rpc, TdsPacketStatus.EndOfMessage, new byte[10]);
        using var stream = new MemoryStream([.. first, .. second]);

        var message = await new TdsMessageReader(stream, TdsMessageReader.SmallestLimit, NullLogger.Instance).Read(TestContext.Current.CancellationToken);

        message.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task TheResetConnectionFlagIsCarriedOnTheMessage()
    {
        using var stream = new MemoryStream(Packet(TdsPacketTypes.SqlBatch, (byte)(TdsPacketStatus.EndOfMessage | TdsPacketStatus.ResetConnection), new byte[4]));

        var message = await new TdsMessageReader(stream, TdsMessageReader.SmallestLimit, NullLogger.Instance).Read(TestContext.Current.CancellationToken);

        message.Value!.ResetsConnection.ShouldBeTrue();
    }

    private static byte[] Packet(byte type, byte status, byte[] body)
    {
        var packet = new byte[HeaderLength + body.Length];
        packet[0] = type;
        packet[1] = status;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), (ushort)packet.Length);
        packet[6] = 1;
        body.CopyTo(packet, HeaderLength);
        return packet;
    }

    private static byte[][] Packets(byte[] bytes)
    {
        var packets = new System.Collections.Generic.List<byte[]>();
        var at = 0;
        while (at < bytes.Length)
        {
            var length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(at + 2));
            packets.Add(bytes.AsSpan(at, length).ToArray());
            at += length;
        }

        return [.. packets];
    }
}
