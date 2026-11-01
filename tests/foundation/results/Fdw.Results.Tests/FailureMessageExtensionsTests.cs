using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Results.Tests;

/// <summary>
/// Tests for <see cref="FailureMessageExtensions.FirstFailureMessage"/>: the one reader of which message of a failed result says why it failed (FDW-3302).
/// </summary>
public class FailureMessageExtensionsTests
{
    private static GenericMessage Coded(string code, string text) => new(MessageSeverity.Error, text, code);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FirstFailureMessageTakesTheFirstFailureCategoryMessageAndNotTheLast()
    {
        var result = GenericResult.Failure(
            Coded("MSSQL-81010", "the command ran longer than the command timeout of connection 'Main', 30 seconds"),
            Coded("MSSQL-81001", "SQL query timeout"));

        var found = result.FirstFailureMessage().ShouldNotBeNull();

        found.Code.ShouldBe("MSSQL-81010");
        found.Number.ShouldBe(81010);
        found.Message.Message.ShouldContain("connection 'Main'");
        result.CurrentMessage.ShouldBe("SQL query timeout");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FirstFailureMessageSkipsProgressAndSuccessCodes()
    {
        var result = GenericResult.Failure(
            Coded("FDW-11036", "starting"),
            Coded("FDW-19999", "still progress"),
            Coded("FDW-21023", "no input rows"));

        var found = result.FirstFailureMessage().ShouldNotBeNull();

        found.Number.ShouldBe(21023);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FirstFailureMessageSkipsMessagesWithNoCodeOrANumberItCannotRead()
    {
        var result = GenericResult.Failure(
            new GenericMessage("script refused"),
            Coded("NODASH", "not a code"),
            Coded("FDW-abc", "no number"),
            Coded("FDW-20000", "the first failure number"));

        var found = result.FirstFailureMessage().ShouldNotBeNull();

        found.Number.ShouldBe(20000);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FirstFailureMessageGivesNothingWhenNoMessageIsInAFailureCategory()
    {
        var result = GenericResult.Failure(Coded("FDW-11036", "starting"), new GenericMessage("script refused"));

        result.FirstFailureMessage().ShouldBeNull();
    }
}
