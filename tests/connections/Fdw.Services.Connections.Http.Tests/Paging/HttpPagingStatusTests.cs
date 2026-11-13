using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Http.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests.Paging;

/// <summary>
/// FDW-5562: a code's number band is its category, and the category is the HTTP status a caller is answered. A paging refusal the caller can correct (a style or a
/// setting the request states wrongly, a query's window) is a validation failure and answers 400; what the host's own setup must fix answers 500; what the remote
/// does answers 502. Every failure of the HTTP paging family is walked here, so a code put in the wrong band fails this table rather than a caller.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HttpPagingStatusTests
{
    private const int CallerCorrectable = 400;
    private const int HostSetup = 500;
    private const int Remote = 502;

    private static readonly (string Code, int Status, Func<ILogger, IGenericMessage> Failure)[] Failures =
    [
        ("HTTP-21010", CallerCorrectable, logger => HttpPagingLog.PagingStyleNotStated(logger, "remote")),
        ("HTTP-21011", CallerCorrectable, logger => HttpPagingLog.PagingStyleUnknown(logger, "remote", "Nope")),
        ("HTTP-21012", CallerCorrectable, logger => HttpPagingLog.PagingPropertyMissing(logger, "remote", "OffsetLimit", "PagingOffsetParameter")),
        ("HTTP-21013", CallerCorrectable, logger => HttpPagingLog.PagingPropertyNotWholeNumber(logger, "remote", "PagingPageSize", "ten", 1)),
        ("HTTP-61014", HostSetup, logger => HttpPagingLog.PagingDefaultNotDeclared(logger, "remote", "OffsetLimit", "PagingPageSize")),
        ("HTTP-21015", CallerCorrectable, logger => HttpPagingLog.PagingWindowNotAddressable(logger, "remote", "PageNumber", 7, "the skip is not a multiple of the page size")),
        ("HTTP-61016", HostSetup, logger => HttpPagingLog.PagingStyleNotForProtocol(logger, "remote", "OffsetLimit", "Soap")),
        ("HTTP-21017", CallerCorrectable, logger => HttpPagingLog.PagingSkipNegative(logger, "remote", -1)),
        ("HTTP-21018", CallerCorrectable, logger => HttpPagingLog.PagingTakeTooSmall(logger, "remote", 0, 1)),
        ("HTTP-21019", CallerCorrectable, logger => HttpPagingLog.PagingPropertyNotExpected(logger, "remote", "SingleResponse", "PagingPageSize")),
        ("HTTP-21020", CallerCorrectable, logger => HttpPagingLog.PagingConnectionNotHttp(logger, "ops", "orders")),
        ("HTTP-21021", CallerCorrectable, logger => HttpPagingLog.PagingSettingsWithoutStyle(logger, "remote/orders")),
        ("HTTP-71007", Remote, logger => HttpPagingLog.PageLargerThanAsked(logger, "remote", 2, 100)),
        ("HTTP-71008", Remote, logger => HttpPagingLog.NextPageRepeats(logger, "remote", 3, "https://remote.example/next")),
        ("HTTP-71009", Remote, logger => HttpPagingLog.NextLinkMalformed(logger, "remote", 3, "ftp://remote.example/next")),
        ("HTTP-71010", Remote, logger => HttpPagingLog.NextLinkLeavesOrigin(logger, "remote", 3, "https://other.example/next", "https://remote.example")),
        ("HTTP-71011", Remote, logger => HttpPagingLog.MaxPagesExceeded(logger, "remote", "LinkHeader", 100000)),
        ("HTTP-71012", Remote, logger => HttpPagingLog.PageBodyUnreadable(logger, "remote", 2, "$.next", "no such member")),
    ];

    [Fact]
    [Trait("Priority", "P1")]
    public void EveryPagingFailureAnswersTheStatusOfItsBand()
    {
        var wrong = new List<string>();
        foreach (var (code, status, failure) in Failures)
        {
            var message = failure(NullLogger.Instance);
            var answered = FailureCategory.Of(GenericResult.Failure(message))?.HttpStatus;
            if (!string.Equals(message.Code, code, StringComparison.Ordinal) || answered != status)
                wrong.Add($"{code}: the message carries '{message.Code}' and answers {answered}, expected {status}");
        }

        wrong.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void EveryFailureOfThePagingLogIsInTheTable()
    {
        var declared = typeof(HttpPagingLog).GetMethods()
            .Where(method => method.DeclaringType == typeof(HttpPagingLog) && method.ReturnType == typeof(IGenericMessage))
            .Select(method => method.GetCustomAttributes(inherit: false).OfType<MessageLoggingAttribute>().FirstOrDefault())
            .OfType<MessageLoggingAttribute>()
            .Where(attribute => attribute.EventId >= 20000)
            .Select(attribute => $"HTTP-{attribute.EventId}")
            .Order(StringComparer.Ordinal)
            .ToList();

        Failures.Select(row => row.Code).Order(StringComparer.Ordinal).ToList().ShouldBe(declared);
    }
}
