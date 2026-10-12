using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// Analyzer that warns when a GenericResult value is not checked for success or failure.
/// Fdw convention: GenericResult values must always be inspected.
/// Detects three tiers:
///   Tier 1 - Fire-and-forget expression statement (await service.Execute(ct);)
///   Tier 2 - Discard assignment (_ = await service.Execute(ct);)
///   Tier 3 - Assigned but never checked (var r = await service.Execute(ct); /* r never checked */)
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UncheckedGenericResultAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for unchecked GenericResult violation.
    /// </summary>
    public const string DiagnosticId = "FDW012";

    private const string Title = "GenericResult value is not checked";
    private const string MessageFormat = "The return value of '{0}' should be checked for success or failure";
    private const string Description = "Fdw convention: GenericResult values must be checked for success or failure. Ignoring a result can cause silent failures.";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    private static readonly ImmutableHashSet<string> CheckedPropertyNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "IsSuccess",
        "IsFailure",
        "Error",
        "Messages",
        "Code",
        "CodeChain",
        "RootCause",
        "InnerResult",
        "CurrentMessage");

    private static readonly ImmutableHashSet<string> CheckedMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Map",
        "Match");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.ExpressionStatement);
        context.RegisterSyntaxNodeAction(AnalyzeLocalDeclaration, SyntaxKind.LocalDeclarationStatement);
    }

    /// <summary>
    /// Tier 1: Fire-and-forget expression statement.
    /// Catches: await service.Execute(ct); or service.Execute(ct);
    /// where the method returns IGenericResult.
    /// </summary>
    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context)
    {
        if (IsTestProject(context))
            return;

        var expressionStatement = (ExpressionStatementSyntax)context.Node;
        var expression = expressionStatement.Expression;

        // Unwrap await / ConfigureAwait to find the underlying invocation
        var invocation = UnwrapToInvocation(expression);
        if (invocation == null)
            return;

        var returnType = GetInvocationReturnType(invocation, context.SemanticModel);
        if (returnType == null)
            return;

        var unwrappedType = UnwrapTaskType(returnType);
        if (!IsGenericResultType(unwrappedType))
            return;

        var methodName = GetMethodDisplayName(invocation);

        var diagnostic = Diagnostic.Create(
            Rule,
            expressionStatement.GetLocation(),
            methodName);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Tier 2 and Tier 3: Local declaration analysis.
    /// Tier 2: _ = await service.Execute(ct);
    /// Tier 3: var r = await service.Execute(ct); where r is never checked.
    /// </summary>
    private static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (IsTestProject(context))
            return;

        var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;

        // We only handle single-variable declarations
        if (localDeclaration.Declaration.Variables.Count != 1)
            return;

        var variable = localDeclaration.Declaration.Variables[0];
        var initializer = variable.Initializer;
        if (initializer == null)
            return;

        var initializerExpression = initializer.Value;

        // Unwrap await / ConfigureAwait to find the underlying invocation
        var invocation = UnwrapToInvocation(initializerExpression);
        if (invocation == null)
            return;

        var returnType = GetInvocationReturnType(invocation, context.SemanticModel);
        if (returnType == null)
            return;

        var unwrappedType = UnwrapTaskType(returnType);
        if (!IsGenericResultType(unwrappedType))
            return;

        var variableName = variable.Identifier.Text;
        var methodName = GetMethodDisplayName(invocation);

        // Tier 2: Discard assignment (_ = ...)
        if (string.Equals(variableName, "_", StringComparison.Ordinal))
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                localDeclaration.GetLocation(),
                methodName);

            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Tier 3: Assigned but never checked
        if (!IsVariableChecked(variable, localDeclaration, context.SemanticModel))
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                localDeclaration.GetLocation(),
                methodName);

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Unwraps an expression through await and ConfigureAwait to find the underlying invocation.
    /// Handles: await x.ConfigureAwait(false), await x, and plain x.
    /// </summary>
    private static InvocationExpressionSyntax? UnwrapToInvocation(ExpressionSyntax expression)
    {
        var current = expression;

        // Unwrap await
        if (current is AwaitExpressionSyntax awaitExpression)
        {
            current = awaitExpression.Expression;
        }

        // Unwrap ConfigureAwait(...)
        if (current is InvocationExpressionSyntax possibleConfigureAwait &&
            possibleConfigureAwait.Expression is MemberAccessExpressionSyntax configureAccess &&
            string.Equals(configureAccess.Name.Identifier.Text, "ConfigureAwait", StringComparison.Ordinal))
        {
            current = configureAccess.Expression;
        }

        // Now we should have the actual invocation
        return current as InvocationExpressionSyntax;
    }

    /// <summary>
    /// Gets the return type of an invocation expression from the semantic model.
    /// </summary>
    private static ITypeSymbol? GetInvocationReturnType(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            return methodSymbol.ReturnType;

        return null;
    }

    /// <summary>
    /// Unwraps Task&lt;T&gt; or ValueTask&lt;T&gt; to get the inner type T.
    /// Returns the type as-is if it is not a Task wrapper.
    /// </summary>
    private static ITypeSymbol UnwrapTaskType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeName = namedType.Name;
            var namespaceName = namedType.ContainingNamespace?.ToDisplayString();

            var isTask = string.Equals(typeName, "Task", StringComparison.Ordinal) &&
                         string.Equals(namespaceName, "System.Threading.Tasks", StringComparison.Ordinal);

            var isValueTask = string.Equals(typeName, "ValueTask", StringComparison.Ordinal) &&
                              string.Equals(namespaceName, "System.Threading.Tasks", StringComparison.Ordinal);

            if ((isTask || isValueTask) && namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0];
            }
        }

        return type;
    }

    /// <summary>
    /// Determines whether a type is IGenericResult or GenericResult from Fdw.Results,
    /// or implements the IGenericResult interface.
    /// </summary>
    private static bool IsGenericResultType(ITypeSymbol? type)
    {
        if (type == null)
            return false;

        // Direct name check: IGenericResult or GenericResult
        if (IsGenericResultByName(type))
            return true;

        // Check all implemented interfaces
        foreach (var iface in type.AllInterfaces)
        {
            if (IsGenericResultByName(iface))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a type symbol has the name IGenericResult or GenericResult
    /// in the Fdw.Results namespace.
    /// </summary>
    private static bool IsGenericResultByName(ITypeSymbol type)
    {
        var typeName = type.Name;
        var namespaceName = type.ContainingNamespace?.ToDisplayString();

        if (!string.Equals(namespaceName, "Fdw.Results", StringComparison.Ordinal))
            return false;

        return string.Equals(typeName, "IGenericResult", StringComparison.Ordinal) ||
               string.Equals(typeName, "GenericResult", StringComparison.Ordinal);
    }

    /// <summary>
    /// Extracts a human-readable method name from an invocation expression for the diagnostic message.
    /// </summary>
    private static string GetMethodDisplayName(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.Text;
        }

        if (invocation.Expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.Text;
        }

        return invocation.Expression.ToString();
    }

    /// <summary>
    /// Determines whether the variable declared in a local declaration is ever "checked"
    /// in subsequent statements within the same method body.
    /// A variable is considered checked if any of its result-inspecting properties or methods
    /// are accessed, if it is returned, or if it is passed as an argument.
    /// </summary>
    private static bool IsVariableChecked(
        VariableDeclaratorSyntax variable,
        LocalDeclarationStatementSyntax declaration,
        SemanticModel semanticModel)
    {
        var variableName = variable.Identifier.Text;

        // Find the enclosing block (method body, local function body, lambda body, etc.)
        var enclosingBlock = FindEnclosingBlock(declaration);
        if (enclosingBlock == null)
            return true; // Cannot determine scope; assume checked to avoid false positives

        // Collect all statements after the declaration within the same block
        var statementsAfter = GetStatementsAfterDeclaration(enclosingBlock, declaration);

        foreach (var statement in statementsAfter)
        {
            if (StatementChecksVariable(statement, variableName, semanticModel))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the nearest enclosing block statement for a given node.
    /// </summary>
    private static BlockSyntax? FindEnclosingBlock(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is BlockSyntax block)
                return block;

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Collects all statements that appear after the given declaration within the enclosing block.
    /// </summary>
    private static List<StatementSyntax> GetStatementsAfterDeclaration(
        BlockSyntax block,
        LocalDeclarationStatementSyntax declaration)
    {
        var result = new List<StatementSyntax>();
        var foundDeclaration = false;

        foreach (var statement in block.Statements)
        {
            if (foundDeclaration)
            {
                result.Add(statement);
            }
            else if (statement == declaration)
            {
                foundDeclaration = true;
            }
        }

        return result;
    }

    /// <summary>
    /// Determines whether a statement (or any descendant node within it) constitutes a "check"
    /// of the given variable.
    /// </summary>
    private static bool StatementChecksVariable(
        StatementSyntax statement,
        string variableName,
        SemanticModel semanticModel)
    {
        foreach (var node in statement.DescendantNodes())
        {
            // Check for member access on the variable: r.IsSuccess, r.Code, etc.
            if (node is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is IdentifierNameSyntax memberIdentifier &&
                string.Equals(memberIdentifier.Identifier.Text, variableName, StringComparison.Ordinal))
            {
                var accessedName = memberAccess.Name.Identifier.Text;

                // Property access check
                if (CheckedPropertyNames.Contains(accessedName))
                    return true;

                // Method invocation check (Map, Match)
                if (CheckedMethodNames.Contains(accessedName))
                    return true;
            }

            // Check for return statement: return r;
            if (node is ReturnStatementSyntax returnStatement &&
                returnStatement.Expression is IdentifierNameSyntax returnIdentifier &&
                string.Equals(returnIdentifier.Identifier.Text, variableName, StringComparison.Ordinal))
            {
                return true;
            }

            // Check for argument usage: SomeMethod(r) or SomeMethod(result: r)
            if (node is ArgumentSyntax argument &&
                argument.Expression is IdentifierNameSyntax argumentIdentifier &&
                string.Equals(argumentIdentifier.Identifier.Text, variableName, StringComparison.Ordinal))
            {
                return true;
            }

            // Check for conditional access: r?.IsSuccess
            if (node is ConditionalAccessExpressionSyntax conditionalAccess &&
                conditionalAccess.Expression is IdentifierNameSyntax conditionalIdentifier &&
                string.Equals(conditionalIdentifier.Identifier.Text, variableName, StringComparison.Ordinal))
            {
                return true;
            }

            // Check for pattern matching: if (r is { IsSuccess: true })
            if (node is IsPatternExpressionSyntax isPattern &&
                isPattern.Expression is IdentifierNameSyntax isIdentifier &&
                string.Equals(isIdentifier.Identifier.Text, variableName, StringComparison.Ordinal))
            {
                return true;
            }

            // Check for switch statement/expression on the variable
            if (node is SwitchStatementSyntax switchStatement &&
                switchStatement.Expression is IdentifierNameSyntax switchIdentifier &&
                string.Equals(switchIdentifier.Identifier.Text, variableName, StringComparison.Ordinal))
            {
                return true;
            }

            if (node is SwitchExpressionSyntax switchExpression &&
                switchExpression.GoverningExpression is IdentifierNameSyntax switchExprIdentifier &&
                string.Equals(switchExprIdentifier.Identifier.Text, variableName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the current compilation is a test project by checking the assembly name.
    /// </summary>
    private static bool IsTestProject(SyntaxNodeAnalysisContext context)
    {
        var assemblyName = context.SemanticModel.Compilation.AssemblyName;
        if (assemblyName == null)
            return false;

        return assemblyName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.EndsWith(".Test", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.IndexOf(".Tests.", StringComparison.OrdinalIgnoreCase) >= 0 ||
               assemblyName.IndexOf(".Test.", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
