using System;
using System.Collections.Generic;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for <see cref="FlowFailureReason"/>: a script step's reason is the first failure message, and a failure with none says so (FDW-3302).
/// </summary>
public class FlowFailureReasonTests
{
    private static GenericMessage Coded(string code, string text) => new(MessageSeverity.Error, text, code);

    private static Mock<ILogger> EnabledLogger()
    {
        var logger = new Mock<ILogger>();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        return logger;
    }

    private static void VerifyNoCodedMessageEvents(Mock<ILogger> logger, Times times)
        => logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.Is<EventId>(id => id.Id == 20936),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void OfTakesTheFirstFailureMessageAndRecordsNothing()
    {
        var logger = EnabledLogger();
        IGenericResult failed = GenericResult.Failure(Coded("MSSQL-81010", "timed out on connection 'Main'"), Coded("MSSQL-81001", "SQL query timeout"));

        var reason = FlowFailureReason.Of(failed, logger.Object, "purge", "Warehouse");

        reason.ShouldBe("timed out on connection 'Main'");
        VerifyNoCodedMessageEvents(logger, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void OfSaysSoAndListsTheMessagesWhenNoneIsInAFailureCategoryAndRecordsIt()
    {
        var logger = EnabledLogger();
        IGenericResult failed = GenericResult.Failure(new GenericMessage("script refused"), Coded("FDW-11036", "starting"));

        var reason = FlowFailureReason.Of(failed, logger.Object, "purge", "Warehouse");

        reason.ShouldContain("no coded failure message");
        reason.ShouldContain("no code: script refused");
        reason.ShouldContain("FDW-11036: starting");
        VerifyNoCodedMessageEvents(logger, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void OfSaysSoWhenTheFailureCarriedNoMessagesAtAll()
    {
        var logger = EnabledLogger();
        IGenericResult failed = GenericResult.Failure(new List<IGenericMessage>());

        var reason = FlowFailureReason.Of(failed, logger.Object, "purge", "Warehouse");

        reason.ShouldContain("no coded failure message");
        reason.ShouldContain("no messages");
        VerifyNoCodedMessageEvents(logger, Times.Once());
    }
}
