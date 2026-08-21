using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// Requires a <c>[TypeOption]</c> / <c>[TypeCollection]</c> class to set its own phase func with
/// <c>Configuration</c>, <c>Registration</c> or <c>Initialization</c>, and forbids the
/// <c>Append</c>/<c>Prepend</c> variants inside it.
/// </summary>
// Why this exists: a phase holds ONE func, and the class that declares the phase owns it outright.
// Append and Prepend are for customising an option somebody else shipped — a consumer bolting an extra
// registration onto a type it does not author. Used from inside the authoring class they mean the
// opposite: that some other contributor also holds part of this phase, and that this class's own body
// must be arranged around whatever that contributor left behind. Once one base class does it, every
// derived option must Append forever or silently destroy the base's contribution, and each one carries a
// paragraph explaining the hazard instead of just declaring what it registers. The composition seam is
// not the problem; using it where the ownership already sits is.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PhaseFuncCompositionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic ID for a phase func composed onto rather than set.</summary>
    public const string DiagnosticId = "FDW049";

    private const string Category = "Design";

    private static readonly LocalizableString Title =
        "A type option or type collection sets its own phase func";

    private static readonly LocalizableString MessageFormat =
        "'{0}' composes onto a phase body this class already owns — call '{1}' to set it";

    private static readonly LocalizableString Description =
        "Each phase holds one func, and the class declaring the phase owns it. AppendRegistration, PrependRegistration and their Configuration/Initialization counterparts exist so a consumer can customise an option it did not author. Inside the authoring class they signal shared ownership of a phase that has a single owner, which forces every later contributor to compose defensively or silently discard work. Set the func with Configuration, Registration or Initialization, and if the body needs what a captured func would have run, capture it in a local and call it explicitly where it can be read.";

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

        var setter = PrimarySetterFor(name);
        if (setter is null)
            return;

        if (!IsPhaseAuthoringType(invocation, context.SemanticModel))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            NameLocation(invocation.Expression),
            name,
            setter));
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

    private static bool IsPhaseAuthoringType(SyntaxNode node, SemanticModel semanticModel)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is not TypeDeclarationSyntax typeDeclaration)
                continue;

            // Why the first enclosing type decides and the walk then stops: a nested type is its own
            // authoring unit, and continuing outward would judge it by the attributes of whatever
            // happens to contain it.
            if (semanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol type)
                return false;

            foreach (var attribute in type.GetAttributes())
            {
                var name = attribute.AttributeClass?.Name;
                if (string.Equals(name, "ServiceTypeOptionAttribute", StringComparison.Ordinal)
                    || string.Equals(name, "ServiceTypeCollectionAttribute", StringComparison.Ordinal)
                    || string.Equals(name, "TypeOptionAttribute", StringComparison.Ordinal)
                    || string.Equals(name, "TypeCollectionAttribute", StringComparison.Ordinal)
                    || string.Equals(name, "PlatformServiceProviderAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        return false;
    }
}
