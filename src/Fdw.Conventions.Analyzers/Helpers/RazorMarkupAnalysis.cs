using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Conventions.Analyzers.Helpers;

/// <summary>
/// Shared plumbing for the .razor markup convention analyzers: which assemblies they apply to, and the
/// text scan that reports every markup occurrence of a literal needle in an additional file.
/// </summary>
/// <remarks>
/// .razor files reach an analyzer as additional files (the Razor SDK adds them to
/// <c>AdditionalFiles</c>), never as syntax trees. A project that holds .razor files without the Razor
/// SDK would need an explicit <c>&lt;AdditionalFiles Include="**/*.razor" /&gt;</c> for these rules to see them.
/// </remarks>
internal static class RazorMarkupAnalysis
{
    private const string RazorExtension = ".razor";

    /// <summary>
    /// The page package whose .razor markup these conventions govern.
    /// </summary>
    private const string PagesAssembly = "Fdw.UI.Pages";

    /// <summary>
    /// The suffix identifying a domain UI component package, whose .razor markup these conventions govern.
    /// </summary>
    private const string ComponentsAssemblySuffix = ".Components";

    /// <summary>
    /// The render package, which composes markup as its purpose and is therefore exempt.
    /// </summary>
    private const string RenderingAssembly = "Fdw.UI.Rendering.Blazor";

    private const string TestAssemblySuffix = ".Tests";

    /// <summary>
    /// Determines whether the .razor markup conventions apply to the named assembly.
    /// </summary>
    /// <param name="assemblyName">The compilation's assembly name.</param>
    /// <returns><see langword="true"/> when the assembly is in scope; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Scope is the page package plus the domain <c>*.Components</c> packages. Fdw.UI.Rendering.Blazor is
    /// excluded because emitting markup — inline style attributes and svg included — is what that package
    /// does. An assembly with no name cannot be placed in or out of scope, so the rules stay silent.
    /// </remarks>
    internal static bool IsAnalyzedAssembly(string? assemblyName)
    {
        if (assemblyName is null)
            return false;

        if (string.Equals(assemblyName, RenderingAssembly, StringComparison.OrdinalIgnoreCase))
            return false;

        if (assemblyName.EndsWith(TestAssemblySuffix, StringComparison.OrdinalIgnoreCase))
            return false;

        return string.Equals(assemblyName, PagesAssembly, StringComparison.OrdinalIgnoreCase) ||
               assemblyName.EndsWith(ComponentsAssemblySuffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reports <paramref name="rule"/> at every occurrence of <paramref name="needle"/> that falls in the
    /// markup of the context's additional file, when that file is a .razor document.
    /// </summary>
    /// <param name="context">The additional file analysis context.</param>
    /// <param name="needle">The literal text to search for, matched case-insensitively.</param>
    /// <param name="rule">The descriptor to report.</param>
    /// <param name="wholeElementName">
    /// When <see langword="true"/> the needle opens an element and only matches where the tag name ends
    /// there, so <c>&lt;svg</c> does not also match the component <c>&lt;SvgGauge&gt;</c>.
    /// </param>
    /// <param name="skipDrawnElements">
    /// When <see langword="true"/> an svg element the component draws — one handling pointer input or
    /// generating its own contents — is not reported. Only the icon rule passes this.
    /// </param>
    internal static void ReportMarkupOccurrences(
        AdditionalFileAnalysisContext context,
        string needle,
        DiagnosticDescriptor rule,
        bool wholeElementName = false,
        bool skipDrawnElements = false)
    {
        if (!context.AdditionalFile.Path.EndsWith(RazorExtension, StringComparison.OrdinalIgnoreCase))
            return;

        var text = context.AdditionalFile.GetText(context.CancellationToken);
        if (text is null)
            return;

        var document = text.ToString();
        var scanner = new RazorMarkupScanner(document);

        foreach (var line in text.Lines)
        {
            var lineText = text.ToString(line.Span);

            for (var column = lineText.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
                 column >= 0;
                 column = lineText.IndexOf(needle, column + 1, StringComparison.OrdinalIgnoreCase))
            {
                if (!scanner.IsMarkup(line.Start + column))
                    continue;

                if (wholeElementName && ContinuesTagName(lineText, column + needle.Length))
                    continue;

                if (skipDrawnElements && RazorSvgElement.IsDrawn(document, line.Start + column))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    rule,
                    Location.Create(
                        context.AdditionalFile.Path,
                        new TextSpan(line.Start + column, needle.Length),
                        new LinePositionSpan(
                            new LinePosition(line.LineNumber, column),
                            new LinePosition(line.LineNumber, column + needle.Length)))));
            }
        }
    }

