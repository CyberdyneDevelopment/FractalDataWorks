using System;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// The streaming limits have no defaults: a limit that was never configured, or was configured as zero, is a
/// configuration error the host must hear about at startup — not a stream that runs under a value nobody chose.
/// </summary>
public sealed class ResponseStreamingOptionsTests
{
    [Fact]
    public void ConfiguredLimitsAreCarriedAsGiven()
    {
        var options = new ResponseStreamingOptions(maxRows: 1_000_000, maxConcurrentStreams: 4);

        options.MaxRows.ShouldBe(1_000_000);
        options.MaxConcurrentStreams.ShouldBe(4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ARowCeilingOfZeroOrLessFailsLoud(long maxRows)
        => Should.Throw<ArgumentOutOfRangeException>(() => new ResponseStreamingOptions(maxRows, 4))
            .ParamName.ShouldBe("maxRows");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AConcurrencyLimitOfZeroOrLessFailsLoud(int maxConcurrentStreams)
        => Should.Throw<ArgumentOutOfRangeException>(() => new ResponseStreamingOptions(10, maxConcurrentStreams))
            .ParamName.ShouldBe("maxConcurrentStreams");
}
