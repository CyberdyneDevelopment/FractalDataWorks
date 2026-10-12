using Fdw.Aegis.Abstractions;

namespace Fdw.Aegis.Abstractions.Tests;

public class VerdictTests
{
    [Fact]
    [Trait("Category", "Security")]
    public void NewVerdictDefaultsToNonApprovingDisposition()
    {
        // Fail-closed: a bare Verdict must never default to an injection-permitting disposition.
        var verdict = new Verdict();

        verdict.Disposition.ShouldBeSameAs(VerdictDispositions.Deny);
        verdict.Disposition.AllowsInjection.ShouldBeFalse();
        verdict.Disposition.ShouldNotBeSameAs(VerdictDispositions.Approve);
    }
}
