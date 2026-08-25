using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// Analyzer that warns against manually creating GenericMessage instances in production code.
/// Fdw convention: Messages should be created via MessageLogging methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ManualGenericMessageAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for manual GenericMessage creation violation.
    /// </summary>
    public const string DiagnosticId = "FDW004";

    private const string Title = "Use MessageLogging method instead of new GenericMessage()";
    private const string MessageFormat = "Manual 'new GenericMessage()' should be replaced with a MessageLogging method";
    private const string Description = "Fdw convention: Use MessageLogging methods (e.g., DomainLog.OperationFailed) instead of manually creating GenericMessage. This ensures logging integration and consistent message formatting.";
    private const string Category = "Usage";

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

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeImplicitObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        // Check the type being created
        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation);
        if (!IsGenericMessageType(typeInfo.Type))
            return;

        // Skip if we're in a test project
        if (IsTestProject(context))
            return;

        // Skip if we're inside the Messages namespace (framework code)
        if (IsInMessagesNamespace(context))
            return;

        // Skip if we're inside GenericResult (internal usage)
        if (IsInGenericResultClass(objectCreation))
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            objectCreation.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ImplicitObjectCreationExpressionSyntax)context.Node;

        // Check the type being created
        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation);
        if (!IsGenericMessageType(typeInfo.Type))
            return;

        // Skip if we're in a test project
        if (IsTestProject(context))
            return;

        // Skip if we're inside the Messages namespace (framework code)
        if (IsInMessagesNamespace(context))
            return;

        // Skip if we're inside GenericResult (internal usage)
        if (IsInGenericResultClass(objectCreation))
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            objectCreation.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsGenericMessageType(ITypeSymbol? type)
    {
        if (type == null)
            return false;

        // Check for GenericMessage
        if (string.Equals(type.Name, "GenericMessage", System.StringComparison.Ordinal) &&
            string.Equals(type.ContainingNamespace?.ToDisplayString(), "Fdw.Messages", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static bool IsTestProject(SyntaxNodeAnalysisContext context)
    {
        var assemblyName = context.SemanticModel.Compilation.AssemblyName;
        if (assemblyName == null)
            return false;

        return assemblyName.EndsWith(".Tests", System.StringComparison.OrdinalIgnoreCase) ||
               assemblyName.IndexOf(".Test.", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsInMessagesNamespace(SyntaxNodeAnalysisContext context)
    {
        var containingSymbol = context.ContainingSymbol;
        while (containingSymbol != null)
        {
            if (containingSymbol is INamespaceSymbol ns)
            {
                var nsName = ns.ToDisplayString();
                if (string.Equals(nsName, "Fdw.Messages", System.StringComparison.Ordinal) ||
                    nsName.StartsWith("Fdw.Messages.", System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
            containingSymbol = containingSymbol.ContainingSymbol;
        }
        return false;
    }

    private static bool IsInGenericResultClass(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is ClassDeclarationSyntax classDecl)
            {
                var className = classDecl.Identifier.Text;
                if (string.Equals(className, "GenericResult", System.StringComparison.Ordinal))
                    return true;
            }
            current = current.Parent;
        }
        return false;
    }
}
