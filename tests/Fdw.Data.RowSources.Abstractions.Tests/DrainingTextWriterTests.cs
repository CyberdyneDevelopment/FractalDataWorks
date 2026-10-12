using System.Text;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Abstractions.Tests;

public class DrainingTextWriterTests
{
    // Why a stream that records each write separately rather than a MemoryStream: the point of this
    // class is WHEN bytes reach the target, and a MemoryStream only shows what arrived, never in how
    // many pieces or at which moment.
    private sealed class RecordingStream : Stream
    {
        public List<byte[]> Writes { get; } = [];

        public int FlushCount { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public string Text => Encoding.UTF8.GetString(Writes.SelectMany(w => w).ToArray());

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Writes.Add(buffer.Skip(offset).Take(count).ToArray());
            return Task.CompletedTask;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            FlushCount++;
            return Task.CompletedTask;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task Drain_writes_pending_characters_to_the_target()
    {
        var target = new RecordingStream();
        await using var writer = new DrainingTextWriter(target, Encoding.UTF8, 16);

        writer.Write("hello");
        await writer.Drain(TestContext.Current.CancellationToken);

        target.Text.ShouldBe("hello");
        writer.PendingCharCount.ShouldBe(0);
        writer.DrainedByteCount.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task Writing_alone_reaches_the_target_with_nothing()
    {
        var target = new RecordingStream();
        await using var writer = new DrainingTextWriter(target, Encoding.UTF8, 16);

        writer.Write("held in memory");

        target.Writes.ShouldBeEmpty();
        writer.PendingCharCount.ShouldBe(14);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task DrainIfFull_drains_only_once_the_threshold_is_reached()
    {
        var target = new RecordingStream();
        await using var writer = new DrainingTextWriter(target, Encoding.UTF8, 10);

        writer.Write("12345");
        await writer.DrainIfFull(TestContext.Current.CancellationToken);
        target.Writes.ShouldBeEmpty();

        writer.Write("67890");
        await writer.DrainIfFull(TestContext.Current.CancellationToken);
        target.Text.ShouldBe("1234567890");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task Many_records_drain_progressively_rather_than_at_the_end()
    {
        var target = new RecordingStream();
        await using var writer = new DrainingTextWriter(target, Encoding.UTF8, 8);

        for (var i = 0; i < 50; i++)
        {
            writer.Write("record");
            await writer.DrainIfFull(TestContext.Current.CancellationToken);
            writer.PendingCharCount.ShouldBeLessThan(16);
        }

        await writer.Complete(TestContext.Current.CancellationToken);

        // Why the write count matters: one write would mean it buffered everything and emitted at the
        // end, which is the behaviour this class exists to replace.
        target.Writes.Count.ShouldBeGreaterThan(20);
        target.Text.ShouldBe(string.Concat(Enumerable.Repeat("record", 50)));
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task A_surrogate_pair_split_across_two_drains_survives_intact()
    {
        var target = new RecordingStream();
        await using var writer = new DrainingTextWriter(target, Encoding.UTF8, 1024);

        // "\U0001F600" is one character carried by two UTF-16 code units.
        writer.Write('\uD83D');
        await writer.Drain(TestContext.Current.CancellationToken);
        writer.Write('\uDE00');
        await writer.Complete(TestContext.Current.CancellationToken);

        target.Text.ShouldBe("\U0001F600");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task Flush_reaches_the_target_with_nothing_so_no_synchronous_write_happens()
    {
        var target = new RecordingStream();
        await using var writer = new DrainingTextWriter(target, Encoding.UTF8, 16);

        writer.Write("pending");

        // RecordingStream throws on any synchronous write or flush, so this passing IS the assertion
        // that nothing synchronous reached the stream.
        Should.NotThrow(() => writer.Flush());
        target.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task Disposing_the_writer_leaves_the_target_stream_open()
    {
        var target = new RecordingStream();
        var writer = new DrainingTextWriter(target, Encoding.UTF8, 16);

        writer.Write("x");
        await writer.Complete(TestContext.Current.CancellationToken);
        await writer.DisposeAsync();

        // A response body outlives the writer, so disposing must not close it.
        target.CanWrite.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_threshold_of_zero_or_less_fails_loud(int threshold)
        => Should.Throw<ArgumentOutOfRangeException>(
            () => new DrainingTextWriter(new RecordingStream(), Encoding.UTF8, threshold));

    [Fact]
    [Trait("Priority", "P1")]
    public void A_missing_target_fails_loud()
        => Should.Throw<ArgumentNullException>(() => new DrainingTextWriter(null!, Encoding.UTF8, 16));

    [Fact]
    [Trait("Priority", "P1")]
    public void A_missing_encoding_fails_loud()
        => Should.Throw<ArgumentNullException>(() => new DrainingTextWriter(new RecordingStream(), null!, 16));
}
