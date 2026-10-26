using Fdw.Messages;

namespace Fdw.Results.Tests;

/// <summary>
/// FDW-2338: the category of a failure is read from its result code, or from the number in a message's code, the same way.
/// </summary>
public sealed class FailureCategoryTests
{
    [Fact]
    public void AFailureBuiltFromAMessageWithAMissingNumberIsMissing()
    {
        var message = new GenericMessage(MessageSeverity.Warning, "not here", "SQLTENANT-31001");

        FailureCategory.IsMissing(GenericResult.Failure(message)).ShouldBeTrue();
    }

    [Fact]
    public void AFailureBuiltFromAValidationNumberIsNotMissing()
    {
        var message = new GenericMessage(MessageSeverity.Warning, "bad input", "SQLTENANT-21000");

        FailureCategory.IsMissing(GenericResult.Failure(message)).ShouldBeFalse();
        FailureCategory.Of(GenericResult.Failure(message)).ShouldBe(ResultCategories.ByName("Validation"));
    }

    [Fact]
    public void AMessageWithNoCodeHasNoCategory()
    {
        FailureCategory.Of(GenericResult.Failure(new GenericMessage("plain"))).ShouldBeNull();
    }

    [Fact]
    public void ASuccessOrProgressNumberIsNotAFailureCategory()
    {
        var message = new GenericMessage(MessageSeverity.Information, "done", "DATA-11000");

        FailureCategory.Of(GenericResult.Failure(message)).ShouldBeNull();
    }
}
