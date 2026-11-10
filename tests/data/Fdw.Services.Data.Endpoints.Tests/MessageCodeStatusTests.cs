using System;
using System.Globalization;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status a caller is
/// answered. A stored stand-in without its strategy row is a data fault (500), not a downstream 502.
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
    public void AStandInWithoutAStrategyAnswersServerError()
        => StatusOf(StandInEndpointLog.StandInHasNoStrategy(NullLogger.Instance, "Amount", Guid.Empty)).ShouldBe(500);
}
