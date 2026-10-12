using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Reads the text of a load step's <c>filterExpression</c> into the data layer's filter expression (FDW-1109), in the
/// same language the Filter transform reads: <c>field op value</c> with ==, !=, &gt;=, &lt;=, &gt;, &lt;, contains,
/// startswith and endswith, <c>field == null</c> and <c>field != null</c>, joined with AND / OR and grouped with
/// parentheses. A value in quotes is text; an unquoted one is read as a boolean, a whole number, a decimal, an ISO date
/// or, failing those, text.
/// </summary>
/// <remarks>
/// Why a parser here and not the Filter transform's evaluator: a set-based load never sees the rows, so the condition has
/// to reach the database as a filter expression the translator renders with parameters. Anything the language does not
/// state is refused with the reason; nothing is guessed.
/// </remarks>
public static class LoadFilterExpressions
{
    private static readonly string[] OrSeparators = [" || ", " OR "];
    private static readonly string[] AndSeparators = [" && ", " AND "];
    private static readonly (string Token, string Operator)[] WordOperators =
        [(" contains ", "Contains"), (" startswith ", "StartsWith"), (" endswith ", "EndsWith")];
    private static readonly (string Token, string Operator)[] SymbolOperators =
        [(">=", "GreaterThanOrEqual"), ("<=", "LessThanOrEqual"), ("!=", "NotEqual"), ("==", "Equal"), (">", "GreaterThan"), ("<", "LessThan")];
    private static readonly Regex FieldName = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex DateStart = new(@"^\d{4}-\d{2}-\d{2}", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>Reads a filter expression.</summary>
    /// <param name="owner">What the expression belongs to, for the refusal message.</param>
    /// <param name="text">The expression text, with any run parameter references already resolved.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The filter expression, or a failure saying what could not be read.</returns>
    public static IGenericResult<IFilterExpression> Parse(string owner, string text, ILogger logger)
    {
        var (node, problem) = Node(text);
        return node is null
            ? GenericResult<IFilterExpression>.Failure(PipelineRunLog.FilterExpressionInvalid(logger, owner, problem!))
            : GenericResult<IFilterExpression>.Success(new FilterExpression { Root = node });
    }

    private static (IFilterNode? Node, string? Problem) Node(string text)
    {
        var expression = text.Trim();
        if (expression.Length == 0)
            return (null, "a condition is empty");

        var parts = Split(expression, OrSeparators);
        if (parts.Count > 1)
            return Group(LogicalOperator.Or, parts);
        parts = Split(expression, AndSeparators);
        if (parts.Count > 1)
            return Group(LogicalOperator.And, parts);
        return IsParenthesised(expression) ? Node(expression[1..^1]) : Comparison(expression);
    }

    private static (IFilterNode? Node, string? Problem) Group(LogicalOperator logical, List<string> parts)
    {
        var nodes = new List<IFilterNode>(parts.Count);
        foreach (var part in parts)
        {
            var (node, problem) = Node(part);
            if (node is null)
                return (null, problem);
            nodes.Add(node);
        }

        return (new FilterGroup { Operator = logical, Nodes = nodes }, null);
    }

    // True when the whole expression is one parenthesised group, not "(a) AND (b)".
    private static bool IsParenthesised(string expression)
    {
        if (expression[0] != '(' || expression[^1] != ')')
            return false;
        var top = TopLevel(expression);
        for (var index = 1; index < expression.Length - 1; index++)
        {
            if (expression[index] == ')' && top[index])
                return false;
        }

        return true;
    }

    // Marks each position that sits outside every quote and every parenthesis, where an operator can split the text.
    private static bool[] TopLevel(string text)
    {
        var top = new bool[text.Length];
        var depth = 0;
        var quote = '\0';
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quote != '\0')
                quote = character == quote ? '\0' : quote;
            else if (character is '\'' or '"')
                quote = character;
            else if (character == '(')
                depth++;
            else if (character == ')')
                depth--;
            top[index] = quote == '\0' && depth == 0 && character is not ('(' or ')');
        }

        return top;
    }

    private static List<string> Split(string text, string[] separators)
    {
        var top = TopLevel(text);
        var parts = new List<string>();
        var start = 0;
        var index = 0;
        while (index < text.Length)
        {
            var length = top[index] ? SeparatorLength(text, index, separators) : 0;
            if (length > 0)
            {
                parts.Add(text[start..index]);
                index += length;
                start = index;
            }
            else
            {
                index++;
            }
        }

        parts.Add(text[start..]);
        return parts;
    }

    private static int SeparatorLength(string text, int index, string[] separators)
    {
        foreach (var separator in separators)
        {
            if (string.Compare(text, index, separator, 0, separator.Length, StringComparison.OrdinalIgnoreCase) == 0)
                return separator.Length;
        }

        return 0;
    }

    private static (IFilterNode? Node, string? Problem) Comparison(string expression)
    {
        if (expression.EndsWith("== null", StringComparison.OrdinalIgnoreCase))
            return NullCondition(expression[..^7], "IsNull");
        if (expression.EndsWith("!= null", StringComparison.OrdinalIgnoreCase))
            return NullCondition(expression[..^7], "IsNotNull");

        foreach (var (token, name) in WordOperators)
        {
            var at = IndexOutsideQuotes(expression, token);
            if (at >= 0)
                return Condition(expression[..at], name, expression[(at + token.Length)..]);
        }

        foreach (var (token, name) in SymbolOperators)
        {
            var at = IndexOutsideQuotes(expression, token);
            if (at >= 0)
                return Condition(expression[..at], name, expression[(at + token.Length)..]);
        }

        return (null, $"'{expression}' is not a comparison");
    }

    private static int IndexOutsideQuotes(string text, string token)
    {
        var quote = '\0';
        for (var index = 0; index <= text.Length - token.Length; index++)
        {
            var character = text[index];
            if (quote != '\0')
                quote = character == quote ? '\0' : quote;
            else if (character is '\'' or '"')
                quote = character;
            else if (string.Compare(text, index, token, 0, token.Length, StringComparison.OrdinalIgnoreCase) == 0)
                return index;
        }

        return -1;
    }

    private static (IFilterNode? Node, string? Problem) NullCondition(string field, string operatorName)
    {
        var name = field.Trim();
        return FieldName.IsMatch(name)
            ? (new FilterCondition { PropertyName = name, Operator = FilterOperators.ByName(operatorName) }, null)
            : (null, $"'{name}' is not a field name");
    }

    private static (IFilterNode? Node, string? Problem) Condition(string field, string operatorName, string value)
    {
        var name = field.Trim();
        var text = value.Trim();
        if (!FieldName.IsMatch(name))
            return (null, $"'{name}' is not a field name");
        if (text.Length == 0)
            return (null, $"the comparison of '{name}' names no value");
        return (new FilterCondition { PropertyName = name, Operator = FilterOperators.ByName(operatorName), Value = Literal(text) }, null);
    }

    private static object Literal(string text)
    {
        if (text.Length >= 2 && text[0] is '\'' or '"' && text[^1] == text[0])
            return text[1..^1];
        if (bool.TryParse(text, out var flag))
            return flag;
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
            return whole;
        if (decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
            return number;
        if (DateStart.IsMatch(text) && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var moment))
            return moment;
        return text;
    }
}
