using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Messaging.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// FDW-2431: a list answers each row's alert fields. The alert service is a double that knows which rows are alerts, so these tests are about what the
/// helper puts on a row, what it leaves alone, and what it refuses.
/// </summary>
public sealed class AlertListFieldsTests
{
    private const string Kind = "ScheduleFailure";

    private static string List(params (Guid Id, string State)[] rows)
        => new JsonObject
        {
            ["items"] = new JsonArray([.. rows.Select(row => (JsonNode)new JsonObject { ["id"] = row.Id.ToString("D"), ["state"] = row.State, ["name"] = "run" })]),
            ["page"] = 1,
            ["totalCount"] = rows.Length,
        }.ToJsonString();

    private static AlertSummary Summary(Guid id) => new()
    {
        Kind = Kind,
        Id = id,
        Shown = true,
        Actions = [new AlertActionOffer { Name = "Acknowledge", DisplayName = "Acknowledge", Description = "Say you have seen it" }],
    };

    private static Mock<IAlertActionService> Alerts(params Guid[] knownAlerts)
    {
        var alerts = new Mock<IAlertActionService>(MockBehavior.Strict);
        alerts
            .Setup(a => a.List(It.IsAny<IReadOnlyList<AlertKey>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AlertKey> keys, CancellationToken _) => GenericResult<AlertList>.Success(new AlertList
            {
                Items = [.. keys.Where(key => knownAlerts.Contains(key.Id)).Select(key => Summary(key.Id))],
                Missing = [.. keys.Where(key => !knownAlerts.Contains(key.Id))],
            }));
        return alerts;
    }

    [Fact]
    public async Task ARowThatIsAnAlertGetsItsAlertFieldsAndOtherRowsAreLeftAsTheyWere()
    {
        var failed = Guid.NewGuid();
        var running = Guid.NewGuid();
        var alerts = Alerts(failed);

        var answer = await AlertListFields.Attach(alerts.Object, Kind, List((failed, "Failed"), (running, "Running")), NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        var items = JsonNode.Parse(answer.Value!)!["items"]!.AsArray();
        var first = items[0]!.AsObject();
        first["state"]!.GetValue<string>().ShouldBe("Failed");
        var alert = first["alert"]!.AsObject();
        alert["kind"]!.GetValue<string>().ShouldBe(Kind);
        alert["id"]!.GetValue<string>().ShouldBe(failed.ToString("D"));
        alert["shown"]!.GetValue<bool>().ShouldBeTrue();
        alert["actions"]!.AsArray().Single()!["name"]!.GetValue<string>().ShouldBe("Acknowledge");
        items[1]!.AsObject().ContainsKey("alert").ShouldBeFalse();
    }

    [Fact]
    public async Task TheRestOfTheListIsKeptAsItWas()
    {
        var failed = Guid.NewGuid();

        var answer = await AlertListFields.Attach(Alerts(failed).Object, Kind, List((failed, "Failed")), NullLogger.Instance, TestContext.Current.CancellationToken);

        var list = JsonNode.Parse(answer.Value!)!.AsObject();
        list["page"]!.GetValue<int>().ShouldBe(1);
        list["totalCount"]!.GetValue<int>().ShouldBe(1);
        list["items"]![0]!["name"]!.GetValue<string>().ShouldBe("run");
    }

    [Fact]
    public async Task EachRowIsAskedOfTheKindNamed()
    {
        var failed = Guid.NewGuid();
        var alerts = Alerts(failed);

        await AlertListFields.Attach(alerts.Object, Kind, List((failed, "Failed")), NullLogger.Instance, TestContext.Current.CancellationToken);

        alerts.Verify(a => a.List(It.Is<IReadOnlyList<AlertKey>>(keys => keys.Count == 1 && keys[0].Kind == Kind && keys[0].Id == failed), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task MoreRowsThanOneQuestionMayNameAreAskedInChunks()
    {
        var rows = Enumerable.Range(0, AlertLimits.MaxAlerts + 30).Select(_ => (Guid.NewGuid(), "Failed")).ToArray();
        var alerts = Alerts(rows.Select(row => row.Item1).ToArray());

        var answer = await AlertListFields.Attach(alerts.Object, Kind, List(rows), NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        alerts.Verify(a => a.List(It.Is<IReadOnlyList<AlertKey>>(keys => keys.Count == AlertLimits.MaxAlerts), It.IsAny<CancellationToken>()), Times.Once());
        alerts.Verify(a => a.List(It.Is<IReadOnlyList<AlertKey>>(keys => keys.Count == 30), It.IsAny<CancellationToken>()), Times.Once());
        JsonNode.Parse(answer.Value!)!["items"]!.AsArray().Count(row => row!.AsObject().ContainsKey("alert")).ShouldBe(rows.Length);
    }

    [Fact]
    public async Task AnEmptyListAsksNothingAndIsReturnedAsItWas()
    {
        var alerts = new Mock<IAlertActionService>(MockBehavior.Strict);

        var answer = await AlertListFields.Attach(alerts.Object, Kind, List(), NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        JsonNode.Parse(answer.Value!)!["items"]!.AsArray().Count.ShouldBe(0);
        alerts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AKindThatDoesNotExistFailsTheAnswerWithTheServicesRefusal()
    {
        var alerts = new Mock<IAlertActionService>(MockBehavior.Strict);
        alerts
            .Setup(a => a.List(It.IsAny<IReadOnlyList<AlertKey>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<AlertList>.Failure(new GenericMessage("no such kind")));

        var answer = await AlertListFields.Attach(alerts.Object, "Nope", List((Guid.NewGuid(), "Failed")), NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"rows\": []}")]
    [InlineData("{\"items\": 3}")]
    [InlineData("not json at all")]
    public async Task AnAnswerThatIsNotAListWithItemsFailsAndNothingIsAsked(string body)
    {
        var alerts = new Mock<IAlertActionService>(MockBehavior.Strict);

        var answer = await AlertListFields.Attach(alerts.Object, Kind, body, NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        alerts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ARowWithoutAGuidIdentifierFailsTheAnswer()
    {
        var alerts = new Mock<IAlertActionService>(MockBehavior.Strict);
        var body = new JsonObject { ["items"] = new JsonArray(new JsonObject { ["id"] = "not-a-guid" }) }.ToJsonString();

        var answer = await AlertListFields.Attach(alerts.Object, Kind, body, NullLogger.Instance, TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        alerts.VerifyNoOtherCalls();
    }
}
