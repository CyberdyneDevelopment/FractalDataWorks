using System;
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
    internal static void ReportMarkupOccurrences(
        AdditionalFileAnalysisContext context,
        string needle,
        DiagnosticDescriptor rule)
    {
        // Why: additional files carry every non-compiled item the project feeds the compiler, not just
        // .razor — filter on the extension rather than assuming the item order or count.
        if (!context.AdditionalFile.Path.EndsWith(RazorExtension, StringComparison.OrdinalIgnoreCase))
            return;

        var text = context.AdditionalFile.GetText(context.CancellationToken);
        if (text is null)
            return;

        var scanner = new RazorMarkupScanner(text.ToString());

        foreach (var line in text.Lines)
        {
            var lineText = text.ToString(line.Span);

            // Why: a single markup line routinely carries several matches (nested elements, an svg with a
            // styled child) — scanning only the first occurrence per line silently drops the rest.
            for (var column = lineText.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
                 column >= 0;
                 column = lineText.IndexOf(needle, column + 1, StringComparison.OrdinalIgnoreCase))
            {
                if (!scanner.IsMarkup(line.Start + column))
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
}
