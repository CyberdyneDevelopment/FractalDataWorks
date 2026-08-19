using System.Threading.Tasks;
using Xunit;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// Covers where FDW046 stops for want of a readable value: a style the markup computes from data is not
/// reported, because no CSS class could carry it. A style that picks between written-out literals is
/// read through those literals, so each alternative is judged as the declaration list it is.
/// </summary>
/// <remarks>
/// The cases here are the shapes that actually occur in the UI packages — a percentage from a ratio, a
/// depth in pixels, grid coordinates behind a helper method, and the status-colour ternaries that look
/// computed but are not.
/// </remarks>
public class InlineStyleComputedValueTests : RazorMarkupAnalyzerTestBase<InlineStyleAttributeAnalyzer>
{
    private const string RuleId = InlineStyleAttributeAnalyzer.DiagnosticId;

    /// <summary>
    /// A width taken from a ratio has no fixed set of values, so there is no class to move it to.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task InterpolatedValueFromData_ReportsNothing()
    {
        await VerifyRazor(
            "<div style=\"@($\"width:{Percent}%;background:var(--success);\")\"></div>");
    }

    /// <summary>
    /// Same reasoning for a computed dimension: the pixel depth comes from the tree level.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task InterpolatedDimension_ReportsNothing()
    {
        await VerifyRazor(
            "<div style=\"@($\"padding-left:{indent + 8}px;\")\"></div>");
    }

    /// <summary>
    /// A helper invocation delegates the value to code the analyzer cannot read. Reporting it would be
    /// advice with nowhere to go, and the call is itself the author saying the value is derived.
    /// </summary>
    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    [InlineData("@CellStyle(placement)")]
    [InlineData("@GridStyle()")]
    [InlineData("@Layout.CellStyle(row, column)")]
    public async Task HelperInvocation_ReportsNothing(string expression)
    {
        await VerifyRazor($"<div style=\"{expression}\"></div>");
    }

    /// <summary>
    /// Both branches name a theme token, so both are already the host's to decide — the ternary changes
    /// which token applies, not who owns its value.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task TernaryBetweenTokenLiterals_ReportsNothing()
    {
        await VerifyRazor(
            "<div style=\"@(ok ? \"color:var(--success);\" : \"color:var(--signal);\")\"></div>");
    }

    /// <summary>
    /// A ternary choosing between two fixed layouts is the component arranging itself, either way.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task TernaryBetweenLayoutLiterals_ReportsNothing()
    {
        await VerifyRazor(
            "<div style=\"@(Compact ? \"display:flex;gap:10px;\" : \"grid-template-columns:repeat(3,1fr);\")\"></div>");
    }

    /// <summary>
    /// The case the expression handling must NOT swallow. Both branches fix a theme-owned property to a
    /// literal, so both are styling the host cannot reach, and each is reported where it is written.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task TernaryBetweenHardcodedLiterals_ReportsBoth()
    {
        await VerifyRazor(
            "<div style=\"@(ok ? \"color:red;\" : \"color:blue;\")\"></div>",
            RazorDiagnostic(RuleId, 1, 21, 9).WithArguments("color:red"),
            RazorDiagnostic(RuleId, 1, 36, 10).WithArguments("color:blue"));
    }

    /// <summary>
    /// A plain literal is untouched by any of this.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task PlainLiteralValue_StillReports()
    {
        await VerifyRazor(
            "<div style=\"color:red\"></div>",
            RazorDiagnostic(RuleId, 1, 13, 9).WithArguments("color:red"));
    }

    /// <summary>
    /// The quotes inside an expression are why the value cannot be read by scanning to the next quote.
    /// A reader that stopped at the first one would see the value end mid-expression, and the literal
    /// that follows would look like a separate unquoted attribute.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task ExpressionContainingQuotes_DoesNotDesynchroniseTheScan()
    {
        // Two reported declarations (the ternary's branches) and one exempt style (the interpolation),
        // on the same line.
        await VerifyRazor(
            "<div style=\"@(ok ? \"color:red;\" : \"color:blue;\")\"><b style=\"@($\"left:{x}px\")\"></b></div>",
            RazorDiagnostic(RuleId, 1, 21, 9).WithArguments("color:red"),
            RazorDiagnostic(RuleId, 1, 36, 10).WithArguments("color:blue"));
    }

    /// <summary>
    /// An exempt style must not mask a genuine one later in the document.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task ExemptThenViolation_ReportsTheViolation()
    {
        await VerifyRazor(
            "<div style=\"@($\"width:{w}px\")\"></div>\n<div style=\"color:red\"></div>",
            RazorDiagnostic(RuleId, 2, 13, 9).WithArguments("color:red"));
    }
}
