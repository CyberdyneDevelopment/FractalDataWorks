using System.Threading.Tasks;
using Xunit;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="RawSvgMarkupAnalyzer"/> (FDW047).
/// </summary>
public class RawSvgMarkupAnalyzerTests : RazorMarkupAnalyzerTestBase<RawSvgMarkupAnalyzer>
{
    private const string RuleId = RawSvgMarkupAnalyzer.DiagnosticId;
    private const int NeedleLength = 4;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task RawSvgElement_ReportsDiagnostic()
    {
        await VerifyRazor(
            "<svg class=\"icon\"><path d=\"M0 0\" /></svg>",
            RazorDiagnostic(RuleId, 1, 1, NeedleLength));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task IconComponent_ReportsNothing()
    {
        await VerifyRazor("<Icon Name=\"chevron-down\" />");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task ClosingTag_DoesNotReportSecondDiagnostic()
    {
        await VerifyRazor(
            "<svg></svg>",
            RazorDiagnostic(RuleId, 1, 1, NeedleLength));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task TwoSvgElementsOnOneLine_ReportsBoth()
    {
        await VerifyRazor(
            "<svg></svg><svg></svg>",
            RazorDiagnostic(RuleId, 1, 1, NeedleLength),
            RazorDiagnostic(RuleId, 1, 12, NeedleLength));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task SvgOnLaterLine_ReportsAtThatLine()
    {
        await VerifyRazor(
            """
            @page "/demo"

            <button>
                <svg viewBox="0 0 24 24"></svg>
            </button>
            """,
            RazorDiagnostic(RuleId, 4, 5, NeedleLength));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task SvgInsideCSharpStringLiteral_ReportsNothing()
    {
        await VerifyRazor(
            """
            <Icon Name="chevron-down" />

            @code {
                private const string Glyph = "<svg viewBox=\"0 0 24 24\"></svg>";
            }
            """);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task SvgInsideRenderFragmentLambda_ReportsDiagnostic()
    {
        // Why: markup written inside a RenderFragment lambda declared in @code is still markup, which is
        // why the scanner excludes only literals and comments in a code block rather than the whole block.
        await VerifyRazor(
            """
            @code {
                private RenderFragment Glyph => __builder =>
                {
                    <svg class="icon"></svg>
                };
            }
            """,
            RazorDiagnostic(RuleId, 4, 9, NeedleLength));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task SvgInsideRazorComment_ReportsNothing()
    {
        await VerifyRazor("@* <svg></svg> *@");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task SvgInsideCSharpLineComment_ReportsNothing()
    {
        await VerifyRazor(
            """
            @code {
                // <svg></svg>
                private int Count;
            }
            """);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task UppercaseSvgElement_ReportsDiagnostic()
    {
        await VerifyRazor(
            "<SVG></SVG>",
            RazorDiagnostic(RuleId, 1, 1, NeedleLength));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task ComponentWhoseNameStartsWithSvg_ReportsNothing()
    {
        // Why: a chart component named SvgGauge is markup already doing what this rule asks for; matching
        // the needle inside a longer tag name would flag the fix as the defect.
        await VerifyRazor("<SvgGauge Value=\"1\" /><SvgSparkline Width=\"80\" />");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task ComponentsAssembly_ReportsDiagnostic()
    {
        await VerifyRazorIn(
            InScopeComponentsAssembly,
            "<svg></svg>",
            RazorDiagnostic(RuleId, 1, 1, NeedleLength));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task RenderingAssembly_ReportsNothing()
    {
        await VerifyRazorIn(RenderingAssembly, "<svg></svg>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task UnrelatedAssembly_ReportsNothing()
    {
        await VerifyRazorIn("Fdw.Services.Connections", "<svg></svg>");
    }
}
