using Fdw.Data.DataSets.Results;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

public class ImplementationInvalidCodeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Results")]
    public void TheClientMessageNamesTheWrongImplementationAndTheValidOnes()
    {
        var code = new ImplementationInvalidCode();

        var shown = code.FormatClientMessage(ImplementationInvalidCode.Describe("orders", "Bogus"));

        shown.ShouldNotBeNull();
        shown.ShouldContain("'Bogus'");
        shown.ShouldContain("Simple");
        shown.ShouldContain("Compound");
        shown.ShouldContain("Federated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Results")]
    public void ADataSetThatStatesNoImplementationIsStillRefusedWithTheValidOnes()
    {
        var shown = new ImplementationInvalidCode().FormatClientMessage(ImplementationInvalidCode.Describe("orders", null));

        shown.ShouldNotBeNull();
        shown.ShouldContain("Implementation ''");
        shown.ShouldContain("valid implementations:");
    }
}
