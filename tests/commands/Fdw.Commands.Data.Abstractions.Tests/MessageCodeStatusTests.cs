using System.Globalization;
using Fdw.Commands.Data.Abstractions.Logging;
using Fdw.Messages;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Abstractions.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status a caller is
/// answered. A delete that states no rows is the caller's 400, not a 500.
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
    public void ADeleteThatStatesNoRowsAnswersBadRequest()
        => StatusOf(DeleteScopeLog.Refused(NullLogger.Instance, "Orders", "no filter")).ShouldBe(400);
}
