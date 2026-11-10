using System.Globalization;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Users.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status a caller is
/// answered. A host that does not run the tenant service is a configuration fault (500), not a downstream 502.
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
    public void AHostWithoutTheTenantServiceAnswersServerError()
        => StatusOf(UnavailableWritersLog.TenantWriterUnavailable(NullLogger.Instance, "created")).ShouldBe(500);
}
