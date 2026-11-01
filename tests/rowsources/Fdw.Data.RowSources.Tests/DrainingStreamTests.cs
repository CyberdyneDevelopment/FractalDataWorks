using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Tests;

/// <summary>FDW-1298: bytes are collected synchronously and sent only when the caller drains.</summary>
public sealed class DrainingStreamTests
{
    [Fact]
    public async Task NothingReachesTheTargetUntilTheThresholdIsDrained()
    {
        using var target = new MemoryStream();
        var stream = new DrainingStream(target, 8);

        stream.Write(new byte[] { 1, 2, 3 }, 0, 3);
        await stream.DrainIfFull(TestContext.Current.CancellationToken);
        target.Length.ShouldBe(0);

        stream.Write(new byte[] { 4, 5, 6, 7, 8 }, 0, 5);
        await stream.DrainIfFull(TestContext.Current.CancellationToken);
        target.ToArray().ShouldBe(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        stream.PendingByteCount.ShouldBe(0);
        stream.DrainedByteCount.ShouldBe(8);
    }

    [Fact]
    public async Task AWriteLargerThanTheThresholdIsKeptWhole()
    {
        using var target = new MemoryStream();
        var stream = new DrainingStream(target, 4);

        stream.Write(new byte[100], 0, 100);
        await stream.Complete(TestContext.Current.CancellationToken);

        target.Length.ShouldBe(100);
    }

    [Fact]
    public void ItIsWriteOnlyAndRefusesAZeroThreshold()
    {
        var stream = new DrainingStream(new MemoryStream(), 4);

        stream.CanWrite.ShouldBeTrue();
        stream.CanRead.ShouldBeFalse();
        stream.CanSeek.ShouldBeFalse();
        Should.Throw<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => new DrainingStream(new MemoryStream(), 0));
    }
}
