using System;
using System.Globalization;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Logging;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status a caller is
/// answered. A request the store cannot serve is the caller's; a limit the caller passed is 400 or 429; a gateway built without its provider is the host's 500.
/// </summary>
public sealed class MessageCodeStatusTests
{
    private static int StatusOf(IGenericMessage message)
    {
        var code = message.Code.ShouldNotBeNull();
        return ResultCategories.ById(int.Parse(code[(code.LastIndexOf('-') + 1)..], NumberStyles.None, CultureInfo.InvariantCulture) / 10000).HttpStatus;
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AVectorCommandOnAStoreWithoutVectorsAnswersBadRequest()
        => StatusOf(DataGatewayLogger.VectorizedNotSupported(NullLogger.Instance, "store", "Nearest")).ShouldBe(400);

    [Fact]
    [Trait("Priority", "P1")]
    public void AGatewayWithoutItsStoreProviderAnswersServerError()
        => StatusOf(DataGatewayLogger.DataStoreProviderUnavailable(NullLogger.Instance)).ShouldBe(500);

    [Fact]
    [Trait("Priority", "P1")]
    public void ACallerOverTheRateCapAnswersTooManyRequests()
        => StatusOf(ConnectionLimitLog.RateExceeded(NullLogger.Instance, Guid.Empty, 12.5, 10)).ShouldBe(429);

    [Fact]
    [Trait("Priority", "P1")]
    public void ACallerAskingPastTheRowCapAnswersBadRequest()
        => StatusOf(ConnectionLimitLog.MaxResultSizeExceeded(NullLogger.Instance, Guid.Empty, 5000, 1000)).ShouldBe(400);
}
