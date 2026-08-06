using Fdw.Aegis.Abstractions;

namespace Fdw.Aegis.Abstractions.Tests;

public class VerdictDispositionsTests
{
    [Fact]
    [Trait("Category", "Security")]
    public void ByNameApproveReturnsTheOption()
    {
        var result = VerdictDispositions.ByName("Approve");

        result.ShouldNotBeSameAs(VerdictDispositions.NotFound);
        result.Name.ShouldBe("Approve");
        result.AllowsInjection.ShouldBeTrue();
        result.IsTerminal.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Security")]
    public void ByNameUnknownReturnsNotFoundSentinelNeverNull()
    {
        var result = VerdictDispositions.ByName("SomethingThatDoesNotExist");

        result.ShouldNotBeNull();
        result.ShouldBeSameAs(VerdictDispositions.NotFound);
    }

    [Fact]
    [Trait("Category", "Security")]
    public void OnlyApproveAllowsInjection()
    {
        VerdictDispositions.ByName("Deny").AllowsInjection.ShouldBeFalse();
        VerdictDispositions.ByName("Abstain").AllowsInjection.ShouldBeFalse();
        VerdictDispositions.ByName("Pending").AllowsInjection.ShouldBeFalse();
    }
}
