using System.Globalization;
using Fdw.Commands.Data.Logging;
using Fdw.Messages;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status a caller is
/// answered. A save that carries no value for a field of its key is the caller's 400, not a 500.
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
    public void ASaveWithoutAKeyValueAnswersBadRequest()
        => StatusOf(ConfigurationWriteLog.LogicalKeyValueMissing(NullLogger.Instance, "Connection", "Logical", "Name")).ShouldBe(400);
}
