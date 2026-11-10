using System.Globalization;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Settings.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Settings.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status a caller is
/// answered. A body that leaves out a caller's choice is the caller's mistake: 400, not 500.
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
    public void AChoiceTheCallerLeftOutAnswersBadRequest()
        => StatusOf(InstanceDefaultLog.ChoiceNotStated(NullLogger.Instance, "Connection", "c1", "Mode")).ShouldBe(400);
}
