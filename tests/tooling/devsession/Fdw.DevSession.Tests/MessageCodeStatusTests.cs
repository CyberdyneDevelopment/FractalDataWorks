using System;
using System.Globalization;
using Fdw.DevSession.Logging;
using Fdw.Messages;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.DevSession.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status a caller is
/// answered. An unknown session id is the caller's 404, not a 500.
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
    public void AnUnknownSessionAnswersNotFound()
        => StatusOf(DevSessionManagerLog.SessionNotFoundById(NullLogger.Instance, Guid.Empty)).ShouldBe(404);
}
