using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Results.Tests;

/// <summary>
/// A result made through <c>Failure</c> or <c>Chain</c> is a failure whatever the severity of its code (FDW-3463). Severity says how loud
/// a log line is; it does not say whether the call succeeded. Before, <c>IsSuccess</c> came from <c>code.Severity.IsSuccess</c>, so a
/// code with a Trace, Debug, Information or Warning severity made <c>GenericResult&lt;T&gt;.Failure(code)</c> report success with a default value.
/// </summary>
public sealed class GenericResultFailureSeverityTests
{
    private readonly ILogger _logger = NullLogger.Instance;

    private static Mock<IResultCode> CodeWithSeverity(string severityName)
    {
        var code = new Mock<IResultCode>();
        code.Setup(c => c.Severity).Returns(ResultSeverities.ByName(severityName));
        code.Setup(c => c.Code).Returns("TEST-1");
        code.Setup(c => c.Domain).Returns("Test");
        code.Setup(c => c.FormatMessage(It.IsAny<IResultDetails?>())).Returns("Formatted message");
        return code;
    }

    private static IGenericResult Inner() => GenericResult.Failure(new GenericMessage("inner"));

    public static TheoryData<string> Severities => new() { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };

    private static void ShouldBeAFailureThatReadsAsOne(IGenericResult result, IResultCode code)
    {
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Status.Name.ShouldBe("Failure");
        result.Code.ShouldBe(code);
        result.Messages[0].ShouldBeOfType<GenericMessage>().Severity.ShouldBe(MessageSeverity.Error);
    }

    [Theory]
    [MemberData(nameof(Severities))]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FailureWithACodeIsAFailureWhateverTheSeverity(string severity)
    {
        var code = CodeWithSeverity(severity).Object;

        ShouldBeAFailureThatReadsAsOne(GenericResult.Failure(code), code);
        ShouldBeAFailureThatReadsAsOne(GenericResult.Failure(code, _logger), code);
    }

    [Theory]
    [MemberData(nameof(Severities))]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ChainWithACodeIsAFailureWhateverTheSeverity(string severity)
    {
        var code = CodeWithSeverity(severity).Object;

        ShouldBeAFailureThatReadsAsOne(GenericResult.Chain(code, Inner()), code);
        ShouldBeAFailureThatReadsAsOne(GenericResult.Chain(code, Inner(), _logger), code);
    }

    [Theory]
    [MemberData(nameof(Severities))]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericFailureWithACodeIsAFailureWithNoValueWhateverTheSeverity(string severity)
    {
        var code = CodeWithSeverity(severity).Object;

        foreach (var result in new[]
        {
            GenericResult<int>.Failure(code),
            GenericResult<int>.Failure(code, _logger),
            GenericResult<int>.Chain(code, Inner()),
            GenericResult<int>.Chain(code, Inner(), _logger),
        })
        {
            ShouldBeAFailureThatReadsAsOne(result, code);
            result.IsEmpty.ShouldBeTrue();
            Should.Throw<InvalidOperationException>(() => _ = result.Value);
        }
    }

    [Theory]
    [InlineData("Trace")]
    [InlineData("Debug")]
    [InlineData("Information")]
    [InlineData("Warning")]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SuccessWithAQuietCodeStillSucceedsAndCarriesItsValue(string severity)
    {
        var code = CodeWithSeverity(severity).Object;

        var plain = GenericResult.Success(code);
        var withValue = GenericResult<int>.Success(7, code);

        plain.IsSuccess.ShouldBeTrue();
        plain.Messages[0].ShouldBeOfType<GenericMessage>().Severity.ShouldBe(MessageSeverity.Information);
        withValue.IsSuccess.ShouldBeTrue();
        withValue.Value.ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AFailureWithACodeStillRefusesANullCodeNamingIt()
    {
        Should.Throw<ArgumentNullException>(() => GenericResult.Failure((IResultCode)null!)).ParamName.ShouldBe("code");
        Should.Throw<ArgumentNullException>(() => GenericResult.Success((IResultCode)null!)).ParamName.ShouldBe("code");
    }
}
