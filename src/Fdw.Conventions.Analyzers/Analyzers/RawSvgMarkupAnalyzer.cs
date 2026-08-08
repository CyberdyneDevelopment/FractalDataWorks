using System.Collections.Immutable;
using Fdw.Conventions.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that reports raw <c>&lt;svg&gt;</c> elements in .razor markup. Icons come from the shared icon
/// component so a glyph is defined once; pasted svg paths duplicate it per page and drift. Applies to
/// Fdw.UI.Pages and the domain <c>*.Components</c> packages; Fdw.UI.Rendering.Blazor is exempt.
/// </summary>
/// <remarks>
/// The .razor document is analyzed as an additional file — raw text — so the diagnostic carries an
/// external file location. <c>#pragma warning disable</c> inside the .razor therefore cannot suppress it;
/// only NoWarn, .editorconfig, or the descriptor severity apply.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RawSvgMarkupAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for a raw svg element in Razor markup.
    /// </summary>
    public const string DiagnosticId = "FDW047";

    private const string Title = "Raw svg element in Razor markup";
    private const string MessageFormat = "Raw <svg> element in Razor markup; render the icon through the shared icon component";
    private const string Description = "Fdw convention: icons come from the shared icon component rather than svg paths pasted into page markup, so a glyph is defined once and stays consistent across the UI.";
    private const string Category = "Design";

    /// <summary>
    /// The element opening text searched for in markup. This matches the opening tag only, so a closing
    /// <c>&lt;/svg&gt;</c> does not produce a second diagnostic for the same element. It is matched as a
    /// whole element name, so a component named <c>SvgGauge</c> or <c>SvgSparkline</c> — which is markup
    /// doing exactly what this rule asks for — is not mistaken for a raw svg element.
    /// </summary>
    private const string Needle = "<svg";

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
        RazorMarkupAnalysis.ReportMarkupOccurrences(context, Needle, Rule, wholeElementName: true);
}
