using System;
using System.Globalization;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status a caller is
/// answered. A run row the store would not take, or a source that could not be read, is a downstream 502; a host that names no operational store is its own 500. Neither is the caller's 400.
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
    public void ARunRowTheStoreWouldNotTakeAnswersBadGateway()
        => StatusOf(PipelineRunStartLog.RunRowNotWritten(NullLogger.Instance, "p", Guid.Empty, "timeout")).ShouldBe(502);

    [Fact]
    [Trait("Priority", "P1")]
    public void AFlowSourceThatCouldNotBeReadAnswersBadGateway()
        => StatusOf(FlowLog.SourceReadFailed(NullLogger.Instance, "src", 0, "timeout")).ShouldBe(502);

    [Fact]
    [Trait("Priority", "P1")]
    public void AComputeRunWithoutAGatewayAnswersServerError()
        => StatusOf(ComputeSetLog.NoGateway(NullLogger.Instance, "stage", "ds")).ShouldBe(500);

    [Fact]
    [Trait("Priority", "P1")]
    public void AnUnnamedOperationalConnectionAnswersServerError()
        => StatusOf(PipelineFailureRuleLog.OperationalConnectionNotNamed(NullLogger.Instance, "p")).ShouldBe(500);
}
