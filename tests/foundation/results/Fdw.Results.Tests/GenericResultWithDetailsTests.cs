using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Results.Tests;

/// <summary>
/// FDW-4279: <c>WithDetails</c> copies a result and adds details to it, keeping the code, messages and chain.
/// </summary>
public sealed class GenericResultWithDetailsTests
{
    private static IGenericResult Failed() => GenericResult.Failure(new GenericMessage(MessageSeverity.Error, "boom", "MSSQL-70001"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AFailureKeepsItsMessagesAndCodeAndGainsTheDetails()
    {
        var source = Failed();

        var copy = GenericResult.WithDetails(source, ResultDetails.Create("connectionId", "abc"));

        copy.IsFailure.ShouldBeTrue();
        copy.Messages.Count.ShouldBe(source.Messages.Count);
        copy.Messages[0].Code.ShouldBe("MSSQL-70001");
        copy.Details.ShouldNotBeNull().Data["connectionId"].ShouldBe("abc");
        source.Details.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheGivenDetailsAreMergedOverTheSourcesAndTheSourcesOtherKeysStay()
    {
        var source = GenericResult.WithDetails(Failed(), ResultDetails.Create("a", 1, "b", 2));

        var copy = GenericResult.WithDetails(source, ResultDetails.Create("b", 3));

        var data = copy.Details.ShouldNotBeNull().Data;
        data["a"].ShouldBe(1);
        data["b"].ShouldBe(3);
        source.Details.ShouldNotBeNull().Data["b"].ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ATypedCopyIsAFailureWithTheSameMessagesAndTheDetails()
    {
        var typed = GenericResult<int>.WithDetails(Failed(), ResultDetails.Create("connectionId", "abc"));

        typed.IsFailure.ShouldBeTrue();
        typed.Messages[0].Code.ShouldBe("MSSQL-70001");
        typed.Details.ShouldNotBeNull().Data["connectionId"].ShouldBe("abc");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheDetailsSurviveToNewResult()
    {
        var typed = GenericResult.WithDetails(Failed(), ResultDetails.Create("connectionId", "abc")).ToNewResult<string>();

        typed.Details.ShouldNotBeNull().Data["connectionId"].ShouldBe("abc");
    }
}
