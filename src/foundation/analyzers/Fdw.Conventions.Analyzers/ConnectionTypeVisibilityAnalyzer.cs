using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// FDW053: the connection type is invisible above the connection layer. Reports <c>is</c>, <c>as</c>, a cast,
/// <c>typeof</c> or a type pattern that names a connection implementation type (a concrete class implementing
/// IGenericConnection, IConnectionType or IConnectionImplementationConfiguration) declared in another assembly.
/// </summary>
/// <remarks>
/// Why another assembly: a connection project naming its own types is the connection layer itself. So is any
/// assembly that declares a connection implementation type of its own, such as a connection's Registration project
/// (it declares the connection type and names the configuration from its sibling project), and any assembly that
/// declares <c>[assembly: Fdw.Conventions.DataAccessAdapter]</c>; those may name another connection project's types
/// (the MsSql family spans several assemblies). Holding, constructing or passing such a
/// type is not reported; deciding behaviour by which connection type it is, is (FDW-3579).
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConnectionTypeVisibilityAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for a connection implementation type named above the connection layer.
    /// </summary>
    public const string DiagnosticId = "FDW053";

    private const string AdapterAttributeName = "Fdw.Conventions.DataAccessAdapterAttribute";

    private static readonly string[] ConnectionInterfaces =
    [
        "Fdw.Services.Connections.Abstractions.IGenericConnection",
        "Fdw.Services.Connections.Abstractions.IConnectionType",
        "Fdw.Services.Connections.Abstractions.IConnectionImplementationConfiguration",
    ];

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Connection type named above the connection layer",
        "'{0}' is a connection implementation type: above the connection layer, work through the connection abstractions instead of testing or naming the concrete type",
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The connection type must be invisible above the connection layer: no is/as/typeof/cast or type pattern on a concrete connection, connection type or connection implementation configuration declared in another assembly, outside an assembly that declares [assembly: Fdw.Conventions.DataAccessAdapter].");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            if (start.Compilation.Assembly.GetAttributes()
                .Any(a => string.Equals(a.AttributeClass?.ToDisplayString(), AdapterAttributeName, StringComparison.Ordinal)))
                return;

            var interfaces = ConnectionInterfaces
                .Select(name => start.Compilation.GetTypeByMetadataName(name))
                .OfType<INamedTypeSymbol>()
                .ToImmutableArray();

            if (interfaces.IsEmpty)
                return;

            var assembly = start.Compilation.Assembly;
            if (DeclaresConnectionImplementation(assembly.GlobalNamespace, interfaces))
                return;

            start.RegisterOperationAction(
                operationContext => Analyze(operationContext, interfaces, assembly),
                OperationKind.IsType,
                OperationKind.Conversion,
                OperationKind.TypeOf,
                OperationKind.DeclarationPattern,
                OperationKind.TypePattern);
        });
    }

    private static void Analyze(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces, IAssemblySymbol assembly)
    {
        var named = context.Operation switch
        {
            IIsTypeOperation isType => isType.TypeOperand,
            IConversionOperation conversion when conversion.IsTryCast || !conversion.IsImplicit => conversion.Type,
            ITypeOfOperation typeOf => typeOf.TypeOperand,
            IDeclarationPatternOperation declaration => declaration.MatchedType,
            ITypePatternOperation typePattern => typePattern.MatchedType,
            _ => null,
        };

        if (named is not INamedTypeSymbol type ||
            SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, assembly) ||
            !IsConnectionImplementation(type, interfaces))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), type.Name));
    }

    private static bool IsConnectionImplementation(INamedTypeSymbol type, ImmutableArray<INamedTypeSymbol> interfaces)
        => type.TypeKind == TypeKind.Class &&
           !type.IsAbstract &&
           type.AllInterfaces.Any(i => interfaces.Any(c => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, c)));

    /// <summary>Whether the assembly's own source declares a connection implementation type: then it is connection layer.</summary>
    private static bool DeclaresConnectionImplementation(INamespaceSymbol ns, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol child && DeclaresConnectionImplementation(child, interfaces))
                return true;

            if (member is INamedTypeSymbol type && DeclaresConnectionImplementation(type, interfaces))
                return true;
        }

        return false;
    }

    private static bool DeclaresConnectionImplementation(INamedTypeSymbol type, ImmutableArray<INamedTypeSymbol> interfaces)
        => IsConnectionImplementation(type, interfaces) ||
           type.GetTypeMembers().Any(nested => DeclaresConnectionImplementation(nested, interfaces));
}
