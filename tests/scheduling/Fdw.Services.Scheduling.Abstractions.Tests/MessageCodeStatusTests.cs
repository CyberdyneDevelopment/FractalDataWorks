using System;
using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Scheduling.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status a caller is
/// answered and whether it is retried. A lookup that answers success with no pipeline is an internal fault (500), not a downstream 502.
/// </summary>
public sealed class MessageCodeStatusTests
{
    private static int CategoryOf(IGenericMessage message)
    {
        var code = message.Code.ShouldNotBeNull();
        return int.Parse(code[(code.LastIndexOf('-') + 1)..], NumberStyles.None, CultureInfo.InvariantCulture) / 10000;
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AnEmptyPipelineLookupIsAnInternalFault()
        => CategoryOf(SchedulePipelineLog.SchedulePipelineLookupEmpty(NullLogger.Instance, "nightly", Guid.Empty)).ShouldBe(9); // Internal: 500, not retried
}
