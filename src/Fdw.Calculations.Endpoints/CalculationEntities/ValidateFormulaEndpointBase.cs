using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for validating a formula expression.
/// Route: POST /calculation-entities/validate-formula
/// </summary>
public abstract class ValidateFormulaEndpointBase : Endpoint<ValidateFormulaRequest, ValidateFormulaResponse>
{
    /// <inheritdoc/>
    public override void Configure()
    {
        Post("calculation-entities/validate-formula");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("calculation-entities:write");
#endif
        Summary(s =>
        {
            s.Summary = "Validate a formula expression";
            s.Description = "Validates the syntax of a formula expression and returns the parsed field references.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ValidateFormulaRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.FormulaBody))
        {
            await Send.OkAsync(new ValidateFormulaResponse
            {
                IsValid = false,
                ErrorMessage = "FormulaBody is required and must not be empty"
            }, ct).ConfigureAwait(false);
            return;
        }

        // Parse field references from formula
        var fieldRefs = ParseFieldReferences(req.FormulaBody);

        // Basic syntax validation: check for balanced brackets and valid structure
        var validationError = ValidateFormulaSyntax(req.FormulaBody);

        await Send.OkAsync(new ValidateFormulaResponse
        {
            IsValid = validationError is null,
            ErrorMessage = validationError,
            FieldReferences = fieldRefs.ToArray()
        }, ct).ConfigureAwait(false);
    }

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

    private static string? ValidateFormulaSyntax(string formula)
    {
        var bracketDepth = 0;
        var parenDepth = 0;

        for (var i = 0; i < formula.Length; i++)
        {
            switch (formula[i])
            {
                case '[': bracketDepth++; break;
                case ']':
                    bracketDepth--;
                    if (bracketDepth < 0)
                        return $"Unexpected ']' at position {i}";
                    break;
                case '(': parenDepth++; break;
                case ')':
                    parenDepth--;
                    if (parenDepth < 0)
                        return $"Unexpected ')' at position {i}";
                    break;
            }
        }

        if (bracketDepth != 0)
            return "Unmatched '[' in formula";
        if (parenDepth != 0)
            return "Unmatched '(' in formula";

        return null;
    }
}
