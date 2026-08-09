using System.Threading.Tasks;
using Xunit;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// Covers where FDW046 stops: a style the markup computes from data is not reported, because no CSS
/// class could carry it. A style that picks between written-out literals still is, because those
/// alternatives are already two classes and a conditional.
/// </summary>
/// <remarks>
/// The cases here are the shapes that actually occur in the UI packages — a percentage from a ratio,
/// a depth in pixels, grid coordinates behind a helper method, and the status-colour ternaries that
/// look computed but are not.
/// </remarks>
public class InlineStyleComputedValueTests : RazorMarkupAnalyzerTestBase<InlineStyleAttributeAnalyzer>
{
    private const string RuleId = InlineStyleAttributeAnalyzer.DiagnosticId;
    private const int NeedleLength = 7;

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
    /// The case the exemption must NOT swallow. Both branches are written out, so they are two classes
    /// and a conditional on the class attribute — exactly what the rule asks for.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task TernaryBetweenLiterals_StillReports()
    {
        await VerifyRazor(
            "<div style=\"@(ok ? \"color:var(--success);\" : \"color:var(--signal);\")\"></div>",
            RazorDiagnostic(RuleId, 1, 6, NeedleLength));
    }

    /// <summary>
    /// A ternary choosing between two fixed layouts is the same case, at greater length.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task TernaryBetweenLayoutLiterals_StillReports()
    {
        await VerifyRazor(
            "<div style=\"@(Compact ? \"display:flex;gap:10px;\" : \"grid-template-columns:repeat(3,1fr);\")\"></div>",
            RazorDiagnostic(RuleId, 1, 6, NeedleLength));
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
            RazorDiagnostic(RuleId, 1, 6, NeedleLength));
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
        // One reported style (the ternary) and one exempt (the interpolation), on the same line.
        await VerifyRazor(
            "<div style=\"@(ok ? \"a:b;\" : \"c:d;\")\"><b style=\"@($\"left:{x}px\")\"></b></div>",
            RazorDiagnostic(RuleId, 1, 6, NeedleLength));
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
            RazorDiagnostic(RuleId, 2, 6, NeedleLength));
    }
}
