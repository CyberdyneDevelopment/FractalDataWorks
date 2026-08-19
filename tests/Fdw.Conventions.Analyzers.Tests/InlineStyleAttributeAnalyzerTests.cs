using System.Threading.Tasks;
using Xunit;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="InlineStyleAttributeAnalyzer"/> (FDW046) — the scan itself: which documents and
/// assemblies it reads, and which regions of a .razor file count as markup. Where the rule draws its
/// line between a declaration that must move and one that may stay is covered by
/// <see cref="InlineStyleThemeOwnershipTests"/>.
/// </summary>
/// <remarks>
/// <c>color:red</c> is the canonical violation used throughout: a theme-owned property fixed to a
/// literal, which is what the rule reports.
/// </remarks>
public class InlineStyleAttributeAnalyzerTests : RazorMarkupAnalyzerTestBase<InlineStyleAttributeAnalyzer>
{
    private const string RuleId = InlineStyleAttributeAnalyzer.DiagnosticId;
    private const string Declaration = "color:red";
    private const int DeclarationLength = 9;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task InlineStyleOnElement_ReportsDiagnostic()
    {
        await VerifyRazor(
            "<div style=\"color:red\">Hello</div>",
            RazorDiagnostic(RuleId, 1, 13, DeclarationLength).WithArguments(Declaration));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task CssClassOnly_ReportsNothing()
    {
        await VerifyRazor("<div class=\"card\">Hello</div>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task TwoInlineStylesOnOneLine_ReportsBoth()
    {
        await VerifyRazor(
            "<div style=\"color:red\"><span style=\"color:blue\"></span></div>",
            RazorDiagnostic(RuleId, 1, 13, DeclarationLength).WithArguments(Declaration),
            RazorDiagnostic(RuleId, 1, 37, 10).WithArguments("color:blue"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task InlineStyleOnLaterLine_ReportsAtThatLine()
    {
        await VerifyRazor(
            """
            @page "/demo"

            <section>
                <p style="color:red">Body</p>
            </section>
            """,
            RazorDiagnostic(RuleId, 4, 15, DeclarationLength).WithArguments(Declaration));
    }

    /// <summary>
    /// A bare expression value states no declaration the reader can see, so there is nothing to judge.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task BareExpressionValue_ReportsNothing()
    {
        await VerifyRazor("<div style=\"@ComputedStyle\">Hello</div>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task StyleInsideCSharpStringLiteral_ReportsNothing()
    {
        await VerifyRazor(
            """
            <div class="card"></div>

            @code {
                private const string Markup = "<div style=\"color:red\"></div>";
            }
            """);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task StyleInsideRenderFragmentLambda_ReportsDiagnostic()
    {
        // Why: markup written inside a RenderFragment lambda declared in @code is still markup, which is
        // why the scanner excludes only literals and comments in a code block rather than the whole block.
        await VerifyRazor(
            """
            @code {
                private RenderFragment Row => __builder =>
                {
                    <div style="color:red"></div>
                };
            }
            """,
            RazorDiagnostic(RuleId, 4, 21, DeclarationLength).WithArguments(Declaration));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task StyleInsideRazorComment_ReportsNothing()
    {
        await VerifyRazor("@* <div style=\"color:red\"></div> *@");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task StyleInsideCSharpLineComment_ReportsNothing()
    {
        await VerifyRazor(
            """
            @code {
                // <div style="color:red"></div>
                private int Count;
            }
            """);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task UppercaseStyleAttribute_ReportsDiagnostic()
    {
        await VerifyRazor(
            "<div STYLE=\"color:red\"></div>",
            RazorDiagnostic(RuleId, 1, 13, DeclarationLength).WithArguments(Declaration));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Analyzer")]
    public async Task SingleQuotedStyleAttribute_ReportsNothing()
    {
        // Documented limitation: only the double-quoted spelling is matched. No such markup exists in the
        // packages this rule governs.
        await VerifyRazor("<div style='color:red'></div>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task ComponentsAssembly_ReportsDiagnostic()
    {
        await VerifyRazorIn(
            InScopeComponentsAssembly,
            "<div style=\"color:red\"></div>",
            RazorDiagnostic(RuleId, 1, 13, DeclarationLength).WithArguments(Declaration));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task RenderingAssembly_ReportsNothing()
    {
        await VerifyRazorIn(RenderingAssembly, "<div style=\"color:red\"></div>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task UnrelatedAssembly_ReportsNothing()
    {
        await VerifyRazorIn("Fdw.Services.Connections", "<div style=\"color:red\"></div>");
    }
}
