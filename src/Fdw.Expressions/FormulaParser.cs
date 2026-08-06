using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Expressions;

/// <summary>
/// Parses formula strings into LINQ expression trees.
/// </summary>
/// <remarks>
/// FormulaParser supports a simple expression language for calculations:
/// - Field access: FieldName or [Field With Spaces]
/// - Arithmetic: +, -, *, /, %
/// - Comparison: ==, !=, &lt;, &gt;, &lt;=, &gt;=
/// - Logical: &amp;&amp;, ||, !
/// - Parentheses for grouping
/// - Numeric and string literals
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class FormulaParser
{
    private readonly IDataSchema _schema;
    private readonly Dictionary<string, int> _fieldOrdinals;

    public FormulaParser(IDataSchema schema)
    {
        _schema = schema;
        _fieldOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Pre-cache field ordinals
        for (int i = 0; i < schema.Fields.Count; i++)
        {
            _fieldOrdinals[schema.Fields[i].Name] = i;
        }
    }

    /// <summary>
    /// Parses a formula string into a compiled expression.
    /// </summary>
    public Expression<Func<IDataRow, TResult>> Parse<TResult>(string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
            throw new ArgumentException("Formula cannot be empty", nameof(formula));

        // Tokenize the formula
        var tokens = Tokenize(formula);

        // Build expression tree
        var rowParam = Expression.Parameter(typeof(IDataRow), "row");
        var body = ParseExpression(tokens, rowParam);

        // Convert to TResult if necessary
        if (body.Type != typeof(TResult))
        {
            body = Expression.Convert(body, typeof(TResult));
        }

        return Expression.Lambda<Func<IDataRow, TResult>>(body, rowParam);
    }

    #region Parsing

    private Expression ParseExpression(Queue<Token> tokens, ParameterExpression rowParam)
    {
        return ParseLogicalOr(tokens, rowParam);
    }

    private Expression ParseLogicalOr(Queue<Token> tokens, ParameterExpression rowParam)
    {
        var left = ParseLogicalAnd(tokens, rowParam);

        while (tokens.Count > 0 && tokens.Peek().Type == TokenType.OrOperator)
        {
            tokens.Dequeue(); // consume ||
            var right = ParseLogicalAnd(tokens, rowParam);
            left = Expression.OrElse(left, right);
        }

        return left;
    }

    private Expression ParseLogicalAnd(Queue<Token> tokens, ParameterExpression rowParam)
    {
        var left = ParseComparison(tokens, rowParam);

        while (tokens.Count > 0 && tokens.Peek().Type == TokenType.AndOperator)
        {
            tokens.Dequeue(); // consume &&
            var right = ParseComparison(tokens, rowParam);
            left = Expression.AndAlso(left, right);
        }

        return left;
    }

    private Expression ParseComparison(Queue<Token> tokens, ParameterExpression rowParam)
    {
        var left = ParseAdditive(tokens, rowParam);

        if (tokens.Count > 0)
        {
            var token = tokens.Peek();
            Expression? right = null;

#pragma warning disable FDW018
            switch (token.Type)
            {
                case TokenType.Equals:
                    tokens.Dequeue();
                    right = ParseAdditive(tokens, rowParam);
                    return Expression.Equal(left, right);

                case TokenType.NotEquals:
                    tokens.Dequeue();
                    right = ParseAdditive(tokens, rowParam);
                    return Expression.NotEqual(left, right);

                case TokenType.LessThan:
                    tokens.Dequeue();
                    right = ParseAdditive(tokens, rowParam);
                    return Expression.LessThan(left, right);

                case TokenType.LessThanOrEqual:
                    tokens.Dequeue();
                    right = ParseAdditive(tokens, rowParam);
                    return Expression.LessThanOrEqual(left, right);

                case TokenType.GreaterThan:
                    tokens.Dequeue();
                    right = ParseAdditive(tokens, rowParam);
                    return Expression.GreaterThan(left, right);

                case TokenType.GreaterThanOrEqual:
                    tokens.Dequeue();
                    right = ParseAdditive(tokens, rowParam);
                    return Expression.GreaterThanOrEqual(left, right);
            }
#pragma warning restore FDW018
        }

        return left;
    }

#pragma warning disable FDW019
    private Expression ParseAdditive(Queue<Token> tokens, ParameterExpression rowParam)
    {
        var left = ParseMultiplicative(tokens, rowParam);

        while (tokens.Count > 0)
        {
            var token = tokens.Peek();
            if (token.Type == TokenType.Plus)
            {
                tokens.Dequeue();
                var right = ParseMultiplicative(tokens, rowParam);
                left = Expression.Add(left, right);
            }
            else if (token.Type == TokenType.Minus)
            {
                tokens.Dequeue();
                var right = ParseMultiplicative(tokens, rowParam);
                left = Expression.Subtract(left, right);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    private Expression ParseMultiplicative(Queue<Token> tokens, ParameterExpression rowParam)
    {
        var left = ParseUnary(tokens, rowParam);

        while (tokens.Count > 0)
        {
            var token = tokens.Peek();
            if (token.Type == TokenType.Multiply)
            {
                tokens.Dequeue();
                var right = ParseUnary(tokens, rowParam);
                left = Expression.Multiply(left, right);
            }
            else if (token.Type == TokenType.Divide)
            {
                tokens.Dequeue();
                var right = ParseUnary(tokens, rowParam);
                left = Expression.Divide(left, right);
            }
            else if (token.Type == TokenType.Modulo)
            {
                tokens.Dequeue();
                var right = ParseUnary(tokens, rowParam);
                left = Expression.Modulo(left, right);
            }
            else
            {
                break;
            }
        }

        return left;
    }
#pragma warning restore FDW019

    private Expression ParseUnary(Queue<Token> tokens, ParameterExpression rowParam)
    {
        if (tokens.Count > 0 && tokens.Peek().Type == TokenType.Not)
        {
            tokens.Dequeue();
            var operand = ParseUnary(tokens, rowParam);
            return Expression.Not(operand);
        }

        if (tokens.Count > 0 && tokens.Peek().Type == TokenType.Minus)
        {
            tokens.Dequeue();
            var operand = ParseUnary(tokens, rowParam);
            return Expression.Negate(operand);
        }

        return ParsePrimary(tokens, rowParam);
    }

    private Expression ParsePrimary(Queue<Token> tokens, ParameterExpression rowParam)
    {
        if (tokens.Count == 0)
            throw new InvalidOperationException("Unexpected end of formula");

        var token = tokens.Dequeue();

#pragma warning disable FDW018
        switch (token.Type)
        {
            case TokenType.Number:
                return Expression.Constant(decimal.Parse(token.Value!, System.Globalization.CultureInfo.InvariantCulture), typeof(decimal));

            case TokenType.String:
                return Expression.Constant(token.Value, typeof(string));

            case TokenType.Identifier:
                // Field access
                return CreateFieldAccess(token.Value!, rowParam);

            case TokenType.LeftParen:
                var expr = ParseExpression(tokens, rowParam);
                if (tokens.Count == 0 || tokens.Dequeue().Type != TokenType.RightParen)
                    throw new InvalidOperationException("Missing closing parenthesis");
                return expr;

            default:
                throw new InvalidOperationException($"Unexpected token: {token.Type}");
        }
#pragma warning restore FDW018
    }

    private MethodCallExpression CreateFieldAccess(string fieldName, ParameterExpression rowParam)
    {
        if (!_fieldOrdinals.TryGetValue(fieldName, out var ordinal))
            throw new ArgumentException($"Field '{fieldName}' not found in schema", nameof(fieldName));

        var field = _schema.Fields[ordinal];

        // Get: row.GetValue<DataType>(ordinal)
        var getValueMethod = typeof(IDataRow)
            .GetMethod(nameof(IDataRow.GetValue), new[] { typeof(int) })!
            .MakeGenericMethod(field.DataType);

        return Expression.Call(rowParam, getValueMethod, Expression.Constant(ordinal));
    }

    #endregion

    #region Tokenization

    // MA0051: Method length acceptable - lexer/tokenizer with comprehensive operator handling (numbers, strings, identifiers, operators)
#pragma warning disable MA0051 // Method is too long
    [ConventionOverride(MaxCyclomaticComplexity = 50, MaxMethodLines = 120)]  // Lexer/tokenizer with comprehensive character classification (numbers, strings, identifiers, operators, delimiters)
    private static Queue<Token> Tokenize(string formula)
#pragma warning restore MA0051
    {
        var tokens = new List<Token>();
        var i = 0;

        while (i < formula.Length)
        {
            // Skip whitespace
            if (char.IsWhiteSpace(formula[i]))
            {
                i++;
                continue;
            }

            // Numbers
            if (char.IsDigit(formula[i]) || formula[i] == '.')
            {
                var start = i;
                while (i < formula.Length && (char.IsDigit(formula[i]) || formula[i] == '.'))
                    i++;
                tokens.Add(new Token(TokenType.Number, formula.Substring(start, i - start)));
                continue;
            }

            // Strings (quoted)
            if (formula[i] == '"' || formula[i] == '\'')
            {
                var quote = formula[i];
                i++; // skip opening quote
                var start = i;
                while (i < formula.Length && formula[i] != quote)
                    i++;
                tokens.Add(new Token(TokenType.String, formula.Substring(start, i - start)));
                i++; // skip closing quote
                continue;
            }

            // Identifiers (field names) or [Quoted Fields]
            if (formula[i] == '[')
            {
                i++; // skip [
                var start = i;
                while (i < formula.Length && formula[i] != ']')
                    i++;
                tokens.Add(new Token(TokenType.Identifier, formula.Substring(start, i - start)));
                i++; // skip ]
                continue;
            }

            if (char.IsLetter(formula[i]) || formula[i] == '_')
            {
                var start = i;
                while (i < formula.Length && (char.IsLetterOrDigit(formula[i]) || formula[i] == '_'))
                    i++;
                tokens.Add(new Token(TokenType.Identifier, formula.Substring(start, i - start)));
                continue;
            }

            // Operators
            switch (formula[i])
            {
                case '+':
                    tokens.Add(new Token(TokenType.Plus));
                    i++;
                    break;
                case '-':
                    tokens.Add(new Token(TokenType.Minus));
                    i++;
                    break;
                case '*':
                    tokens.Add(new Token(TokenType.Multiply));
                    i++;
                    break;
                case '/':
                    tokens.Add(new Token(TokenType.Divide));
                    i++;
                    break;
                case '%':
                    tokens.Add(new Token(TokenType.Modulo));
                    i++;
                    break;
                case '(':
                    tokens.Add(new Token(TokenType.LeftParen));
                    i++;
                    break;
                case ')':
                    tokens.Add(new Token(TokenType.RightParen));
                    i++;
                    break;
                case '!':
                    if (i + 1 < formula.Length && formula[i + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.NotEquals));
                        i += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Not));
                        i++;
                    }
                    break;
                case '=':
                    if (i + 1 < formula.Length && formula[i + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.Equals));
                        i += 2;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected character '=' at position {i}");
                    }
                    break;
                case '<':
                    if (i + 1 < formula.Length && formula[i + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.LessThanOrEqual));
                        i += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.LessThan));
                        i++;
                    }
                    break;
                case '>':
                    if (i + 1 < formula.Length && formula[i + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.GreaterThanOrEqual));
                        i += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.GreaterThan));
                        i++;
                    }
                    break;
                case '&':
                    if (i + 1 < formula.Length && formula[i + 1] == '&')
                    {
                        tokens.Add(new Token(TokenType.AndOperator));
                        i += 2;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected character '&' at position {i}");
                    }
                    break;
                case '|':
                    if (i + 1 < formula.Length && formula[i + 1] == '|')
                    {
                        tokens.Add(new Token(TokenType.OrOperator));
                        i += 2;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected character '|' at position {i}");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected character '{formula[i]}' at position {i}");
            }
        }

        return new Queue<Token>(tokens);
    }

    #endregion

    #region Token

#pragma warning disable FDW017
    private enum TokenType
    {
        Number,
        String,
        Identifier,
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,
        LeftParen,
        RightParen,
        Equals,
        NotEquals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        AndOperator,
        OrOperator,
        Not
    }
#pragma warning restore FDW017

    private readonly struct Token
    {
        public Token(TokenType type, string? value = null)
        {
            Type = type;
            Value = value;
        }

        public TokenType Type { get; }
        public string? Value { get; }
    }

    #endregion
}
