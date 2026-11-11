using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// The one place a connection answers a write that reports its unmatched keys (FDW-4988): the command must opt in and the caller must ask for a BulkWriteAnswer,
/// and the answer is checked as it is read.
/// </summary>
public sealed class UnmatchedKeysAnswerTests
{
    private static IDataCommand Reporting(int? sample)
    {
        var command = new Mock<IReportingDataCommand>();
        command.SetupGet(c => c.CommandType).Returns("BulkUpdate");
        command.SetupGet(c => c.UnmatchedKeySample).Returns(sample);
        return command.Object;
    }

    private static IReadOnlyList<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Sets(long unmatched, params string[] codes) =>
    [
        [new Dictionary<string, object?> { [BulkWriteAnswer.AffectedColumn] = 2L, [BulkWriteAnswer.UnmatchedColumn] = unmatched }],
        [.. System.Linq.Enumerable.Select(codes, code => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { ["Code"] = code })],
    ];

    [Fact]
    public void ACommandThatReportsOrAnAnswerResultTakesTheAnswerPath()
    {
        UnmatchedKeysAnswer.Applies<int>(Reporting(3)).ShouldBeTrue();
        UnmatchedKeysAnswer.Applies<BulkWriteAnswer>(Reporting(null)).ShouldBeTrue();
        UnmatchedKeysAnswer.Applies<int>(Reporting(null)).ShouldBeFalse();
    }

    [Fact]
    public async Task AReportingCommandIsAnsweredFromItsResultSets()
    {
        var answer = await UnmatchedKeysAnswer.Execute<BulkWriteAnswer>(
            Reporting(5), NullLogger.Instance, () => Task.FromResult(GenericResult<IReadOnlyList<IReadOnlyList<IReadOnlyDictionary<string, object?>>>>.Success(Sets(1, "A"))));

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        answer.Value!.UnmatchedKeys.ShouldBe(1L);
        answer.Value.UnmatchedSample.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AnAnswerAskedOfACommandThatDoesNotReportIsRefusedWithoutRunningIt()
    {
        var ran = false;
        var answer = await UnmatchedKeysAnswer.Execute<BulkWriteAnswer>(Reporting(null), NullLogger.Instance, () =>
        {
            ran = true;
            return Task.FromResult(GenericResult<IReadOnlyList<IReadOnlyList<IReadOnlyDictionary<string, object?>>>>.Success(Sets(0)));
        });

        answer.IsFailure.ShouldBeTrue();
        answer.CurrentMessage.ShouldNotBeNull().ShouldContain("does not report");
        ran.ShouldBeFalse();
    }

    [Fact]
    public async Task AReportingCommandRunForACountIsRefused()
    {
        var answer = await UnmatchedKeysAnswer.Execute<int>(
            Reporting(5), NullLogger.Instance, () => Task.FromResult(GenericResult<IReadOnlyList<IReadOnlyList<IReadOnlyDictionary<string, object?>>>>.Success(Sets(0))));

        answer.IsFailure.ShouldBeTrue();
        answer.CurrentMessage.ShouldNotBeNull().ShouldContain("BulkWriteAnswer");
    }

    [Fact]
    public async Task AFailureReadingTheSetsIsCarried()
    {
        var answer = await UnmatchedKeysAnswer.Execute<BulkWriteAnswer>(
            Reporting(5), NullLogger.Instance, () => Task.FromResult(GenericResult<IReadOnlyList<IReadOnlyList<IReadOnlyDictionary<string, object?>>>>.Failure(new GenericMessage("the server refused"))));

        answer.CurrentMessage.ShouldBe("the server refused");
    }
}
