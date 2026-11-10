using System.Globalization;
using Fdw.Messages;
using Fdw.Operations.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status a caller is
/// answered. A blank pipeline name passed by the caller is a 400, not a 500.
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
    public void ABlankPipelineNameAnswersBadRequest()
        => StatusOf(OperationsLog.LatestRunsNameBlank(NullLogger.Instance)).ShouldBe(400);
}
