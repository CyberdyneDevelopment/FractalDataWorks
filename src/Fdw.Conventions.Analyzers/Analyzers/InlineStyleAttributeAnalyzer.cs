using System.Collections.Immutable;
using Fdw.Conventions.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that reports a markup <c>style="..."</c> declaration which sets a theme-owned property to a
/// value the host theme cannot override. Applies to Fdw.UI.Pages and the domain <c>*.Components</c>
/// packages; Fdw.UI.Rendering.Blazor is exempt.
/// </summary>
/// <remarks>
/// <para>
/// The .razor document is analyzed as an additional file — raw text — so the diagnostic carries an
/// external file location. <c>#pragma warning disable</c> inside the .razor therefore cannot suppress it;
/// only NoWarn, .editorconfig, or the descriptor severity apply.
/// </para>
/// <para>
/// The rule is about themeability, and it reports only what themeability is about. A style attribute
/// carries three kinds of declaration and they are not the same defect: the component's own geometry
/// (<c>display:flex;gap:8px</c>), which a host skin does not re-decide; a theme property already routed
/// through a token (<c>color:var(--n-200)</c>), where the custom property IS the seam and the host
/// already owns the value; and a theme property written as a literal (<c>font-size:12px</c>), where an
/// inline declaration outranks every normal author rule and the host cannot restyle it at all. Only the
/// third is reported. <see cref="ThemeOwnedCssProperties"/> draws the line.
/// </para>
/// <para>
/// A style whose value the markup computes is not reported either, because a CSS class cannot carry it.
/// A class names a fixed set of declarations; a grid coordinate, a percentage width or a depth in pixels
/// has no fixed set to name. An expression selecting between written-out literals is read through those
/// literals, so each alternative is judged as the declaration list it is. <see cref="RazorAttributeValue"/>
/// and <see cref="CssStyleValue"/> draw those lines.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InlineStyleAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for an inline style attribute in Razor markup.
    /// </summary>
    public const string DiagnosticId = "FDW046";

    private const string Title = "Inline style declaration the host theme cannot override";
    private const string MessageFormat = "Inline '{0}' fixes a theme-owned property in Razor markup, where no host stylesheet can reach it; move it to a CSS class or name a theme token";
    private const string Description = "Fdw convention: the host theme decides colour, type and border treatment. An inline declaration outranks every normal author rule, so a literal written there is the one styling a host skin cannot override -- move it to a CSS class in the theme layer, or route it through a var(--token) the theme already owns. The component's own geometry (display, gap, grid, padding) is not reported: a host does not re-decide a component's arrangement, and a class used by one element only adds a hop.";
    private const string Category = "Design";

    /// <summary>
    /// The attribute opening searched for in markup, whose value is then read declaration by declaration.
    /// Single-quoted and whitespace-separated spellings (<c>style='...'</c>, <c>style ="..."</c>) are not
    /// matched.
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
        RazorMarkupAnalysis.ReportThemeOwnedStyleDeclarations(context, Needle, Rule);
}
