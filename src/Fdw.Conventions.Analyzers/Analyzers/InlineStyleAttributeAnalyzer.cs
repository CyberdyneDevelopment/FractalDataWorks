using System.Collections.Immutable;
using Fdw.Conventions.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that reports inline <c>style="..."</c> attributes in .razor markup. Styling belongs in a CSS
/// class in the theme layer, not on the element. Applies to Fdw.UI.Pages and the domain
/// <c>*.Components</c> packages; Fdw.UI.Rendering.Blazor is exempt.
/// </summary>
/// <remarks>
/// <para>
/// The .razor document is analyzed as an additional file — raw text — so the diagnostic carries an
/// external file location. <c>#pragma warning disable</c> inside the .razor therefore cannot suppress it;
/// only NoWarn, .editorconfig, or the descriptor severity apply.
/// </para>
/// <para>
/// A style whose value the markup computes is not reported, because a CSS class cannot carry it. A class
/// names a fixed set of declarations; a grid coordinate, a percentage width or a depth in pixels has no
/// fixed set to name, so the advice this rule gives would have no way to be followed. Selecting between
/// written-out literals stays reported — the alternatives are already fixed, which makes them two classes
/// and a conditional on the class attribute. <see cref="RazorAttributeValue"/> draws the line.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InlineStyleAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for an inline style attribute in Razor markup.
    /// </summary>
    public const string DiagnosticId = "FDW046";

    private const string Title = "Inline style attribute in Razor markup";
    private const string MessageFormat = "Inline style attribute in Razor markup; move the styling to a CSS class";
    private const string Description = "Fdw convention: Razor markup should not carry inline style attributes. Styling belongs to a CSS class in the theme layer so it stays themeable and overridable.";
    private const string Category = "Design";

    /// <summary>
    /// The attribute text searched for in markup. Single-quoted and whitespace-separated spellings
    /// (<c>style='...'</c>, <c>style ="..."</c>) are not matched.
    /// </summary>
    private const string Needle = "style=\"";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            if (!RazorMarkupAnalysis.IsAnalyzedAssembly(compilationContext.Compilation.AssemblyName))
                return;

            // Why: the per-file callback, not RegisterCompilationAction — a compilation action would force
            // the CompilationEnd custom tag on the descriptor (RS1037) and that excludes the diagnostic
            // from IDE live analysis, which is where a markup convention needs to show up.
            compilationContext.RegisterAdditionalFileAction(Analyze);
        });
    }

    private static void Analyze(AdditionalFileAnalysisContext context) =>
        RazorMarkupAnalysis.ReportMarkupOccurrences(context, Needle, Rule, skipDataDrivenValues: true);
}
