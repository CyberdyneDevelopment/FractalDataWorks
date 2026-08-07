using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Calculations;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Calculation entity type for formula-based evaluations (C# or SQL expressions).
/// Registered in <see cref="CalculationEntityTypes"/> under the key <c>"Formula"</c>.
/// </summary>
/// <remarks>
/// Compiles and executes formula expressions against resolved input data.
/// Uses field reference syntax: <c>[FieldName]</c> for field access, standard arithmetic operators.
/// </remarks>
[TypeOption(typeof(CalculationEntityTypes), "Formula")]
public sealed class FormulaCalculationEntityType : CalculationEntityBase<FormulaCalculationConfiguration>
{
    private readonly ILogger<FormulaCalculationEntityType> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FormulaCalculationEntityType"/>.
    /// </summary>
    public FormulaCalculationEntityType()
        : base("Formula", "Formula Calculation", "Executes a C# or SQL formula expression")
    {
        _logger = NullLogger<FormulaCalculationEntityType>.Instance;
    }

    /// <inheritdoc/>
    public override string? TypedContainerName => "FormulaCalculation";

    /// <inheritdoc/>
    public override IGenericConfiguration? CreateTypedConfiguration(
        IReadOnlyDictionary<string, object?> nodeConfiguration, Guid entityId)
    {
        if (!nodeConfiguration.TryGetValue("FormulaBody", out var bodyObj) || bodyObj is null)
            return null;

        // Why: IsNullOrWhiteSpace covers a null ToString(); no "?? string.Empty" fallback needed.
        var formulaBody = bodyObj.ToString();
        if (string.IsNullOrWhiteSpace(formulaBody))
            return null;

        // Why: FormulaLanguage is required — a fabricated "CSharp" default is a silent fallback
        // (NO-FALLBACKS rule). Treat a missing/blank language the same as a missing body: cannot build.
        if (!nodeConfiguration.TryGetValue("FormulaLanguage", out var langObj) || langObj is null)
            return null;
        var language = langObj.ToString();
        if (string.IsNullOrWhiteSpace(language))
            return null;

        return new FormulaCalculationConfiguration
        {
            Id = entityId,
            Name = string.Empty,
            SectionName = string.Empty,
            ServiceType = "Formula",
            FormulaLanguage = language,
            FormulaBody = formulaBody,
            TimeoutSeconds = 30
        };
    }

    /// <inheritdoc/>
    protected override IGenericResult ValidateTypedConfiguration(FormulaCalculationConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.FormulaBody))
        {
            return GenericResult.Failure(
                CalculationEntityLog.CalculationValidationFailed(
                    _logger,
                    "Formula",
                    "FormulaBody is required and must not be empty"));
        }

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<string>> ExecuteTyped(
        ICalculationEntity entity,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken)
    {
        CalculationEntityLog.FormulaExecutionStarted(_logger, entity.Name);

        try
        {
            var formulaBody = GetFormulaBody(entity);
            if (formulaBody is null)
            {
                return GenericResult<string>.Failure(
                    CalculationEntityLog.FormulaConfigurationNotLoaded(_logger, entity.Name));
            }
            if (string.IsNullOrWhiteSpace(formulaBody))
            {
                return GenericResult<string>.Failure(
                    CalculationEntityLog.CalculationValidationFailed(
                        _logger, entity.Name, "FormulaBody is empty"));
            }

            CalculationEntityLog.FormulaCompilationStarted(_logger, entity.Name);

            // Parse field references from the formula (e.g., [PassingYards], [GamesPlayed])
            var fieldRefs = ParseFieldReferences(formulaBody);

            CalculationEntityLog.FormulaCompilationSucceeded(_logger, entity.Name);

            // Resolve input data as dictionary rows
            var inputData = ResolveInputData(inputs);

            // Apply the formula to each row
            var results = new List<Dictionary<string, object>>();
            foreach (var row in inputData)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var evaluatedValue = EvaluateFormula(formulaBody, row, fieldRefs);
                var resultRow = new Dictionary<string, object>(row, StringComparer.OrdinalIgnoreCase)
                {
                    [entity.Output.ResultFieldName] = evaluatedValue
                };
                results.Add(resultRow);
            }

            var resultJson = JsonSerializer.Serialize(new
            {
                CalculationName = entity.Name,
                ResultField = entity.Output.ResultFieldName,
                RowCount = results.Count,
                Rows = results
            });

            CalculationEntityLog.FormulaExecutionSucceeded(_logger, entity.Name);
            return GenericResult<string>.Success(resultJson);
        }
        catch (Exception ex)
        {
            return GenericResult<string>.Failure(
                CalculationEntityLog.CalculationExecuteFailed(_logger, ex, entity.Name));
        }
    }

    private static string? GetFormulaBody(ICalculationEntity entity)
        => (entity.TypedConfiguration as FormulaCalculationConfiguration)?.FormulaBody;

    private static List<string> ParseFieldReferences(string formula)
    {
        var fields = new List<string>();
        var i = 0;
        while (i < formula.Length)
        {
            if (formula[i] == '[')
            {
                var end = formula.IndexOf(']', i + 1);
                if (end > i)
                {
                    fields.Add(formula.Substring(i + 1, end - i - 1));
                    i = end + 1;
                    continue;
                }
            }
            i++;
        }
        return fields;
    }

    private static List<Dictionary<string, object>> ResolveInputData(IReadOnlyList<ResolvedCalculationInput> inputs)
    {
        var rows = new List<Dictionary<string, object>>();

        foreach (var input in inputs)
        {
            if (input.ResolvedValue is IEnumerable<Dictionary<string, object>> dataRows)
            {
                rows.AddRange(dataRows);
            }
        }

        return rows;
    }

    private static decimal EvaluateFormula(
        string formula,
        Dictionary<string, object> row,
        List<string> fieldRefs)
    {
        // Replace field references with their values
        var expression = formula;
        foreach (var field in fieldRefs)
        {
            if (row.TryGetValue(field, out var value))
            {
                var decimalValue = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                expression = expression.Replace(
                    $"[{field}]",
                    decimalValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        // Evaluate the arithmetic expression
        return EvaluateArithmetic(expression);
    }

    private static decimal EvaluateArithmetic(string expression)
    {
        // Simple recursive descent parser for basic arithmetic: +, -, *, /
        var pos = 0;
        return ParseAddSub(expression.Replace(" ", string.Empty), ref pos);
    }

    private static decimal ParseAddSub(string expr, ref int pos)
    {
        var left = ParseMulDiv(expr, ref pos);
        while (pos < expr.Length && (expr[pos] == '+' || expr[pos] == '-'))
        {
            var op = expr[pos];
            pos++;
            var right = ParseMulDiv(expr, ref pos);
            left = op == '+' ? left + right : left - right;
        }
        return left;
    }

    private static decimal ParseMulDiv(string expr, ref int pos)
    {
        var left = ParseUnary(expr, ref pos);
        while (pos < expr.Length && (expr[pos] == '*' || expr[pos] == '/' || expr[pos] == '%'))
        {
            var op = expr[pos];
            pos++;
            var right = ParseUnary(expr, ref pos);
            left = op switch
            {
                '*' => left * right,
                '/' => right != 0 ? left / right : 0m,
                '%' => right != 0 ? left % right : 0m,
                _ => left
            };
        }
        return left;
    }

    // Why the operand is parsed by ParseUnary and not ParsePrimary: a unary minus may be applied to
    // another unary minus. Descending straight to ParsePrimary meant the second '-' was never treated
    // as an operator — ParsePrimary matched no digits, returned zero, and left the operand unconsumed,
    // so "--5" evaluated to 0 with the 5 silently dropped. Fdw.Expressions.FormulaParser.ParseUnary
    // already recurses into itself for exactly this reason.
    private static decimal ParseUnary(string expr, ref int pos)
    {
        if (pos < expr.Length && expr[pos] == '-')
        {
            pos++;
            return -ParseUnary(expr, ref pos);
        }
        return ParsePrimary(expr, ref pos);
    }

    private static decimal ParsePrimary(string expr, ref int pos)
    {
        if (pos < expr.Length && expr[pos] == '(')
        {
            pos++; // skip '('
            var result = ParseAddSub(expr, ref pos);
            if (pos < expr.Length && expr[pos] == ')')
                pos++; // skip ')'
            return result;
        }

        var start = pos;
        while (pos < expr.Length && (char.IsDigit(expr[pos]) || expr[pos] == '.'))
            pos++;

        if (start == pos)
            return 0m;

        return decimal.Parse(expr.AsSpan(start, pos - start), NumberStyles.Number, CultureInfo.InvariantCulture);
    }
}
