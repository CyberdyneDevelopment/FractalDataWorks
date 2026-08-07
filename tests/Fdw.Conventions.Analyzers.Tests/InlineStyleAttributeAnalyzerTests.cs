using System.Threading.Tasks;
using Xunit;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="InlineStyleAttributeAnalyzer"/> (FDW046).
/// </summary>
public class InlineStyleAttributeAnalyzerTests : RazorMarkupAnalyzerTestBase<InlineStyleAttributeAnalyzer>
{
    private const string RuleId = InlineStyleAttributeAnalyzer.DiagnosticId;
    private const int NeedleLength = 7;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task InlineStyleOnElement_ReportsDiagnostic()
    {
        await VerifyRazor(
            "<div style=\"color:red\">Hello</div>",
            RazorDiagnostic(RuleId, 1, 6, NeedleLength));
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
            "<div style=\"a\"><span style=\"b\"></span></div>",
            RazorDiagnostic(RuleId, 1, 6, NeedleLength),
            RazorDiagnostic(RuleId, 1, 22, NeedleLength));
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
                <p style="margin:0">Body</p>
            </section>
            """,
            RazorDiagnostic(RuleId, 4, 8, NeedleLength));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task DynamicStyleValue_ReportsDiagnostic()
    {
        await VerifyRazor(
            "<div style=\"@ComputedStyle\">Hello</div>",
            RazorDiagnostic(RuleId, 1, 6, NeedleLength));
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
            RazorDiagnostic(RuleId, 4, 14, NeedleLength));
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
            RazorDiagnostic(RuleId, 1, 6, NeedleLength));
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
            RazorDiagnostic(RuleId, 1, 6, NeedleLength));
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