    /// <summary>
    /// Reports <paramref name="rule"/> at every declaration of a markup style attribute that sets a
    /// theme-owned property to a value the host cannot override.
    /// </summary>
    /// <param name="context">The additional file analysis context.</param>
    /// <param name="needle">The attribute opening text to search for, matched case-insensitively.</param>
    /// <param name="rule">The descriptor to report, whose message takes the declaration as its argument.</param>
    /// <remarks>
    /// The diagnostic lands on the declaration, not on the attribute, because the declaration is the unit
    /// that gets fixed: a style attribute routinely mixes the component's own geometry — which stays —
    /// with one hardcoded colour or size that has to move.
    /// </remarks>
    internal static void ReportThemeOwnedStyleDeclarations(
        AdditionalFileAnalysisContext context,
        string needle,
        DiagnosticDescriptor rule)
    {
        if (!context.AdditionalFile.Path.EndsWith(RazorExtension, StringComparison.OrdinalIgnoreCase))
            return;

        var text = context.AdditionalFile.GetText(context.CancellationToken);
        if (text is null)
            return;

        var document = text.ToString();
        var scanner = new RazorMarkupScanner(document);
        var declarations = new List<CssDeclarationSpan>();

        foreach (var line in text.Lines)
        {
            var lineText = text.ToString(line.Span);

            for (var column = lineText.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
                 column >= 0;
                 column = lineText.IndexOf(needle, column + 1, StringComparison.OrdinalIgnoreCase))
            {
                if (!scanner.IsMarkup(line.Start + column))
                    continue;

                var valueStart = line.Start + column + needle.Length;

                if (RazorAttributeValue.IsDataDriven(document, valueStart))
                    continue;

                declarations.Clear();
                CssStyleValue.Collect(document, valueStart, declarations);

                foreach (var declaration in declarations)
                {
                    if (!ThemeOwnedCssProperties.IsThemeOwned(declaration.Property))
                        continue;

                    if (ThemeOwnedCssProperties.IsHostOverridable(declaration.Value))
                        continue;

                    context.ReportDiagnostic(Diagnostic.Create(
                        rule,
                        Location.Create(
                            context.AdditionalFile.Path,
                            new TextSpan(declaration.Start, declaration.Length),
                            new LinePositionSpan(
                                text.Lines.GetLinePosition(declaration.Start),
                                text.Lines.GetLinePosition(declaration.Start + declaration.Length))),
                        declaration.Text));
                }
            }
        }
    }

    /// <summary>
    /// Determines whether the tag name that began before <paramref name="position"/> carries on past it.
    /// </summary>
    /// <param name="lineText">The markup line being scanned.</param>
    /// <param name="position">The offset just past the matched needle.</param>
    /// <returns><see langword="true"/> when the element name continues; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// A tag name runs to whitespace, <c>&gt;</c>, or the <c>/</c> of a self-closing tag; a name character
    /// there means the match landed inside a longer name — a component whose name merely starts with the
    /// needle — and not on the element the rule is about.
    /// </remarks>
    private static bool ContinuesTagName(string lineText, int position)
    {
        if (position >= lineText.Length)
            return false;

        var next = lineText[position];
        return char.IsLetterOrDigit(next) || next == '-' || next == '_' || next == '.' || next == ':';
    }
}
