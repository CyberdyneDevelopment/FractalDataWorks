using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// Requires every <c>[ServiceTypeCollection]</c> / <c>[PlatformServiceProvider]</c> class to declare the
/// static Configure/Register/Initialize phase methods that PlatformServicesRegistrationGenerator emits a
/// method group for.
/// </summary>
// Why this exists: PlatformServicesRegistrationGenerator documents that "every discovered class is
// guaranteed to declare the required static ... shape before this generator ever runs — the
// ServiceTypeCollectionPhaseMethodsAnalyzer (FDW024) enforces it as a build ERROR", and on that basis it
// deliberately performs NO existence check and emits `{Collection}.Configure, .Register, .Initialize`
// unconditionally. That analyzer did not exist. A collection missing a phase method therefore failed as a
// CS-level error inside generated code the author cannot open, naming a file they did not write, instead
// of pointing at the declaration that is actually wrong.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ServiceTypeCollectionPhaseMethodsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic ID for a phase method that is missing or has the wrong shape.</summary>
    public const string DiagnosticId = "FDW024";

    private const string Category = "Usage";

    private static readonly LocalizableString Title =
        "ServiceTypeCollection must declare its static phase methods";

    private static readonly LocalizableString MessageFormat =
        "'{0}' is registered into PlatformServices but does not declare 'public static {1}' — the registration generator emits a method group for it unconditionally";

    private static readonly LocalizableString Description =
        "PlatformServicesRegistrationGenerator collects each discovered domain's Configure/Register/Initialize method groups into a ServiceTypeCollectionDescriptor without checking that they exist. A missing or mis-shaped phase method therefore surfaces as a compiler error inside generated code rather than at the declaration. Declare all three with the exact signatures the descriptor requires.";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    // Why: the three shapes are the ServiceTypeCollectionDescriptor constructor parameters verbatim
    // (ServiceTypeCollectionDescriptor.cs) — the method group must be convertible to that delegate, so the
    // parameter and return types are checked, not just the name.
    private const string HostApplicationBuilder = "Microsoft.Extensions.Hosting.IHostApplicationBuilder";
    private const string LoggerFactory = "Microsoft.Extensions.Logging.ILoggerFactory";
    private const string Host = "Microsoft.Extensions.Hosting.IHost";

    // Why the return types are spelled separately from the parameter types: a phase takes a builder or
    // host and hands back a RESULT carrying it, so the two are no longer the same symbol and checking
    // "returns what it took" would now reject every correct phase.
    private const string BuilderResult = "Fdw.Results.IGenericResult<Microsoft.Extensions.Hosting.IHostApplicationBuilder>";
    private const string HostResult = "Fdw.Results.IGenericResult<Microsoft.Extensions.Hosting.IHost>";

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null) return;

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (!IsRegisteredIntoPlatformServices(type))
            return;

        // Why: partial classes and the generated half both contribute members, and the generator emits the
        // phase methods for the common case. Only report what is genuinely absent from the merged symbol.
        RequirePhase(context, type, "Configure", HostApplicationBuilder, BuilderResult);
        RequirePhase(context, type, "Register", HostApplicationBuilder, BuilderResult);
        RequirePhase(context, type, "Initialize", Host, HostResult);
    }

    private static bool IsRegisteredIntoPlatformServices(INamedTypeSymbol type)
    {
        var attributes = type.GetAttributes();
        for (var i = 0; i < attributes.Length; i++)
        {
            var name = attributes[i].AttributeClass?.Name;
            if (string.Equals(name, "ServiceTypeCollectionAttribute", System.StringComparison.Ordinal)
                || string.Equals(name, "PlatformServiceProviderAttribute", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void RequirePhase(
        SymbolAnalysisContext context,
        INamedTypeSymbol type,
        string phaseName,
        string firstParameterType,
        string returnType)
    {
        // Why walk the base chain: the phase methods are INHERITED statics on ServiceTypeCollectionBase,
        // not redeclared per collection — a C# static is reachable through the derived type name, so
        // `HealthMonitorTypes.Configure` binds to the base and the generator's method group is valid.
        // GetMembers sees only declared members, so checking the type alone reports every correct
        // collection in the solution. The generator additionally emits an override on some collections
        // (Register, when GenerateProvider = true), which is why a declared-only check failed
        // inconsistently rather than uniformly.
        for (var current = type; current is not null; current = current.BaseType)
        {
            var matched = current.GetMembers(phaseName)
                .OfType<IMethodSymbol>()
                .Any(m => IsPhaseMethod(m, firstParameterType, returnType));

            if (matched)
                return;
        }

        // Why: report on the declaration itself. Locations[0] is the class identifier, which is where the
        // author has to add the method — not the generated file that would otherwise fail to compile.
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            type.Locations.Length > 0 ? type.Locations[0] : Location.None,
            type.Name,
            $"{Short(returnType)} {phaseName}({Short(firstParameterType)}, ILoggerFactory?)"));
    }

    private static bool IsPhaseMethod(IMethodSymbol method, string firstParameterType, string returnType)
    {
        if (!method.IsStatic
            || method.DeclaredAccessibility != Accessibility.Public
            || method.Parameters.Length != 2)
        {
            return false;
        }

        return Is(method.Parameters[0].Type, firstParameterType)
            && Is(method.Parameters[1].Type, LoggerFactory)
            && Is(method.ReturnType, returnType);
    }

    private static bool Is(ITypeSymbol symbol, string fullyQualifiedName)
        => string.Equals(
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", string.Empty)
                .TrimEnd('?'),
            fullyQualifiedName,
            System.StringComparison.Ordinal);

    // Why this shortens each part rather than taking the tail after the last dot: the return types are
    // now generic, so "…IGenericResult<…IHostApplicationBuilder>" has its last dot INSIDE the type
    // argument. Taking the tail produced "IHostApplicationBuilder>" — a diagnostic telling the author
    // to declare a method whose return type does not parse.
    private static string Short(string fullyQualifiedName)
    {
        var open = fullyQualifiedName.IndexOf('<');
        if (open < 0)
            return Tail(fullyQualifiedName);

        var close = fullyQualifiedName.LastIndexOf('>');
        return close > open
            ? $"{Tail(fullyQualifiedName.Substring(0, open))}<{Tail(fullyQualifiedName.Substring(open + 1, close - open - 1))}>"
            : Tail(fullyQualifiedName);
    }

    private static string Tail(string name)
    {
        var lastDot = name.LastIndexOf('.');
        return lastDot >= 0 ? name.Substring(lastDot + 1) : name;
    }
}
