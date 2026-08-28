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

    private const string HostApplicationBuilder = "Microsoft.Extensions.Hosting.IHostApplicationBuilder";
    private const string LoggerFactory = "Microsoft.Extensions.Logging.ILoggerFactory";
    private const string Host = "Microsoft.Extensions.Hosting.IHost";

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
        for (var current = type; current is not null; current = current.BaseType)
        {
            var matched = current.GetMembers(phaseName)
                .OfType<IMethodSymbol>()
                .Any(m => IsPhaseMethod(m, firstParameterType, returnType));

            if (matched)
                return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            type.Locations.Length > 0 ? type.Locations[0] : Location.None,
            type.Name,
            $"{Short(returnType)} {phaseName}({Short(firstParameterType)}, ILoggerFactory?, bool force = false, bool defer = false)"));
    }

    private static bool IsPhaseMethod(IMethodSymbol method, string firstParameterType, string returnType)
    {
        if (!method.IsStatic
            || method.DeclaredAccessibility != Accessibility.Public
            || method.Parameters.Length is not (2 or 3 or 4))
        {
            return false;
        }

        return Is(method.Parameters[0].Type, firstParameterType)
            && Is(method.Parameters[1].Type, LoggerFactory)
            && method.Parameters.Skip(2).All(p => p.Type.SpecialType == SpecialType.System_Boolean)
            && Is(method.ReturnType, returnType);
    }

    private static bool Is(ITypeSymbol symbol, string fullyQualifiedName)
        => string.Equals(
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", string.Empty)
                .TrimEnd('?'),
            fullyQualifiedName,
            System.StringComparison.Ordinal);

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
