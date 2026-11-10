using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Results.Tests;

/// <summary>
/// FDW-4785: a result does not own its details. WithDetails keeps the caller's instance when the source has none, and a copied result holds its
/// source's instance. A consumer disposing a details object must therefore never clear it or hand it to another result.
/// </summary>
public sealed class ResultDetailsSharingTests
{
    private const int Iterations = 20_000;

    private static IGenericResult Failed() => GenericResult.Failure(new GenericMessage(MessageSeverity.Error, "boom", "MSSQL-70001"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DisposingDetailsAResultHoldsLeavesTheResultsDetailsIntact()
    {
        var details = ResultDetails.Create("connectionId", "abc");
        var result = GenericResult.WithDetails(Failed(), details);

        details.Dispose();

        result.Details.ShouldNotBeNull().Data["connectionId"].ShouldBe("abc");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DisposingACopiedResultsDetailsLeavesTheSourcesDetailsIntact()
    {
        var source = GenericResult.WithDetails(Failed(), ResultDetails.Create("connectionId", "abc"));
        var copy = source.ToNewResult<string>();

        copy.Details.ShouldNotBeNull().Dispose();

        source.Details.ShouldNotBeNull().Data["connectionId"].ShouldBe("abc");
        copy.Details.ShouldNotBeNull().Data["connectionId"].ShouldBe("abc");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ADisposedDetailsObjectIsNeverIssuedToAnotherResult()
    {
        var details = ResultDetails.Create("owner", "first");
        var first = GenericResult.WithDetails(Failed(), details);
        details.Dispose();

        var second = GenericResult.WithDetails(Failed(), ResultDetails.Create("owner", "second"));

        ReferenceEquals(first.Details, second.Details).ShouldBeFalse();
        first.Details.ShouldNotBeNull().Data["owner"].ShouldBe("first");
        second.Details.ShouldNotBeNull().Data["owner"].ShouldBe("second");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ParallelCreateMergeAndDisposeKeepEveryResultsOwnDetails()
    {
        // One result whose details other threads merge from while consumers dispose them.
        var shared = GenericResult.WithDetails(Failed(), ResultDetails.Create("a", 1, "b", 2, "c", 3));

        Parallel.For(0, Iterations, i =>
        {
            if (i % 4 == 0)
            {
                shared.Details.ShouldNotBeNull().Dispose();
            }

            var merged = GenericResult.WithDetails(shared, ResultDetails.Create("x", i));
            var mergedData = merged.Details.ShouldNotBeNull().Data;
            mergedData.Count.ShouldBe(4);
            mergedData["a"].ShouldBe(1);
            mergedData["x"].ShouldBe(i);

            var own = ResultDetails.Create("owner", i);
            var result = GenericResult.WithDetails(Failed(), own);
            own.Dispose();
            var other = GenericResult.WithDetails(Failed(), ResultDetails.Create("owner", -i - 1));

            result.Details.ShouldNotBeNull().Data["owner"].ShouldBe(i);
            other.Details.ShouldNotBeNull().Data["owner"].ShouldBe(-i - 1);
        });

        shared.Details.ShouldNotBeNull().Data.Count.ShouldBe(3);
    }
}
