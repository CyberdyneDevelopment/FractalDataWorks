using System;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="RunFailureClassification"/> (FDW-3862): a failed run's kind is read from the category its failure message's code names, and whether that category is
/// transient is the category's own retryable flag. The text is never read.
/// </summary>
public sealed class RunFailureClassificationTests
{
    private static IGenericResult Failure(string text, string? code) => GenericResult.Failure(new GenericMessage(MessageSeverity.Error, text, code));

    [Theory]
    [InlineData("ETL-70001", "Dependency", true)]
    [InlineData("ETL-80001", "Transient", true)]
    [InlineData("ETL-110001", "GatewayTimeout", true)]
    [InlineData("ETL-20001", "Validation", false)]
    [InlineData("ETL-30001", "Missing", false)]
    [InlineData("ETL-40001", "Conflict", false)]
    [InlineData("ETL-60001", "Configuration", false)]
    [InlineData("ETL-90001", "Internal", false)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AFailureIsClassifiedByTheCategoryItsCodeNamesAndTransientIsTheCategorysOwnFlag(string code, string category, bool transient)
    {
        var kind = RunFailureClassification.Of(Failure("anything", code));

        kind.Classified.ShouldBeTrue();
        kind.Category.ShouldBe(category);
        kind.Code.ShouldBe(code);
        kind.Transient.ShouldBe(transient);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheTextOfAFailureIsNeverRead()
    {
        // A message that talks about a connection failure but carries a validation code is a validation failure.
        var kind = RunFailureClassification.Of(Failure("Could not connect to the server: connection refused", "ETL-20001"));

        kind.Category.ShouldBe("Validation");
        kind.Transient.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheConnectionThatRaisedTheFailureIsRecordedWithTheKindAsText()
    {
        var connection = new Guid("cccccccc-0000-0000-0000-000000000001");

        var data = RunFailureClassification.Of(ConnectionFailures.RaisedBy<int>(Failure("x", "ETL-90001"), connection)).ToEventData();

        data["connectionId"].ShouldBe(connection.ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AFailureARaisedByConnectionKeepsItsCategoryAndCode()
    {
        var kind = RunFailureClassification.Of(ConnectionFailures.RaisedBy<int>(Failure("x", "ETL-70001"), Guid.NewGuid()));

        kind.Category.ShouldBe("Dependency");
        kind.Code.ShouldBe("ETL-70001");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AFailureNoConnectionRaisedRecordsNoConnection()
    {
        var data = RunFailureClassification.Of(Failure("x", "ETL-20001")).ToEventData();

        data["connectionId"].ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no number here")]
    [InlineData("ETL-12")]
    [InlineData("ETL-15000")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AFailureWithNoCodedFailureMessageIsUnclassifiedAndNotTransient(string? code)
    {
        var kind = RunFailureClassification.Of(Failure("it failed", code));

        kind.Classified.ShouldBeFalse();
        kind.Category.ShouldBeNull();
        kind.Transient.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AProgressMessageBeforeTheCodedFailureIsSkipped()
    {
        var result = GenericResult.Failure(
            new GenericMessage(MessageSeverity.Information, "started", "ETL-10001"),
            new GenericMessage(MessageSeverity.Error, "the source refused", "ETL-70002"));

        var kind = RunFailureClassification.Of(result);

        kind.Category.ShouldBe("Dependency");
        kind.Code.ShouldBe("ETL-70002");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AFailureInTheRefusedCategoryIsRefusedAndOneInAnyOtherCategoryIsNot()
    {
        var refused = RunFailureClassification.Of(Failure("x", "CONN-120000"));
        var conflict = RunFailureClassification.Of(Failure("x", "ETL-40000"));

        refused.Classified.ShouldBeTrue();
        refused.Category.ShouldBe("Refused");
        refused.Refused.ShouldBeTrue();
        refused.Transient.ShouldBeFalse();
        refused.ToEventData()["refused"].ShouldBe(true);
        refused.Describe().ShouldContain("is not counted");
        conflict.Category.ShouldBe("Conflict");
        conflict.Refused.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheEventCarriesTheFiveValuesAndTheMessageSaysWhetherItIsTransient()
    {
        var kind = RunFailureClassification.Of(Failure("x", "ETL-70001"));

        var data = kind.ToEventData();

        data["classified"].ShouldBe(true);
        data["category"].ShouldBe("Dependency");
        data["code"].ShouldBe("ETL-70001");
        data["transient"].ShouldBe(true);
        data["refused"].ShouldBe(false);
        kind.Describe().ShouldBe("The run failed with a Dependency failure (ETL-70001), which is transient");
        RunFailureClassification.Of(Failure("x", null)).Describe().ShouldBe("The run failed with a failure that carries no coded category");
    }
}
