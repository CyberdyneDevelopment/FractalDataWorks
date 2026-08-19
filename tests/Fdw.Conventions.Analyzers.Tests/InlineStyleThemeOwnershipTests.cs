using System.Threading.Tasks;
using Xunit;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// Covers the line FDW046 draws inside a style attribute: a theme-owned property fixed to a literal is
/// reported, the component's own geometry is not, and a theme property routed through a token is not.
/// </summary>
/// <remarks>
/// The distinction is what the rule is for. A host skin restyles colour, type and border treatment, and
/// an inline declaration outranks every normal author rule — so a literal written there is styling the
/// host cannot reach. It does not restyle whether a row is a flexbox, and a <c>var(--token)</c> value is
/// already the host's to decide. Reporting those two would be asking for a class per element that buys
/// no override the markup did not already have.
/// </remarks>
public class InlineStyleThemeOwnershipTests : RazorMarkupAnalyzerTestBase<InlineStyleAttributeAnalyzer>
{
    private const string RuleId = InlineStyleAttributeAnalyzer.DiagnosticId;

    /// <summary>
    /// Geometry is the component's own arrangement; a host does not re-decide it.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task GeometryOnly_ReportsNothing()
    {
        await VerifyRazor("<div style=\"display:flex;align-items:center;gap:8px;\"></div>");
    }

    /// <summary>
    /// Typographic and pointer affordances travel with the arrangement, not with the skin.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task AlignmentAndCursor_ReportNothing()
    {
        await VerifyRazor("<div style=\"text-align:center;cursor:pointer;\"></div>");
    }

    /// <summary>
    /// The custom property IS the theming seam — the host defines what the token resolves to.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task ThemeTokenValue_ReportsNothing()
    {
        await VerifyRazor("<div style=\"color:var(--n-200);background:var(--s2);\"></div>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task GeometryWithThemeToken_ReportsNothing()
    {
        await VerifyRazor("<div style=\"display:flex;gap:8px;color:var(--n-200);\"></div>");
    }

    /// <summary>
    /// A cascade keyword hands the decision back rather than asserting one, so nothing is blocked.
    /// </summary>
    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    [InlineData("color:inherit")]
    [InlineData("font-size:inherit")]
    [InlineData("border-radius:unset")]
    public async Task CascadeKeywordValue_ReportsNothing(string declaration)
    {
        await VerifyRazor($"<div style=\"{declaration};\"></div>");
    }

    /// <summary>
    /// The mixed attribute is the common shape, and only the part the host cannot reach is reported.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task GeometryWithHardcodedTheme_ReportsOnlyTheThemeDeclaration()
    {
        await VerifyRazor(
            "<div style=\"display:flex;gap:8px;font-size:12px;\"></div>",
            RazorDiagnostic(RuleId, 1, 34, 14).WithArguments("font-size:12px"));
    }

    /// <summary>
    /// Each fixed declaration is its own move, so each is reported where it stands.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task SeveralHardcodedThemeDeclarations_ReportEach()
    {
        await VerifyRazor(
            "<div style=\"font-size:10px;letter-spacing:0.1em;text-transform:uppercase;color:var(--n-500);\"></div>",
            RazorDiagnostic(RuleId, 1, 13, 14).WithArguments("font-size:10px"),
            RazorDiagnostic(RuleId, 1, 28, 20).WithArguments("letter-spacing:0.1em"),
            RazorDiagnostic(RuleId, 1, 49, 24).WithArguments("text-transform:uppercase"));
    }

    /// <summary>
    /// A hex colour is the plainest case of a value no stylesheet can reach.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task HexColour_ReportsDiagnostic()
    {
        await VerifyRazor(
            "<div style=\"background:#08090b;\"></div>",
            RazorDiagnostic(RuleId, 1, 13, 18).WithArguments("background:#08090b"));
    }

    /// <summary>
    /// Spacing around the colon is the author's; the span covers what is written and the message
    /// names the declaration.
    /// </summary>
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Analyzer")]
    public async Task SpacedDeclarations_ReportEachAtItsOwnSpan()
    {
        await VerifyRazor(
            "<div style=\"font-size: 12px; color:red\"></div>",
            RazorDiagnostic(RuleId, 1, 13, 15).WithArguments("font-size:12px"),
            RazorDiagnostic(RuleId, 1, 30, 9).WithArguments("color:red"));
    }

    /// <summary>
    /// A declaration whose value the markup computes cannot be judged, and the rest of the attribute
    /// still can.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task DeclarationCarryingATransition_IsSkippedAndTheRestIsRead()
    {
        await VerifyRazor(
            "<div style=\"width:@(Percent)%;color:red\"></div>",
            RazorDiagnostic(RuleId, 1, 31, 9).WithArguments("color:red"));
    }
}
