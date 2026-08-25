using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// Analyzer that warns when a GenericResult is checked for success but the failure path is silently ignored.
/// Fdw convention: When checking a GenericResult for success, the failure path must also be explicitly handled.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnhandledFailurePathAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for unhandled failure path violation.
    /// </summary>
    public const string DiagnosticId = "FDW013";

    private const string Title = "GenericResult failure path is not handled";
    private const string MessageFormat = "The failure path for '{0}' is not handled — add an else clause, guard return, or subsequent failure check";
    private const string Description = "Fdw convention: When checking a GenericResult for success, the failure path must also be explicitly handled.";
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

        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        // Skip test projects
        if (IsTestProject(context))
            return;

        var ifStatement = (IfStatementSyntax)context.Node;

        // Extract the variable name and determine the branch kind from the condition
        var analysisResult = AnalyzeCondition(ifStatement.Condition, context.SemanticModel);
        if (analysisResult == null)
            return;

        var variableName = analysisResult.Value.VariableName;
        var isSuccessBranch = analysisResult.Value.IsSuccessBranch;

        // For failure-branch patterns (guard patterns), skip entirely — these ARE proper failure handling
        // e.g., if (result.IsFailure) return ...; or if (!result.IsSuccess) return ...;
        if (!isSuccessBranch)
            return;

        // For success-branch patterns: if (result.IsSuccess)
        // Check 1: If the if has an else clause, the failure path is handled
        if (ifStatement.Else != null)
            return;

        // Check 2: If the if body always returns or throws, code after is the failure path (guard-like)
        if (BlockAlwaysExits(ifStatement.Statement))
            return;

        // Check 3: Check if a subsequent statement in the SAME block checks the same variable for failure
        if (HasSubsequentFailureCheck(ifStatement, variableName, context.SemanticModel))
            return;

        // Check 4: Check if the same variable is returned after the if (passthrough pattern)
        // e.g., if (result.IsSuccess) { /* extra work */ } return result; — failure IS returned as-is
        if (HasSubsequentReturnOfVariable(ifStatement, variableName))
            return;

        // Check 5: Only enforce in methods that return IGenericResult (best-effort checks in void methods are OK)
        if (!EnclosingMethodReturnsGenericResult(ifStatement, context.SemanticModel))
            return;

        // None of the above — report FDW013
        var diagnostic = Diagnostic.Create(
            Rule,
            ifStatement.IfKeyword.GetLocation(),
            variableName);

        context.ReportDiagnostic(diagnostic);
    }

    private static ConditionAnalysisResult? AnalyzeCondition(ExpressionSyntax condition, SemanticModel semanticModel)
    {
        // Unwrap parenthesized expressions
        condition = UnwrapParentheses(condition);

        // Handle negation: !result.IsFailure → success branch, !result.IsSuccess → failure branch
        if (condition is PrefixUnaryExpressionSyntax prefixUnary &&
            prefixUnary.IsKind(SyntaxKind.LogicalNotExpression))
        {
            var inner = UnwrapParentheses(prefixUnary.Operand);
            var innerResult = ExtractMemberAccess(inner, semanticModel);
            if (innerResult != null)
            {
                // !IsFailure → success branch; !IsSuccess → failure branch
                var isSuccessBranch = string.Equals(innerResult.Value.PropertyName, "IsFailure", System.StringComparison.Ordinal);
                return new ConditionAnalysisResult(innerResult.Value.VariableName, isSuccessBranch);
            }

            return null;
        }

        // Handle direct member access: result.IsSuccess or result.IsFailure
        var directResult = ExtractMemberAccess(condition, semanticModel);
        if (directResult != null)
        {
            // IsSuccess → success branch; IsFailure → failure branch
            var isSuccessBranch = string.Equals(directResult.Value.PropertyName, "IsSuccess", System.StringComparison.Ordinal);
            return new ConditionAnalysisResult(directResult.Value.VariableName, isSuccessBranch);
        }

        // Handle compound conditions: result.IsSuccess && something
        if (condition is BinaryExpressionSyntax binaryExpression &&
            binaryExpression.IsKind(SyntaxKind.LogicalAndExpression))
        {
            // Check left side first, then right side
            var leftResult = AnalyzeCondition(binaryExpression.Left, semanticModel);
            if (leftResult != null)
                return leftResult;

            var rightResult = AnalyzeCondition(binaryExpression.Right, semanticModel);
            if (rightResult != null)
                return rightResult;
        }

        return null;
    }

    private static MemberAccessResult? ExtractMemberAccess(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        var propertyName = memberAccess.Name.Identifier.Text;

        // Only interested in IsSuccess or IsFailure
        if (!string.Equals(propertyName, "IsSuccess", System.StringComparison.Ordinal) &&
            !string.Equals(propertyName, "IsFailure", System.StringComparison.Ordinal))
        {
            return null;
        }

        // Get the variable name from the left side of the member access
        var variableName = GetVariableName(memberAccess.Expression);
        if (variableName == null)
            return null;

        // Verify the type implements IGenericResult via semantic model
        var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
        if (!IsGenericResultType(typeInfo.Type))
            return null;

        return new MemberAccessResult(variableName, propertyName);
    }

    private static string? GetVariableName(ExpressionSyntax expression)
    {
        // Simple identifier: result
        if (expression is IdentifierNameSyntax identifier)
            return identifier.Identifier.Text;

        // Member access: this.result or obj.Result
        if (expression is MemberAccessExpressionSyntax memberAccess)
            return memberAccess.ToString();

        return null;
    }

    private static bool IsGenericResultType(ITypeSymbol? type)
    {
        if (type == null)
            return false;

        // Check if the type itself is IGenericResult or GenericResult
        if (IsGenericResultTypeName(type))
            return true;

        // Check interfaces
        foreach (var iface in type.AllInterfaces)
        {
            if (IsGenericResultTypeName(iface))
                return true;
        }

        // Check base types
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (IsGenericResultTypeName(baseType))
                return true;

            foreach (var iface in baseType.AllInterfaces)
            {
                if (IsGenericResultTypeName(iface))
                    return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool IsGenericResultTypeName(ITypeSymbol type)
    {
        var typeName = type.Name;
        var namespaceName = type.ContainingNamespace?.ToDisplayString();

        if (string.Equals(namespaceName, "Fdw.Results", System.StringComparison.Ordinal))
        {
            if (string.Equals(typeName, "IGenericResult", System.StringComparison.Ordinal) ||
                string.Equals(typeName, "GenericResult", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool BlockAlwaysExits(StatementSyntax statement)
    {
        // A block statement: check if all code paths exit
        if (statement is BlockSyntax block)
        {
            if (block.Statements.Count == 0)
                return false;

            return StatementAlwaysExits(block.Statements.Last());
        }

        // A single statement (no braces)
        return StatementAlwaysExits(statement);
    }

    private static bool StatementAlwaysExits(StatementSyntax statement)
    {
        switch (statement)
        {
            case ReturnStatementSyntax:
            case ThrowStatementSyntax:
            case BreakStatementSyntax:
            case ContinueStatementSyntax:
                return true;

            case BlockSyntax block:
                return block.Statements.Count > 0 && StatementAlwaysExits(block.Statements.Last());

            case IfStatementSyntax ifStatement:
                // Both branches must always exit
                if (ifStatement.Else == null)
                    return false;

                return BlockAlwaysExits(ifStatement.Statement) &&
                       BlockAlwaysExits(ifStatement.Else.Statement);

            case SwitchStatementSyntax switchStatement:
                // All sections must exit, and there must be a default
                var hasDefault = switchStatement.Sections.Any(s =>
                    s.Labels.Any(l => l is DefaultSwitchLabelSyntax));

                if (!hasDefault)
                    return false;

                return switchStatement.Sections.All(s =>
                    s.Statements.Count > 0 && StatementAlwaysExits(s.Statements.Last()));

            default:
                return false;
        }
    }

    private static bool HasSubsequentFailureCheck(IfStatementSyntax ifStatement, string variableName, SemanticModel semanticModel)
    {
        // The if statement must be inside a block to have subsequent statements
        if (ifStatement.Parent is not BlockSyntax parentBlock)
            return false;

        var statements = parentBlock.Statements;
        var ifIndex = -1;

        for (var i = 0; i < statements.Count; i++)
        {
            if (statements[i] == ifStatement)
            {
                ifIndex = i;
                break;
            }
        }

        if (ifIndex < 0)
            return false;

        // Check subsequent statements for a failure check on the same variable
        for (var i = ifIndex + 1; i < statements.Count; i++)
        {
            if (statements[i] is IfStatementSyntax subsequentIf)
            {
                if (IsFailureCheckForVariable(subsequentIf.Condition, variableName, semanticModel))
                    return true;
            }
        }

        return false;
    }

    private static bool IsFailureCheckForVariable(ExpressionSyntax condition, string variableName, SemanticModel semanticModel)
    {
        condition = UnwrapParentheses(condition);

        // Check for: variableName.IsFailure
        if (condition is MemberAccessExpressionSyntax memberAccess)
        {
            var propName = memberAccess.Name.Identifier.Text;
            if (string.Equals(propName, "IsFailure", System.StringComparison.Ordinal))
            {
                var leftName = GetVariableName(memberAccess.Expression);
                if (string.Equals(leftName, variableName, System.StringComparison.Ordinal))
                {
                    // Verify it's still a GenericResult type
                    var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
                    return IsGenericResultType(typeInfo.Type);
                }
            }
        }

        // Check for: !variableName.IsSuccess
        if (condition is PrefixUnaryExpressionSyntax prefixUnary &&
            prefixUnary.IsKind(SyntaxKind.LogicalNotExpression))
        {
            var inner = UnwrapParentheses(prefixUnary.Operand);
            if (inner is MemberAccessExpressionSyntax innerMemberAccess)
            {
                var propName = innerMemberAccess.Name.Identifier.Text;
                if (string.Equals(propName, "IsSuccess", System.StringComparison.Ordinal))
                {
                    var leftName = GetVariableName(innerMemberAccess.Expression);
                    if (string.Equals(leftName, variableName, System.StringComparison.Ordinal))
                    {
                        var typeInfo = semanticModel.GetTypeInfo(innerMemberAccess.Expression);
                        return IsGenericResultType(typeInfo.Type);
                    }
                }
            }
        }

        return false;
    }

    private static bool HasSubsequentReturnOfVariable(IfStatementSyntax ifStatement, string variableName)
    {
        if (ifStatement.Parent is not BlockSyntax parentBlock)
            return false;

        var statements = parentBlock.Statements;
        var ifIndex = -1;

        for (var i = 0; i < statements.Count; i++)
        {
            if (statements[i] == ifStatement)
            {
                ifIndex = i;
                break;
            }
        }

        if (ifIndex < 0)
            return false;

        for (var i = ifIndex + 1; i < statements.Count; i++)
        {
            if (statements[i] is ReturnStatementSyntax returnStatement &&
                returnStatement.Expression is IdentifierNameSyntax identifier &&
                string.Equals(identifier.Identifier.Text, variableName, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EnclosingMethodReturnsGenericResult(IfStatementSyntax ifStatement, SemanticModel semanticModel)
    {
        var current = ifStatement.Parent;
        while (current != null)
        {
            if (current is MethodDeclarationSyntax methodDecl)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl);
                if (methodSymbol == null)
                    return true; // Cannot determine; assume yes to avoid false negatives

                return IsGenericResultReturnType(methodSymbol.ReturnType);
            }

            if (current is LocalFunctionStatementSyntax localFunc)
            {
                var localSymbol = semanticModel.GetDeclaredSymbol(localFunc);
                if (localSymbol == null)
                    return true;

                return IsGenericResultReturnType(localSymbol.ReturnType);
            }

            current = current.Parent;
        }

        return true; // Cannot find enclosing method; assume yes
    }

    private static bool IsGenericResultReturnType(ITypeSymbol? type)
    {
        if (type == null)
            return false;

        // Unwrap Task<T> or ValueTask<T>
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeName = namedType.Name;
            var namespaceName = namedType.ContainingNamespace?.ToDisplayString();

            if ((string.Equals(typeName, "Task", System.StringComparison.Ordinal) ||
                 string.Equals(typeName, "ValueTask", System.StringComparison.Ordinal)) &&
                string.Equals(namespaceName, "System.Threading.Tasks", System.StringComparison.Ordinal) &&
                namedType.TypeArguments.Length == 1)
            {
                type = namedType.TypeArguments[0];
            }
        }

        return IsGenericResultType(type);
    }

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    private static bool IsTestProject(SyntaxNodeAnalysisContext context)
    {
        var assemblyName = context.SemanticModel.Compilation.AssemblyName;
        if (assemblyName == null)
            return false;

        return assemblyName.EndsWith(".Tests", System.StringComparison.OrdinalIgnoreCase) ||
               assemblyName.IndexOf(".Test.", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private readonly struct ConditionAnalysisResult
    {
        public ConditionAnalysisResult(string variableName, bool isSuccessBranch)
        {
            VariableName = variableName;
            IsSuccessBranch = isSuccessBranch;
        }

        public string VariableName { get; }
        public bool IsSuccessBranch { get; }
    }

    private readonly struct MemberAccessResult
    {
        public MemberAccessResult(string variableName, string propertyName)
        {
            VariableName = variableName;
            PropertyName = propertyName;
        }

        public string VariableName { get; }
        public string PropertyName { get; }
    }
}
