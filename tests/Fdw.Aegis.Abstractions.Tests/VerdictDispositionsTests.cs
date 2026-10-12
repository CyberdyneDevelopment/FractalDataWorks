using Fdw.Aegis.Abstractions;

namespace Fdw.Aegis.Abstractions.Tests;

public class VerdictDispositionsTests
{
    [Fact]
    [Trait("Category", "Security")]
    public void OnlyApproveAllowsInjection()
    {
        VerdictDispositions.ByName("Deny").AllowsInjection.ShouldBeFalse();
        VerdictDispositions.ByName("Abstain").AllowsInjection.ShouldBeFalse();
        VerdictDispositions.ByName("Pending").AllowsInjection.ShouldBeFalse();
    }
}
