using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// Governs who sets a phase func: the service type or service type collection that declares the phase
/// sets it outright, and nothing between it and <c>ServiceTypeBase</c> holds a piece of it.
/// </summary>
// Why service types only: the three phases are optional on a plain [TypeOption] / [TypeCollection], which
// is an enhanced-enum member and frequently has no host wiring at all. A service type is the shape that
// always has phases, so it is the shape where ownership of one can be stated.
//
// Why two rules from one analyzer: they are one rule seen from the two places a phase func can be written.
// The declaring class must own its phase outright (STC001), and nothing beneath it in the chain may hold
// part of one (STC002). Either alone leaves the other half of the hazard open.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PhaseFuncCompositionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic ID for a phase func composed onto rather than set.</summary>
    public const string CompositionDiagnosticId = "STC001";

    /// <summary>Diagnostic ID for a phase func set from an intermediate base class.</summary>
    public const string IntermediateDiagnosticId = "STC002";

    private const string Category = "Design";

    private static readonly DiagnosticDescriptor CompositionRule = new DiagnosticDescriptor(
        CompositionDiagnosticId,
        "A service type sets its own phase func",
        "'{0}' composes onto a phase body this class already owns — call '{1}' to set it",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each phase holds one func, and the service type or service type collection declaring the phase owns it. AppendRegistration, PrependRegistration and their Configuration/Initialization counterparts exist so a consumer can customise an option it did not author. Inside the declaring class they signal shared ownership of a phase that has a single owner, which forces every later contributor to compose defensively or silently discard work. Set the func with Configuration, Registration or Initialization; if the body needs what a captured func would have run, capture it in a local and call it explicitly where it can be read.");

    private static readonly DiagnosticDescriptor IntermediateRule = new DiagnosticDescriptor(
        IntermediateDiagnosticId,
        "Only the declared service type sets a phase func",
        "'{0}' sets a phase func from a base class between ServiceTypeBase and the declared service type — wiring that every option needs belongs in the collection's Register",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A phase belongs to the [ServiceTypeOption] or [ServiceTypeCollection] class that declares it. An intermediate base class holding part of a phase leaves the leaf option unable to set its own func without silently destroying what the base contributed, and neither site shows that it happened. Wiring that every option of a domain needs applies to the domain rather than to any one option, so it belongs in the collection's Register body, where the option set is already in hand.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(CompositionRule, IntermediateRule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null) return;

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var name = InvokedName(invocation.Expression);
        if (name is null)
            return;

        var composedSetter = PrimarySetterFor(name);
        if (composedSetter is null && !IsPrimarySetter(name))
            return;

        var declaringType = EnclosingType(invocation, context.SemanticModel);
        if (declaringType is null || !IsServiceTypeShape(declaringType))
            return;

        if (composedSetter is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CompositionRule,
                NameLocation(invocation.Expression),
                name,
                composedSetter));
            return;
        }

        // Why the declared type is exempt and the receiver is checked: setting the phase is exactly what a
        // [ServiceTypeOption] / [ServiceTypeCollection] class is for, and `option.Registration(...)` on
        // somebody else's instance is the ordinary customisation the seam exists for. What is left is a
        // base class in the middle of the chain contributing to a phase it does not declare.
        if (IsDeclaredServiceType(declaringType) || !IsImplicitReceiver(invocation.Expression))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            IntermediateRule,
            NameLocation(invocation.Expression),
            name));
    }

    private static string? InvokedName(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
        _ => null
    };

    private static Location NameLocation(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.GetLocation(),
        _ => expression.GetLocation()
    };

    // Why "this" counts as implicit: `this.Registration(...)` from inside the class is the same call as the
    // bare one, and it is the instance being the class itself that makes it a contribution to its own phase.
    private static bool IsImplicitReceiver(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax => true,
        MemberAccessExpressionSyntax member => member.Expression is ThisExpressionSyntax,
        _ => false
    };

    private static bool IsPrimarySetter(string name) =>
        string.Equals(name, "Configuration", StringComparison.Ordinal)
        || string.Equals(name, "Registration", StringComparison.Ordinal)
        || string.Equals(name, "Initialization", StringComparison.Ordinal);

    // Why the mapping is spelled out rather than trimming an "Append"/"Prepend" prefix: the message names
    // the exact call the author should make instead, and a prefix trim would happily invent one for any
    // unrelated method that starts with the same word.
    private static string? PrimarySetterFor(string name) => name switch
    {
        "AppendConfiguration" or "PrependConfiguration" => "Configuration",
        "AppendRegistration" or "PrependRegistration" => "Registration",
        "AppendInitialization" or "PrependInitialization" => "Initialization",
        _ => null
    };

    private static INamedTypeSymbol? EnclosingType(SyntaxNode node, SemanticModel semanticModel)
    {
        foreach (var ancestor in node.Ancestors())
        {
            // Why the first enclosing type decides and the walk then stops: a nested type is its own unit,
            // and continuing outward would judge it by whatever happens to contain it.
            if (ancestor is TypeDeclarationSyntax typeDeclaration)
                return semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
        }

        return null;
    }

    // Why the base chain and not the attribute: the classes this most needs to reach are the ones between
    // ServiceTypeBase and the declared option, and those carry no attribute — that is what makes them
    // intermediate. Matching the chain also excludes TypeOptionBase / TypeCollectionBase for free.
    private static bool IsServiceTypeShape(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (string.Equals(current.Name, "ServiceTypeBase", StringComparison.Ordinal)
                || string.Equals(current.Name, "ServiceTypeCollectionBase", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDeclaredServiceType(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            var name = attribute.AttributeClass?.Name;
            if (string.Equals(name, "ServiceTypeOptionAttribute", StringComparison.Ordinal)
                || string.Equals(name, "ServiceTypeCollectionAttribute", StringComparison.Ordinal)
                || string.Equals(name, "PlatformServiceProviderAttribute", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
