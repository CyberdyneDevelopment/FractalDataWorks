using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// What a host that serves no hub does once a tick (FDW-5249): the events gathered since the last tick go out in one request, oldest first;
/// a tick with nothing waiting sends nothing; a failed send is one coded failure and does not stop the next tick.
/// </summary>
[Trait("Category", "CoreFramework")]
public sealed class PipelineEventRelayFlusherTests
{
    private static readonly Guid Run = new("aaaaaaaa-0000-0000-0000-000000000001");

    private static PipelineEventRequest Status(string status)
        => new() { Kind = "StatusChanged", Status = new PipelineStatusUpdate { PipelineName = "Nightly", ExecutionId = Run, Status = status } };

    private static (PipelineEventRelayFlusher Flusher, PipelineEventRelayBuffer Buffer, RelayRecordingLogger<PipelineEventRelayFlusher> Logger) Build(
        IPipelineEventBatchClient? client)
    {
        var services = new ServiceCollection();
        if (client is not null)
        {
            services.AddScoped(_ => client);
        }

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.ApplicationName).Returns("Reference.Etl");
        var buffer = new PipelineEventRelayBuffer(100);
        var logger = new RelayRecordingLogger<PipelineEventRelayFlusher>();
        var flusher = new PipelineEventRelayFlusher(
            buffer,
            new PipelineEventRelayTuning(TimeSpan.FromMilliseconds(50), 100),
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            environment.Object,
            logger);
        return (flusher, buffer, logger);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task EverythingGatheredGoesOutInOneRequestOldestFirst()
    {
        var sent = new List<PipelineEventBatchRequest>();
        var client = new Mock<IPipelineEventBatchClient>(MockBehavior.Strict);
        client.Setup(c => c.RelayBatch(It.IsAny<PipelineEventBatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PipelineEventBatchRequest, CancellationToken>((r, _) => sent.Add(r))
            .ReturnsAsync(GenericResult<PipelineEventBatchResponse>.Success(new PipelineEventBatchResponse { Relayed = 3 }));
        var (flusher, buffer, logger) = Build(client.Object);
        var events = new[] { Status("Running"), Status("Paused"), Status("Completed") };
        foreach (var e in events)
        {
            buffer.Add(e);
        }

        await flusher.FlushOnce(TestContext.Current.CancellationToken);

        sent.Count.ShouldBe(1);
        sent[0].Events.ShouldBe(events);
        buffer.Count.ShouldBe(0);
        logger.EventIds.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ATickWithNothingWaitingSendsNothing()
    {
        var client = new Mock<IPipelineEventBatchClient>(MockBehavior.Strict);
        var (flusher, _, _) = Build(client.Object);

        await flusher.FlushOnce(TestContext.Current.CancellationToken);

        client.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedSendIsOneCodedFailureAndTheEventsAreNotSentAgain()
    {
        var client = new Mock<IPipelineEventBatchClient>(MockBehavior.Strict);
        client.Setup(c => c.RelayBatch(It.IsAny<PipelineEventBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineEventBatchResponse>.Failure(new GenericMessage("403")));
        var (flusher, buffer, logger) = Build(client.Object);
        buffer.Add(Status("Running"));

        await flusher.FlushOnce(TestContext.Current.CancellationToken);
        await flusher.FlushOnce(TestContext.Current.CancellationToken);

        logger.EventIds.ShouldBe([71315]);
        client.Verify(c => c.RelayBatch(It.IsAny<PipelineEventBatchRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASendThatThrowsIsOneCodedFailureAndDoesNotEscape()
    {
        var client = new Mock<IPipelineEventBatchClient>(MockBehavior.Strict);
        client.Setup(c => c.RelayBatch(It.IsAny<PipelineEventBatchRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var (flusher, buffer, logger) = Build(client.Object);
        buffer.Add(Status("Running"));

        await Should.NotThrowAsync(() => flusher.FlushOnce());

        logger.EventIds.ShouldBe([71316]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AHostWithNoBatchClientLogsTheMissingClientAndDoesNotThrow()
    {
        var (flusher, buffer, logger) = Build(client: null);
        buffer.Add(Status("Running"));

        await Should.NotThrowAsync(() => flusher.FlushOnce());

        logger.EventIds.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task TheBufferingClientTakesAnEventAndAnswersWithItsKind()
    {
        var buffer = new PipelineEventRelayBuffer(10);
        var client = new BufferedPipelineEventClient(buffer);
        var request = Status("Running");

        var result = await client.Relay(request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Kind.ShouldBe("StatusChanged");
        buffer.Drain().ShouldBe([request]);
    }
}
