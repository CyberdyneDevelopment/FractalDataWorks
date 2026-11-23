namespace Fdw.Data.Options.Tests;

/// <summary>FDW-6454: the owner kinds and value kinds of a data configuration rule are type collections, each kind registered.</summary>
public sealed class DataOptionKindsTests
{

    [Fact]
    public void TheOwnerKindsAreTheOrganizationAndTheWorkspace()
    {
        DataOptionOwnerKinds.All().Select(kind => kind.Name).ShouldBe(["Organization", "Workspace"], ignoreOrder: true);
    }

    [Theory]
    [InlineData("Literal", "value", typeof(LiteralValueKind))]
    [InlineData("Parameter", "parameter", typeof(ParameterValueKind))]
    public void AValueKindResolvesByNameStatesItsMemberAndIsNotTheNotFoundSentinel(string name, string member, Type expected)
    {
        var kind = DataOptionValueKinds.ByName(name);

        ReferenceEquals(kind, DataOptionValueKinds.NotFound).ShouldBeFalse();
        kind.ShouldBeOfType(expected);
        kind.Member.ShouldBe(member);
    }

    [Fact]
    public void TheValueKindsAreTheLiteralAndTheParameter()
    {
        DataOptionValueKinds.All().Select(kind => kind.Name).ShouldBe(["Literal", "Parameter"], ignoreOrder: true);
    }

    [Theory]
    [InlineData("Holder", false)]
    [InlineData("NoLongerHolder", true)]
    public void ACustodianStateStatesWhetherARuleInItNeedsAttention(string name, bool needsAttention)
    {
        DataOptionCustodianStates.ByName(name).NeedsAttention.ShouldBe(needsAttention);
    }

    [Fact]
    public void TheCustodianStatesAreHolderAndNoLongerHolder()
    {
        DataOptionCustodianStates.All().Select(state => state.Name).ShouldBe(["Holder", "NoLongerHolder"], ignoreOrder: true);
    }
}
