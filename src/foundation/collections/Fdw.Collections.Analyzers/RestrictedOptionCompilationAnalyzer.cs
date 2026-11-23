using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Collections.Analyzers;

/// <summary>
/// Reports a <c>[TypeOption]</c> with <c>RestrictToCurrentCompilation = true</c> whose collection is declared in another compilation
/// that does not reference the option's assembly.
/// </summary>
/// <remarks>
/// Why this is a defect: a restricted option is registered only by the static constructor of a collection that can see it, and only
/// a collection in the same compilation can. The module initializer an application's compilation generates skips a restricted
/// option as well. An option declared in a different assembly than its collection is therefore registered nowhere, and a lookup by
/// name answers the NotFound sentinel: discovery stored every SQL Server table with containerType NotFound that way (FDW-5136,
/// FDW-6309).
/// <para>
/// A collection's own generated constructor registers every option its compilation can see, in its own assembly and in every assembly
/// its compilation references, whatever the option's own flag says (TypeOptionDiscovery.DiscoverAll). So an option whose assembly is in
/// the reference closure of its collection's assembly is registered and is not reported. Only the module initializer of an executable
/// skips a restricted option.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RestrictedOptionCompilationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for a restricted option whose collection is in another compilation.
    /// </summary>
    public const string DiagnosticId = "TC014";

    private const string TypeOptionAttributeName = "Fdw.Collections.Attributes.TypeOptionAttribute";
    private const string RestrictPropertyName = "RestrictToCurrentCompilation";

    /// <summary>
    /// The rule this analyzer reports.
    /// </summary>
    public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Restricted type option whose collection is in another compilation",
        "Option '{0}' sets RestrictToCurrentCompilation = true but its collection '{1}' is declared in assembly '{2}', so no collection registers it and a lookup answers NotFound",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A type option restricted to its own compilation is registered only by a collection in that compilation. When the collection is declared in another assembly the option is registered nowhere. Remove RestrictToCurrentCompilation = true, or move the option into the collection's assembly.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        foreach (var attribute in type.GetAttributes())
        {
            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), TypeOptionAttributeName, System.StringComparison.Ordinal))
                continue;

            if (!IsRestricted(attribute))
                continue;

            if (attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol collection)
                continue;

            if (collection.ContainingAssembly is null
                || SymbolEqualityComparer.Default.Equals(collection.ContainingAssembly, context.Compilation.Assembly))
                continue;

            if (ReferenceClosureIncludes(collection.ContainingAssembly, context.Compilation))
                continue;

            var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? type.Locations[0];
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, type.Name, collection.Name, collection.ContainingAssembly.Name));
        }
    }

    private static bool ReferenceClosureIncludes(IAssemblySymbol collectionAssembly, Compilation compilation)
    {
        var loaded = new Dictionary<string, IAssemblySymbol>(StringComparer.Ordinal);
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
            loaded[assembly.Name] = assembly;

        return Reaches(
            collectionAssembly.Name,
            compilation.Assembly.Name,
            name => loaded.TryGetValue(name, out var assembly)
                ? assembly.Modules.SelectMany(module => module.ReferencedAssemblies).Select(identity => identity.Name)
                : Enumerable.Empty<string>());
    }

    /// <summary>
    /// Tells whether <paramref name="target"/> is reachable from <paramref name="start"/> by following the references each assembly names.
    /// </summary>
    /// <param name="start">The assembly the walk begins at, by name.</param>
    /// <param name="target">The assembly looked for, by name.</param>
    /// <param name="referencesOf">The names of the assemblies an assembly references directly; none for one the walk cannot open.</param>
    /// <returns>True when <paramref name="target"/> is among the assemblies <paramref name="start"/> references, directly or through others.</returns>
    public static bool Reaches(string start, string target, Func<string, IEnumerable<string>> referencesOf)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal) { start };
        var pending = new Stack<string>();
        pending.Push(start);
        while (pending.Count > 0)
        {
            foreach (var next in referencesOf(pending.Pop()))
            {
                if (string.Equals(next, target, StringComparison.Ordinal))
                    return true;

                if (seen.Add(next))
                    pending.Push(next);
            }
        }

        return false;
    }

    private static bool IsRestricted(AttributeData attribute)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (string.Equals(argument.Key, RestrictPropertyName, System.StringComparison.Ordinal) && argument.Value.Value is true)
                return true;
        }

        return false;
    }
}
