using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that warns when a component-local helper maps a value to CSS class strings instead of
/// the mapping living in a shared component or helper.
/// </summary>
/// <remarks>
/// <para>
/// The rule is deliberately conservative — it prefers missing a violation to reporting a
/// well-behaved helper. All three gates below must hold before anything is reported:
/// </para>
/// <list type="number">
/// <item><description>Shape: a non-public, single-parameter method returning <see langword="string"/>
/// that is not an override and not an explicit interface implementation.</description></item>
/// <item><description>Payload: every string literal in result position is a styling literal
/// (a marker-prefixed CSS class list, a hex colour, a <c>var(--x)</c> reference, or a CSS
/// declaration), and at least two of them are distinct.</description></item>
/// <item><description>Provenance: the reported location maps back to hand-written source. Razor
/// <c>@code</c> blocks qualify (they map to the <c>.razor</c> file); machine-generated
/// <c>*.g.cs</c> does not.</description></item>
/// </list>
/// <para>
/// What is being mapped is deliberately NOT consulted. Statuses are the most common case and have
/// an existing home in <c>StatusVariants</c>/<c>StatusColors</c>, but roles, HTTP verbs and node
/// types get the same treatment in <c>Fdw.UI.Pages</c> today; keying on a status vocabulary made the
/// rule fire on some of those and miss others for no principled reason.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LocalStatusClassMapperAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for the component-local status-to-CSS-class mapper violation.
    /// </summary>
    public const string DiagnosticId = "FDW048";

    private const string Title = "Value is mapped to CSS classes by a component-local helper";
    private const string MessageFormat = "Method '{0}' maps a value to CSS class strings in the component; move the mapping to a shared component or helper (for status/severity/state/health, the Fdw.UI.Components StatusVariants/StatusColors TypeCollection already exists)";
    private const string Description = "Fdw convention: presentation mapping belongs in one shared place, not re-derived per component. A private switch over CSS class literals forks that mapping and drifts -- Fdw.UI.Pages carries 17 such helpers under five different names, mapping statuses, roles, HTTP verbs and node types. Where the value is a status, severity, state or health, the StatusVariants/StatusColors TypeCollection is the existing home; otherwise extract a shared component or helper.";
    private const string Category = "Design";

    /// <summary>
    /// CSS class tokens that, on their own, identify a literal as a styling class rather than
    /// arbitrary kebab-case prose such as "in-progress". Matched as a whole token.
    /// </summary>
    private static readonly string[] ClassMarkerWords =
    [
        "badge",
        "chip",
        "pill",
        "tag",
        "dot",
        "alert",
        "label",
    ];

    /// <summary>
    /// CSS class token prefixes that identify a literal as a styling class.
    /// </summary>
    private static readonly string[] ClassMarkerPrefixes =
    [
        "b-",
        "bg-",
        "text-",
        "border-",
        "badge-",
        "chip-",
        "pill-",
        "tag-",
        "dot-",
        "status-",
        "state-",
        "sev-",
        "severity-",
        "health-",
        "alert-",
        "label-",
        "btn-",
        "ring-",
        "fill-",
        "stroke-",
    ];

    /// <summary>
    /// File-name suffixes that mark a path as machine-generated output rather than hand-written source.
    /// </summary>
    private static readonly string[] GeneratedFileSuffixes =
    [
        ".g.cs",
        ".g.i.cs",
        ".generated.cs",
        ".designer.cs",
    ];

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
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;

        if (!HasMapperShape(method))
            return;

        if (!ReturnsOnlyStylingLiterals(method))
            return;

        var location = method.Identifier.GetLocation();

        if (IsGeneratedPath(location.GetMappedLineSpan().Path))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, location, method.Identifier.Text));
    }

    /// <summary>
    /// Gate 1 — the structural shape of a component-local mapper: a non-public helper taking one
    /// value and returning a string, whose signature the author is free to change.
    /// </summary>
    private static bool HasMapperShape(MethodDeclarationSyntax method)
    {
        if (method.ParameterList.Parameters.Count != 1)
            return false;

        if (!IsStringType(method.ReturnType))
            return false;

        if (method.ExplicitInterfaceSpecifier != null)
            return false;

        foreach (var modifier in method.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifier.IsKind(SyntaxKind.OverrideKeyword) ||
                modifier.IsKind(SyntaxKind.PartialKeyword) ||
                modifier.IsKind(SyntaxKind.ExternKeyword))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gate 3 — every string literal the method can return is a styling literal, and at least two
    /// of them differ. Requiring all of them keeps helpers that return display text
    /// (GetHealthBadgeText: "Healthy"/"Unhealthy"/"Unknown") out of the rule.
    /// </summary>
    private static bool ReturnsOnlyStylingLiterals(MethodDeclarationSyntax method)
    {
        var literals = new List<string>();
        CollectResultLiterals(method, literals);

        if (literals.Count < 2)
            return false;

        var distinctStyling = new HashSet<string>(StringComparer.Ordinal);

        foreach (var literal in literals)
        {
            if (!IsStylingLiteral(literal))
                return false;

            distinctStyling.Add(literal);
        }

        return distinctStyling.Count >= 2;
    }

    private static void CollectResultLiterals(MethodDeclarationSyntax method, List<string> literals)
    {
        if (method.ExpressionBody?.Expression is { } arrowBody)
            FlattenResultExpression(arrowBody, literals);

        if (method.Body is not { } body)
            return;

        foreach (var node in body.DescendantNodes(descendIntoChildren: n => !IsNestedFunction(n)))
        {
            if (node is ReturnStatementSyntax { Expression: { } returned })
                FlattenResultExpression(returned, literals);
        }
    }

    private static bool IsNestedFunction(SyntaxNode node)
        => node is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax;

    /// <summary>
    /// Walks an expression in result position down to the string literals it can evaluate to,
    /// covering the four shapes the codebase actually uses: switch expression, switch statement
    /// and if-chain (both reached through their return statements), and conditional expression.
    /// </summary>
    private static void FlattenResultExpression(ExpressionSyntax expression, List<string> literals)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                FlattenResultExpression(parenthesized.Expression, literals);
                return;

            case CastExpressionSyntax cast:
                FlattenResultExpression(cast.Expression, literals);
                return;

            case ConditionalExpressionSyntax conditional:
                FlattenResultExpression(conditional.WhenTrue, literals);
                FlattenResultExpression(conditional.WhenFalse, literals);
                return;

            case SwitchExpressionSyntax switchExpression:
                foreach (var arm in switchExpression.Arms)
                    FlattenResultExpression(arm.Expression, literals);

                return;

            case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
                literals.Add(literal.Token.ValueText);
                return;

            default:
                return;
        }
    }

    /// <summary>
    /// Determines whether a returned literal is presentation rather than data: a CSS class list, a
    /// hex colour, a custom-property reference, or a CSS declaration.
    /// </summary>
    internal static bool IsStylingLiteral(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        if (IsHexColor(trimmed))
            return true;

        if (trimmed.IndexOf("var(--", StringComparison.Ordinal) >= 0)
            return true;

        if (IsCssDeclaration(trimmed))
            return true;

        return IsCssClassList(trimmed);
    }

    private static bool IsHexColor(string value)
    {
        if (value.Length is not (4 or 5 or 7 or 9) || value[0] != '#')
            return false;

        for (var i = 1; i < value.Length; i++)
        {
            if (!Uri.IsHexDigit(value[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Recognises an inline style payload such as "color:var(--success);". The trailing semicolon
    /// is required so that ordinary prose containing a colon is not mistaken for CSS.
    /// </summary>
    private static bool IsCssDeclaration(string value)
    {
        if (value[value.Length - 1] != ';')
            return false;

        var colon = value.IndexOf(':');
        if (colon <= 0 || colon == value.Length - 1)
            return false;

        for (var i = 0; i < colon; i++)
        {
            var c = value[i];
            if (!IsAsciiLower(c) && c != '-')
                return false;
        }

        return true;
    }

    /// <summary>
    /// Recognises a space-separated CSS class list in which at least one token carries a styling
    /// marker. The marker requirement is what separates "badge b-ok" from kebab-case data such as
    /// "in-progress".
    /// </summary>
    private static bool IsCssClassList(string value)
    {
        var anyMarker = false;

        foreach (var token in value.Split(' '))
        {
            if (token.Length == 0)
                continue;

            if (!IsCssIdentifier(token))
                return false;

            if (IsClassMarker(token))
                anyMarker = true;
        }

        return anyMarker;
    }

    private static bool IsCssIdentifier(string token)
    {
        if (!IsAsciiLetter(token[0]) && token[0] != '_')
            return false;

        for (var i = 1; i < token.Length; i++)
        {
            var c = token[i];
            if (!IsAsciiLetter(c) && !IsAsciiDigit(c) && c != '-' && c != '_' && c != ':' && c != '/')
                return false;
        }

        return true;
    }

    private static bool IsClassMarker(string token)
    {
        foreach (var word in ClassMarkerWords)
        {
            if (string.Equals(token, word, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var prefix in ClassMarkerPrefixes)
        {
            if (token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsStringType(TypeSyntax type)
    {
        var unwrapped = type is NullableTypeSyntax nullable ? nullable.ElementType : type;

        if (unwrapped is PredefinedTypeSyntax predefined)
            return predefined.Keyword.IsKind(SyntaxKind.StringKeyword);

        return string.Equals(GetSimpleTypeName(unwrapped), "String", StringComparison.Ordinal);
    }

    /// <summary>
    /// Reduces a type reference to its simple name, unwrapping nullable and qualified forms, so
    /// <c>System.String</c> and <c>string?</c> both resolve to "String".
    /// </summary>
    private static string GetSimpleTypeName(TypeSyntax? type)
    {
        while (true)
        {
            switch (type)
            {
                case NullableTypeSyntax nullable:
                    type = nullable.ElementType;
                    continue;

                case QualifiedNameSyntax qualified:
                    type = qualified.Right;
                    continue;

                case GenericNameSyntax generic:
                    return generic.Identifier.Text;

                case IdentifierNameSyntax identifier:
                    return identifier.Identifier.Text;

                default:
                    return string.Empty;
            }
        }
    }

    private static bool IsGeneratedPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var fileName = Path.GetFileName(path);

        foreach (var suffix in GeneratedFileSuffixes)
        {
            if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

    private static bool IsAsciiLower(char c) => c >= 'a' && c <= 'z';

    private static bool IsAsciiDigit(char c) => c >= '0' && c <= '9';
}
