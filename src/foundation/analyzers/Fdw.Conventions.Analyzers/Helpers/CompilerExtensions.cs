using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fdw.Conventions.Analyzers.Helpers;

/// <summary>
/// Says whether a compilation is a compiler extension: it references Roslyn and declares a generator, an analyzer or a code fix. FDW055 and FDW073 guard
/// what a running system reads, and a compiler extension runs inside the compiler, so they leave it out (FDW-3580, FDW-7059).
/// </summary>
internal static class CompilerExtensions
{
    private static readonly ImmutableArray<string> ExtensionAttributes =
        ["Microsoft.CodeAnalysis.GeneratorAttribute", "Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerAttribute", "Microsoft.CodeAnalysis.CodeFixes.ExportCodeFixProviderAttribute"];

    /// <summary>Whether the compilation is a compiler extension.</summary>
    /// <param name="compilation">The compilation.</param>
    /// <returns>True when it references Roslyn and declares a generator, analyzer or code fix.</returns>
    public static bool IsCompilerExtension(Compilation compilation) =>
        compilation.ReferencedAssemblyNames.Any(reference => string.Equals(reference.Name, "Microsoft.CodeAnalysis", StringComparison.Ordinal)) &&
        DeclaresExtension(compilation.Assembly.GlobalNamespace);

    private static bool DeclaresExtension(INamespaceSymbol ns) =>
        ns.GetTypeMembers().Any(type => type.GetAttributes().Any(a =>
            a.AttributeClass is { } attribute && ExtensionAttributes.Contains(attribute.ToDisplayString(), StringComparer.Ordinal))) ||
        ns.GetNamespaceMembers().Any(DeclaresExtension);
}
